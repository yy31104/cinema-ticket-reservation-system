using System.Net;
using System.Net.Http.Json;
using MVC.Models.Api;

namespace MVC.Tests.Infrastructure;

public static class AuthTestHelpers
{
    private const string Password = "Passw0rd!";

    public static async Task<HttpClient> RegisterAdminAsync(CinemaApiFactory factory)
    {
        var client = factory.CreateApiClient();
        var user = await RegisterAsync(client, NewEmail("admin"));

        Assert.True(user.IsAdmin);
        Assert.Contains("Admin", user.Roles);

        return client;
    }

    public static async Task<HttpClient> RegisterUserAsync(CinemaApiFactory factory)
    {
        var client = factory.CreateApiClient();
        var user = await RegisterAsync(client, NewEmail("user"));

        Assert.False(user.IsAdmin);

        return client;
    }

    private static async Task<AuthUserDto> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = Password,
            ConfirmPassword = Password,
            Name = "Test",
            Surname = "User"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<AuthUserDto>();
        Assert.NotNull(user);
        Assert.Equal(email, user.Email);

        return user;
    }

    private static string NewEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@example.test";
    }
}
