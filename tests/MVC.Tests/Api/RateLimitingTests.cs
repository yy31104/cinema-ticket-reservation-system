using System.Net;
using System.Net.Http.Json;
using MVC.Models.Api;
using MVC.Services;
using MVC.Tests.Infrastructure;

namespace MVC.Tests.Api;

public sealed class RateLimitingTests : ApiTestBase
{
    private const string Password = "Passw0rd!";

    [Fact]
    public async Task ApiLogin_BeyondLimit_Returns429WithRetryAfter_EvenForCorrectPassword()
    {
        Factory.RateLimits = o => o.Login = new FixedWindowLimit { PermitLimit = 3, WindowSeconds = 60 };
        var email = await RegisterAsync(CreateClient());
        var client = CreateClient();

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var wrong = await client.PostAsJsonAsync("/api/auth/login", Login(email, "wrong-password"));
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        }

        // Once limited, even the right password is refused, so guessing stops paying off.
        var limited = await client.PostAsJsonAsync("/api/auth/login", Login(email, Password));

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.TryGetValues("Retry-After", out var retryAfter));
        Assert.InRange(int.Parse(retryAfter.Single()), 1, 60);
        var error = await limited.Content.ReadFromJsonAsync<ApiErrorDto>();
        Assert.Contains("Too many requests", error!.Message);
    }

    [Fact]
    public async Task IdentityLoginPage_CountsPostsOnly_AndSharesBudgetWithApiLogin()
    {
        Factory.RateLimits = o => o.Login = new FixedWindowLimit { PermitLimit = 2, WindowSeconds = 60 };
        var client = CreateClient();

        // Rendering the form never counts.
        for (var i = 0; i < 5; i++)
        {
            var page = await client.GetAsync("/Identity/Account/Login");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        }

        var token = await HtmlForms.GetAntiforgeryTokenAsync(client, "/Identity/Account/Login");
        var formPost = await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = "nobody@example.test",
            ["Input.Password"] = "wrong-password",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, formPost.StatusCode);

        var apiPost = await client.PostAsJsonAsync("/api/auth/login", Login("nobody@example.test", "wrong-password"));
        Assert.Equal(HttpStatusCode.Unauthorized, apiPost.StatusCode);

        var limitedForm = await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = "nobody@example.test",
            ["Input.Password"] = "wrong-password",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.TooManyRequests, limitedForm.StatusCode);
        Assert.StartsWith("text/plain", limitedForm.Content.Headers.ContentType!.ToString());
    }

    [Fact]
    public async Task Reservations_LimitIsPerUser_AndSharedByMvcAndApi()
    {
        Factory.RateLimits = o => o.Reservations = new FixedWindowLimit { PermitLimit = 3, WindowSeconds = 60 };
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var userClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        var otherClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        var screeningId = await CreateScreeningAsync(adminClient);

        Assert.Equal(HttpStatusCode.Created, (await ReserveViaApi(userClient, screeningId, 1, 1)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await ReserveViaApi(userClient, screeningId, 1, 2)).StatusCode);

        var token = await HtmlForms.GetAntiforgeryTokenAsync(userClient, $"/Screenings/Details/{screeningId}");
        var mvc = await ReserveViaMvc(userClient, screeningId, 1, 3, token);
        Assert.Equal(HttpStatusCode.Redirect, mvc.StatusCode);

        // Fourth request from the same user, on either path, is refused.
        Assert.Equal(HttpStatusCode.TooManyRequests, (await ReserveViaApi(userClient, screeningId, 1, 4)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await ReserveViaMvc(userClient, screeningId, 1, 4, token)).StatusCode);

        // A different account has its own budget.
        Assert.Equal(HttpStatusCode.Created, (await ReserveViaApi(otherClient, screeningId, 2, 1)).StatusCode);

        // Reads are not limited.
        var seats = await userClient.GetAsync($"/api/screenings/{screeningId}/seats");
        Assert.Equal(HttpStatusCode.OK, seats.StatusCode);
    }

    [Fact]
    public async Task DefaultLimits_LeaveNormalUseAlone()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var userClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        var screeningId = await CreateScreeningAsync(adminClient);

        // A full per-user allowance of seats plus a retry stays well under the default budget.
        for (var seat = 1; seat <= 7; seat++)
        {
            var response = await ReserveViaApi(userClient, screeningId, 1, seat);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }

    private static Task<HttpResponseMessage> ReserveViaApi(HttpClient client, int screeningId, int row, int seat)
    {
        return client.PostAsJsonAsync(
            $"/api/screenings/{screeningId}/reservations",
            new ReserveSeatRequestDto { RowNumber = row, SeatNumber = seat });
    }

    private static Task<HttpResponseMessage> ReserveViaMvc(HttpClient client, int screeningId, int row, int seat, string token)
    {
        return client.PostAsync($"/Screenings/Reserve/{screeningId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["rowNumber"] = row.ToString(),
            ["seatNumber"] = seat.ToString(),
            ["__RequestVerificationToken"] = token
        }));
    }

    private static async Task<int> CreateScreeningAsync(HttpClient adminClient)
    {
        var response = await adminClient.PostAsJsonAsync("/api/screenings", new CreateScreeningRequestDto
        {
            FilmTitle = "Rate Limit Test",
            StartTime = new DateTime(2026, 7, 4, 18, 0, 0),
            CinemaId = 1
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var screening = await response.Content.ReadFromJsonAsync<ScreeningDetailsDto>();
        return screening!.Id;
    }

    private static async Task<string> RegisterAsync(HttpClient client)
    {
        var email = $"ratelimit-{Guid.NewGuid():N}@example.test";
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = Password,
            ConfirmPassword = Password,
            Name = "Rate",
            Surname = "Limit"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return email;
    }

    private static LoginRequestDto Login(string email, string password)
    {
        return new LoginRequestDto { Email = email, Password = password };
    }
}
