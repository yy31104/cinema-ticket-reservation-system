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
