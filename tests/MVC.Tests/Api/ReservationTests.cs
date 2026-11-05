using System.Net;
using System.Net.Http.Json;
using MVC.Models.Api;
using MVC.Tests.Infrastructure;

namespace MVC.Tests.Api;

public sealed class ReservationTests : ApiTestBase
{
    [Fact]
    public async Task ReserveFreeSeat_ReturnsCreated()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var userClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        var screening = await CreateScreeningAsync(adminClient);

        var response = await userClient.PostAsJsonAsync($"/api/screenings/{screening.Id}/reservations", Seat(1, 1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ReserveSameSeatBySecondUser_ReturnsConflict()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var firstUserClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        var secondUserClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        var screening = await CreateScreeningAsync(adminClient);

        var firstResponse = await firstUserClient.PostAsJsonAsync($"/api/screenings/{screening.Id}/reservations", Seat(1, 1));
        var secondResponse = await secondUserClient.PostAsJsonAsync($"/api/screenings/{screening.Id}/reservations", Seat(1, 1));

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task CancelOwnReservation_AllowsSeatToBeReservedAgain()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var userClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        var screening = await CreateScreeningAsync(adminClient);

        var reserveResponse = await userClient.PostAsJsonAsync($"/api/screenings/{screening.Id}/reservations", Seat(1, 1));
        Assert.Equal(HttpStatusCode.Created, reserveResponse.StatusCode);

        var reservation = await reserveResponse.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.NotNull(reservation);

        var cancelResponse = await userClient.DeleteAsync($"/api/screenings/{screening.Id}/reservations/{reservation.Id}");
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        var reserveAgainResponse = await userClient.PostAsJsonAsync($"/api/screenings/{screening.Id}/reservations", Seat(1, 1));
        Assert.Equal(HttpStatusCode.Created, reserveAgainResponse.StatusCode);
    }

    [Fact]
    public async Task ReserveAfterScreeningStarted_ReturnsConflictAndStoresNothing()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var userClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        var screening = await CreateScreeningAsync(adminClient);

        // The screening starts 2026-07-04 18:00; move the clock one minute past it.
        Factory.Clock.SetUtcNow(new DateTimeOffset(2026, 7, 4, 18, 1, 0, TimeSpan.Zero));
        var response = await userClient.PostAsJsonAsync($"/api/screenings/{screening.Id}/reservations", Seat(1, 1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        Assert.Contains("already started", error!.Message);

        var details = await userClient.GetFromJsonAsync<ScreeningDetailsDto>($"/api/screenings/{screening.Id}");
        Assert.Equal(0, details!.ReservationCount);
    }

    [Fact]
    public async Task ReserveBeyondPerUserLimit_ReturnsConflict()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var userClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        var otherClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        var screening = await CreateScreeningAsync(adminClient);

        for (var seat = 1; seat <= 6; seat++)
        {
            var allowed = await userClient.PostAsJsonAsync($"/api/screenings/{screening.Id}/reservations", Seat(1, seat));
            Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
        }

        var overLimit = await userClient.PostAsJsonAsync($"/api/screenings/{screening.Id}/reservations", Seat(2, 1));
        Assert.Equal(HttpStatusCode.Conflict, overLimit.StatusCode);
        var error = await overLimit.Content.ReadFromJsonAsync<ApiErrorDto>();
        Assert.Contains("at most 6 seats", error!.Message);

        // The limit is per user: another account can still take the same seat.
        var other = await otherClient.PostAsJsonAsync($"/api/screenings/{screening.Id}/reservations", Seat(2, 1));
        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
    }

    [Fact]
    public async Task RetrySameSeatBySameUser_ReturnsExistingReservation()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var userClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        var screening = await CreateScreeningAsync(adminClient);

        var first = await userClient.PostAsJsonAsync($"/api/screenings/{screening.Id}/reservations", Seat(1, 1));
        var retry = await userClient.PostAsJsonAsync($"/api/screenings/{screening.Id}/reservations", Seat(1, 1));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var original = await first.Content.ReadFromJsonAsync<ReservationDto>();
        var replayed = await retry.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.Equal(original!.Id, replayed!.Id);
        Assert.True(replayed.ReservedByCurrentUser);
    }

    [Fact]
    public async Task RetryOwnSeatAtPerUserLimit_StillReturnsExistingReservation()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var userClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        var screening = await CreateScreeningAsync(adminClient);

        for (var seat = 1; seat <= 6; seat++)
        {
            await userClient.PostAsJsonAsync($"/api/screenings/{screening.Id}/reservations", Seat(1, seat));
        }

        var retry = await userClient.PostAsJsonAsync($"/api/screenings/{screening.Id}/reservations", Seat(1, 6));

        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
    }

    private static async Task<ScreeningDetailsDto> CreateScreeningAsync(HttpClient adminClient)
    {
        var response = await adminClient.PostAsJsonAsync("/api/screenings", new CreateScreeningRequestDto
        {
            FilmTitle = "Reservation Test",
            StartTime = new DateTime(2026, 7, 4, 18, 0, 0),
            CinemaId = 1
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var screening = await response.Content.ReadFromJsonAsync<ScreeningDetailsDto>();
        Assert.NotNull(screening);

        return screening;
    }

    private static ReserveSeatRequestDto Seat(int rowNumber, int seatNumber)
    {
        return new ReserveSeatRequestDto
        {
            RowNumber = rowNumber,
            SeatNumber = seatNumber
        };
    }
}
