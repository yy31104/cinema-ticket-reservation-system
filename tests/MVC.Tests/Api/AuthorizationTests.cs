using System.Net;
using System.Net.Http.Json;
using MVC.Models.Api;
using MVC.Tests.Infrastructure;

namespace MVC.Tests.Api;

public sealed class AuthorizationTests : ApiTestBase
{
    [Fact]
    public async Task AnonymousCanGetScreenings()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/screenings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousCannotCreateScreening()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/screenings", ValidScreening());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NonAdminCannotCreateScreening()
    {
        await AuthTestHelpers.RegisterAdminAsync(Factory);
        var userClient = await AuthTestHelpers.RegisterUserAsync(Factory);

        var response = await userClient.PostAsJsonAsync("/api/screenings", ValidScreening());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NonAdminCannotGetUsers()
    {
        await AuthTestHelpers.RegisterAdminAsync(Factory);
        var userClient = await AuthTestHelpers.RegisterUserAsync(Factory);

        var response = await userClient.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminCanGetUsers()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);

        var response = await adminClient.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FirstRegisteredUserIsAdmin()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);

        var user = await adminClient.GetFromJsonAsync<AuthUserDto>("/api/auth/me");

        Assert.NotNull(user);
        Assert.True(user.IsAdmin);
        Assert.Contains("Admin", user.Roles);
    }

    [Fact]
    public async Task SecondRegisteredUserIsNotAdmin()
    {
        await AuthTestHelpers.RegisterAdminAsync(Factory);
        var userClient = await AuthTestHelpers.RegisterUserAsync(Factory);

        var user = await userClient.GetFromJsonAsync<AuthUserDto>("/api/auth/me");

        Assert.NotNull(user);
        Assert.False(user.IsAdmin);
        Assert.DoesNotContain("Admin", user.Roles);
    }

    private static CreateScreeningRequestDto ValidScreening()
    {
        return new CreateScreeningRequestDto
        {
            FilmTitle = "Authorization Test",
            StartTime = new DateTime(2026, 7, 5, 18, 0, 0),
            CinemaId = 1
        };
    }
}
