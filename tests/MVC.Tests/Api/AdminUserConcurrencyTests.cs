using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MVC.Models.Api;
using MVC.Tests.Infrastructure;

namespace MVC.Tests.Api;

public sealed class AdminUserConcurrencyTests : ApiTestBase
{
    [Fact]
    public async Task AdminCanReadUserListAndDetailsWithRowVersion()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var userClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        var user = await GetCurrentUserAsync(userClient);

        var users = await adminClient.GetFromJsonAsync<List<UserListItemDto>>("/api/users");
        Assert.NotNull(users);

        var listItem = Assert.Single(users, item => item.Id == user.Id);
        Assert.NotEmpty(listItem.RowVersion);

        var details = await adminClient.GetFromJsonAsync<UserDetailsDto>($"/api/users/{user.Id}");
        Assert.NotNull(details);
        Assert.Equal(user.Id, details.Id);
        Assert.NotEmpty(details.RowVersion);
    }

    [Fact]
    public async Task AdminUpdateUser_WithCurrentRowVersion_Succeeds()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var userClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        var user = await GetCurrentUserAsync(userClient);
        var details = await GetUserDetailsAsync(adminClient, user.Id);

        var response = await adminClient.PutAsJsonAsync($"/api/users/{user.Id}", new UpdateUserRequestDto
        {
            Name = "Admin",
            Surname = "Updated",
            PhoneNumber = "5550100",
            RowVersion = details.RowVersion
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<UserDetailsDto>();
        Assert.NotNull(updated);
        Assert.Equal("Admin", updated.Name);
        Assert.Equal("Updated", updated.Surname);
        Assert.Equal("5550100", updated.PhoneNumber);
        Assert.NotEqual(details.RowVersion, updated.RowVersion);
    }

    [Fact]
    public async Task AdminUpdateUser_WithStaleRowVersion_ReturnsConflict()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var userClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        var user = await GetCurrentUserAsync(userClient);
        var details = await GetUserDetailsAsync(adminClient, user.Id);

        var firstUpdate = await adminClient.PutAsJsonAsync($"/api/users/{user.Id}", new UpdateUserRequestDto
        {
            Name = "Current",
            Surname = "Value",
            RowVersion = details.RowVersion
        });
        Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);

        var staleUpdate = await adminClient.PutAsJsonAsync($"/api/users/{user.Id}", new UpdateUserRequestDto
        {
            Name = "Stale",
            Surname = "Value",
            RowVersion = details.RowVersion
        });

        Assert.Equal(HttpStatusCode.Conflict, staleUpdate.StatusCode);
        await AssertConflictBodyHasMessageAndCurrentAsync(staleUpdate);
    }

    [Fact]
    public async Task AdminDeleteUser_WithStaleRowVersion_ReturnsConflict()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var userClient = await AuthTestHelpers.RegisterUserAsync(Factory);
        var user = await GetCurrentUserAsync(userClient);
        var details = await GetUserDetailsAsync(adminClient, user.Id);

        var update = await adminClient.PutAsJsonAsync($"/api/users/{user.Id}", new UpdateUserRequestDto
        {
            Name = "Current",
            Surname = "Before Delete",
            RowVersion = details.RowVersion
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var staleDelete = await adminClient.DeleteAsync(
            $"/api/users/{user.Id}?rowVersion={Uri.EscapeDataString(details.RowVersion)}");

        Assert.Equal(HttpStatusCode.Conflict, staleDelete.StatusCode);
        await AssertConflictBodyHasMessageAndCurrentAsync(staleDelete);

        var stillExists = await adminClient.GetAsync($"/api/users/{user.Id}");
        Assert.Equal(HttpStatusCode.OK, stillExists.StatusCode);
    }

    private static async Task<AuthUserDto> GetCurrentUserAsync(HttpClient client)
    {
        var user = await client.GetFromJsonAsync<AuthUserDto>("/api/auth/me");
        Assert.NotNull(user);

        return user;
    }

    private static async Task<UserDetailsDto> GetUserDetailsAsync(HttpClient adminClient, string userId)
    {
        var details = await adminClient.GetFromJsonAsync<UserDetailsDto>($"/api/users/{userId}");
        Assert.NotNull(details);

        return details;
    }

    private static async Task AssertConflictBodyHasMessageAndCurrentAsync(HttpResponseMessage response)
    {
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("message", out var message));
        Assert.Contains("modified by another operation", message.GetString());
        Assert.True(root.TryGetProperty("current", out _));
    }
}
