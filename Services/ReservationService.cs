using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MVC.Data;
using MVC.Models;

namespace MVC.Services;

public sealed class ReservationOptions
{
    public const string SectionName = "Reservations";

    /// <summary>Maximum seats one account may hold for a single screening.</summary>
    public int MaxSeatsPerUserPerScreening { get; set; } = 6;
}

public enum ReserveOutcome
{
    Created,
    AlreadyReservedByYou,
    ScreeningNotFound,
    SeatOutOfBounds,
    ScreeningStarted,
    PerUserLimitReached,
    TakenByAnotherUser,
    Failed
}

public sealed record ReserveResult(ReserveOutcome Outcome, Reservation? Reservation = null, int Limit = 0);

/// <summary>
/// The single place where seat reservation rules live. Both the JSON API and the
/// Razor pages call it, so the rules cannot drift between the two entry points.
/// </summary>
public sealed class ReservationService
{
    private readonly ApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ReservationOptions _options;

    public ReservationService(
        ApplicationDbContext context,
        TimeProvider timeProvider,
        IOptions<ReservationOptions> options)
    {
        _context = context;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    /// <summary>
    /// Screening times are stored as local wall-clock values (see README), so the
    /// comparison uses the local "now" of the configured time provider.
    /// </summary>
    public bool HasStarted(Screening screening)
    {
        return screening.StartTime <= _timeProvider.GetLocalNow().DateTime;
    }

    public async Task<ReserveResult> ReserveAsync(int screeningId, string userId, int rowNumber, int seatNumber)
    {
        var screening = await _context.Screenings
            .AsNoTracking()
            .Include(s => s.Cinema)
            .SingleOrDefaultAsync(s => s.Id == screeningId);

        if (screening is null)
        {
            return new ReserveResult(ReserveOutcome.ScreeningNotFound);
        }

        if (rowNumber < 1 || rowNumber > screening.Cinema.Rows
            || seatNumber < 1 || seatNumber > screening.Cinema.SeatsPerRow)
        {
            return new ReserveResult(ReserveOutcome.SeatOutOfBounds);
        }

        if (HasStarted(screening))
        {
            return new ReserveResult(ReserveOutcome.ScreeningStarted);
        }

        // The count-then-insert below must not interleave with another request
        // from the same user, or two requests could each see room under the
        // limit. For a Serializable, non-deferred transaction Microsoft.Data.Sqlite
        // issues BEGIN IMMEDIATE, which takes SQLite's single write lock before
        // the count, so the check and the insert happen as one step.
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var existing = await FindSeatAsync(screeningId, rowNumber, seatNumber);
        if (existing is not null)
        {
            await transaction.RollbackAsync();
            return existing.UserId == userId
                ? new ReserveResult(ReserveOutcome.AlreadyReservedByYou, existing)
                : new ReserveResult(ReserveOutcome.TakenByAnotherUser);
        }

        var limit = _options.MaxSeatsPerUserPerScreening;
        var held = await _context.Reservations
            .CountAsync(r => r.ScreeningId == screeningId && r.UserId == userId);
        if (held >= limit)
        {
            await transaction.RollbackAsync();
            return new ReserveResult(ReserveOutcome.PerUserLimitReached, Limit: limit);
        }

        var reservation = new Reservation
        {
            ScreeningId = screeningId,
            UserId = userId,
            RowNumber = rowNumber,
            SeatNumber = seatNumber,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };
        _context.Reservations.Add(reservation);

        try
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new ReserveResult(ReserveOutcome.Created, reservation);
        }
        catch (DbUpdateException ex) when (IsSeatUniqueConstraintViolation(ex))
        {
            // Kept as a backstop: the unique index is still the final authority
            // if a writer outside this service inserts the same seat.
            await transaction.RollbackAsync();
            _context.Entry(reservation).State = EntityState.Detached;
            var winner = await FindSeatAsync(screeningId, rowNumber, seatNumber);
            return winner?.UserId == userId
                ? new ReserveResult(ReserveOutcome.AlreadyReservedByYou, winner)
                : new ReserveResult(ReserveOutcome.TakenByAnotherUser);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return new ReserveResult(ReserveOutcome.Failed);
        }
    }

    private Task<Reservation?> FindSeatAsync(int screeningId, int rowNumber, int seatNumber)
    {
        return _context.Reservations
            .AsNoTracking()
            .SingleOrDefaultAsync(r =>
                r.ScreeningId == screeningId
                && r.RowNumber == rowNumber
                && r.SeatNumber == seatNumber);
    }

    public static bool IsSeatUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is not SqliteException sqliteEx)
        {
            return false;
        }

        const int sqliteConstraintErrorCode = 19;
        if (sqliteEx.SqliteErrorCode != sqliteConstraintErrorCode)
        {
            return false;
        }

        return sqliteEx.Message.Contains("IX_Reservations_ScreeningId_RowNumber_SeatNumber", StringComparison.OrdinalIgnoreCase)
            || sqliteEx.Message.Contains("UNIQUE constraint failed: Reservations.ScreeningId, Reservations.RowNumber, Reservations.SeatNumber", StringComparison.OrdinalIgnoreCase);
    }
}
