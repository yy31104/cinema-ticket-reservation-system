using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using MVC.Models.Api;
using MVC.Tests.Infrastructure;

namespace MVC.Tests.Mvc;

/// <summary>
/// Drives the Razor form endpoint (POST /Screenings/Reserve/{id}) and checks it
/// applies the same rules as the JSON API, because both call ReservationService.
/// </summary>
public sealed partial class MvcReservationTests : ApiTestBase
{
    [Fact]
    public async Task MvcReserve_CreatesSeatThatApiTreatsAsOwnRetry()
    {
        var (userClient, screeningId) = await ArrangeAsync();

        var page = await PostReserveAsync(userClient, screeningId, 3, 4);
        Assert.Contains("Seat R3 S4 reserved.", page);

        // Same seat, other entry point: idempotent 200, not a 409.
        var apiRetry = await userClient.PostAsJsonAsync($"/api/screenings/{screeningId}/reservations", Seat(3, 4));
        Assert.Equal(HttpStatusCode.OK, apiRetry.StatusCode);
        Assert.Equal(1, await ReservationCountAsync(userClient, screeningId));
    }

    [Fact]
    public async Task ApiReserve_ThenMvcRetry_ReportsAlreadyYours()
    {
        var (userClient, screeningId) = await ArrangeAsync();
        var created = await userClient.PostAsJsonAsync($"/api/screenings/{screeningId}/reservations", Seat(2, 2));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var page = await PostReserveAsync(userClient, screeningId, 2, 2);

        Assert.Contains("already reserved by you", page);
        Assert.Equal(1, await ReservationCountAsync(userClient, screeningId));
    }

    [Fact]
    public async Task MvcReserve_AfterScreeningStarted_StoresNothing()
    {
        var (userClient, screeningId) = await ArrangeAsync();
        var token = await GetAntiforgeryTokenAsync(userClient, screeningId);

        Factory.Clock.SetUtcNow(new DateTimeOffset(2026, 7, 4, 18, 0, 0, TimeSpan.Zero));
        var page = await PostReserveAsync(userClient, screeningId, 1, 1, token);

        Assert.Contains("already started", page);
        Assert.Equal(0, await ReservationCountAsync(userClient, screeningId));
    }

    [Fact]
    public async Task MvcReserve_CountsSeatsTakenThroughApiTowardsLimit()
    {
        var (userClient, screeningId) = await ArrangeAsync();
        for (var seat = 1; seat <= 6; seat++)
        {
            var allowed = await userClient.PostAsJsonAsync($"/api/screenings/{screeningId}/reservations", Seat(1, seat));
            Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
        }

        var page = await PostReserveAsync(userClient, screeningId, 2, 1);

        Assert.Contains("at most 6 seats", page);
        Assert.Equal(6, await ReservationCountAsync(userClient, screeningId));
    }

    [Fact]
    public async Task MvcReserve_SeatTakenByOtherUser_ShowsConflictMessage()
    {
        var (userClient, screeningId) = await ArrangeAsync();
        var otherClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        await otherClient.PostAsJsonAsync($"/api/screenings/{screeningId}/reservations", Seat(5, 5));

        var page = await PostReserveAsync(userClient, screeningId, 5, 5);

        Assert.Contains("just taken by another user", page);
        Assert.Equal(1, await ReservationCountAsync(userClient, screeningId));
    }

    private async Task<(HttpClient UserClient, int ScreeningId)> ArrangeAsync()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var userClient = await AuthTestHelpers.RegisterUserAsync(Factory);

        var response = await adminClient.PostAsJsonAsync("/api/screenings", new CreateScreeningRequestDto
        {
            FilmTitle = "MVC Reservation Test",
            StartTime = new DateTime(2026, 7, 4, 18, 0, 0),
            CinemaId = 1
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var screening = await response.Content.ReadFromJsonAsync<ScreeningDetailsDto>();

        return (userClient, screening!.Id);
    }

    /// <summary>Posts the seat form and returns the Details page the redirect lands on.</summary>
    private static async Task<string> PostReserveAsync(HttpClient client, int screeningId, int row, int seat, string? token = null)
    {
        token ??= await GetAntiforgeryTokenAsync(client, screeningId);

        var response = await client.PostAsync($"/Screenings/Reserve/{screeningId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["rowNumber"] = row.ToString(),
            ["seatNumber"] = seat.ToString(),
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var details = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
        return WebUtility.HtmlDecode(await details.Content.ReadAsStringAsync());
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, int screeningId)
    {
        var html = await client.GetStringAsync($"/Screenings/Details/{screeningId}");
        var match = AntiforgeryField().Match(html);
        Assert.True(match.Success, "Details page should render an antiforgery token.");
        return match.Groups["token"].Value;
    }

    private static async Task<int> ReservationCountAsync(HttpClient client, int screeningId)
    {
        var details = await client.GetFromJsonAsync<ScreeningDetailsDto>($"/api/screenings/{screeningId}");
        return details!.ReservationCount;
    }

    private static ReserveSeatRequestDto Seat(int rowNumber, int seatNumber)
    {
        return new ReserveSeatRequestDto { RowNumber = rowNumber, SeatNumber = seatNumber };
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<token>[^\"]+)\"")]
    private static partial Regex AntiforgeryField();
}
