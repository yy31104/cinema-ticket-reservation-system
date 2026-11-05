using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MVC.Data;
using MVC.Models;
using MVC.Services;
using MVC.Tests.Infrastructure;

namespace MVC.Tests.Services;

/// <summary>
/// Exercises ReservationService directly, without HTTP. Both the MVC controller
/// and the JSON API delegate to this service, so these tests pin the rules once
/// for both entry points.
/// </summary>
public sealed class ReservationServiceTests : IDisposable
{
    private const string Alice = "user-alice";
    private const string Bob = "user-bob";

    private static readonly DateTime ScreeningStart = new(2026, 7, 4, 18, 0, 0);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly TestTimeProvider _clock = new(new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly int _screeningId;

    public ReservationServiceTests()
    {
        _connection.Open();
        using var context = NewContext();
        context.Database.EnsureCreated();

        context.Users.AddRange(
            new ApplicationUser { Id = Alice, UserName = "alice@example.test" },
            new ApplicationUser { Id = Bob, UserName = "bob@example.test" });

        // Cinema 1 is seeded with 8 rows x 12 seats.
        var screening = new Screening { FilmTitle = "Service Test", StartTime = ScreeningStart, CinemaId = 1 };
        context.Screenings.Add(screening);
        context.SaveChanges();
        _screeningId = screening.Id;
    }

    [Fact]
    public async Task FreeSeat_IsCreatedWithClockTimestamp()
    {
        var result = await Reserve(Alice, 1, 1);

        Assert.Equal(ReserveOutcome.Created, result.Outcome);
        Assert.NotNull(result.Reservation);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, result.Reservation.CreatedAt);
        Assert.Equal(1, await CountReservations());
    }

    [Fact]
    public async Task UnknownScreening_IsNotFound()
    {
        var result = await Reserve(Alice, 1, 1, screeningId: 9999);

        Assert.Equal(ReserveOutcome.ScreeningNotFound, result.Outcome);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(9, 1)]
    [InlineData(1, 13)]
    public async Task SeatOutsideAuditorium_IsRejected(int row, int seat)
    {
        var result = await Reserve(Alice, row, seat);

        Assert.Equal(ReserveOutcome.SeatOutOfBounds, result.Outcome);
        Assert.Equal(0, await CountReservations());
    }

    [Fact]
    public async Task ScreeningAtExactStartTime_CountsAsStarted()
    {
        _clock.SetUtcNow(new DateTimeOffset(ScreeningStart, TimeSpan.Zero));

        var result = await Reserve(Alice, 1, 1);

        Assert.Equal(ReserveOutcome.ScreeningStarted, result.Outcome);
        Assert.Equal(0, await CountReservations());
    }

    [Fact]
    public async Task OneMinuteBeforeStart_IsStillAllowed()
    {
        _clock.SetUtcNow(new DateTimeOffset(ScreeningStart.AddMinutes(-1), TimeSpan.Zero));

        var result = await Reserve(Alice, 1, 1);

        Assert.Equal(ReserveOutcome.Created, result.Outcome);
    }

    [Fact]
    public async Task SeatHeldByAnotherUser_IsTaken()
    {
        await Reserve(Bob, 2, 3);

        var result = await Reserve(Alice, 2, 3);

        Assert.Equal(ReserveOutcome.TakenByAnotherUser, result.Outcome);
        Assert.Null(result.Reservation);
        Assert.Equal(1, await CountReservations());
    }

    [Fact]
    public async Task RetryOfOwnSeat_ReturnsTheOriginalReservation()
    {
        var first = await Reserve(Alice, 2, 3);

        var retry = await Reserve(Alice, 2, 3);

        Assert.Equal(ReserveOutcome.AlreadyReservedByYou, retry.Outcome);
        Assert.Equal(first.Reservation!.Id, retry.Reservation!.Id);
        Assert.Equal(1, await CountReservations());
    }

    [Fact]
    public async Task PerUserLimit_IsConfigurableAndPerUser()
    {
        Assert.Equal(ReserveOutcome.Created, (await Reserve(Alice, 1, 1, limit: 2)).Outcome);
        Assert.Equal(ReserveOutcome.Created, (await Reserve(Alice, 1, 2, limit: 2)).Outcome);

        var overLimit = await Reserve(Alice, 1, 3, limit: 2);
        Assert.Equal(ReserveOutcome.PerUserLimitReached, overLimit.Outcome);
        Assert.Equal(2, overLimit.Limit);

        // Another account is unaffected by Alice's count.
        Assert.Equal(ReserveOutcome.Created, (await Reserve(Bob, 1, 3, limit: 2)).Outcome);
    }

    [Fact]
    public async Task RetryOfOwnSeatAtLimit_IsIdempotentNotLimitReached()
    {
        await Reserve(Alice, 1, 1, limit: 1);

        var retry = await Reserve(Alice, 1, 1, limit: 1);

        Assert.Equal(ReserveOutcome.AlreadyReservedByYou, retry.Outcome);
    }

    [Fact]
    public async Task StartedScreening_WinsOverIdempotentRetry()
    {
        await Reserve(Alice, 1, 1);
        _clock.SetUtcNow(new DateTimeOffset(ScreeningStart.AddMinutes(5), TimeSpan.Zero));

        var retry = await Reserve(Alice, 1, 1);

        Assert.Equal(ReserveOutcome.ScreeningStarted, retry.Outcome);
    }

    [Fact]
    public async Task UniqueIndexViolation_IsRecognisedOnlyForTheSeatIndex()
    {
        await using var context = NewContext();
        context.Reservations.Add(new Reservation { ScreeningId = _screeningId, UserId = Bob, RowNumber = 4, SeatNumber = 4 });
        await context.SaveChangesAsync();

        // Bypass the service to hit the unique index directly, then make sure the
        // service's classifier recognises exactly that failure.
        context.Reservations.Add(new Reservation { ScreeningId = _screeningId, UserId = Alice, RowNumber = 4, SeatNumber = 4 });
        var seatClash = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.True(ReservationService.IsSeatUniqueConstraintViolation(seatClash));

        context.ChangeTracker.Clear();
        // Same SQLite error code (19, constraint) but a different constraint.
        context.Reservations.Add(new Reservation { ScreeningId = _screeningId, UserId = null!, RowNumber = 5, SeatNumber = 5 });
        var notNull = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.False(ReservationService.IsSeatUniqueConstraintViolation(notNull));
    }

    private async Task<ReserveResult> Reserve(string userId, int row, int seat, int? screeningId = null, int limit = 6)
    {
        await using var context = NewContext();
        var service = new ReservationService(
            context,
            _clock,
            Options.Create(new ReservationOptions { MaxSeatsPerUserPerScreening = limit }));

        return await service.ReserveAsync(screeningId ?? _screeningId, userId, row, seat);
    }

    private async Task<int> CountReservations()
    {
        await using var context = NewContext();
        return await context.Reservations.CountAsync();
    }

    private ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
