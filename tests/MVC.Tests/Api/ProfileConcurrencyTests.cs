using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MVC.Models.Api;
using MVC.Tests.Infrastructure;

namespace MVC.Tests.Api;

public sealed class ProfileConcurrencyTests : ApiTestBase
{
    [Fact]
    public async Task AuthenticatedUserCanReadProfileWithRowVersion()
    {
        var client = await AuthTestHelpers.RegisterAdminAsync(Factory);

        var profile = await client.GetFromJsonAsync<ProfileDto>("/api/profile");

        Assert.NotNull(profile);
        Assert.NotEmpty(profile.Id);
        Assert.NotEmpty(profile.Email);
        Assert.NotEmpty(profile.RowVersion);
    }

    [Fact]
    public async Task ProfileUpdate_WithCurrentRowVersion_Succeeds()
    {
        var client = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var profile = await GetProfileAsync(client);

        var response = await client.PutAsJsonAsync("/api/profile", new UpdateProfileRequestDto
        {
            Name = "Updated",
            Surname = "Profile",
            PhoneNumber = "123456789",
            RowVersion = profile.RowVersion
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProfileDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.Name);
        Assert.Equal("Profile", updated.Surname);
        Assert.Equal("123456789", updated.PhoneNumber);
        Assert.NotEqual(profile.RowVersion, updated.RowVersion);
    }

    [Fact]
    public async Task ProfileUpdate_WithStaleRowVersion_ReturnsConflict()
    {
        var client = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var profile = await GetProfileAsync(client);

        var firstUpdate = await client.PutAsJsonAsync("/api/profile", new UpdateProfileRequestDto
        {
            Name = "Current",
            Surname = "Value",
            RowVersion = profile.RowVersion
        });
        Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);

        var staleUpdate = await client.PutAsJsonAsync("/api/profile", new UpdateProfileRequestDto
        {
            Name = "Stale",
            Surname = "Value",
            RowVersion = profile.RowVersion
        });

        Assert.Equal(HttpStatusCode.Conflict, staleUpdate.StatusCode);
        await AssertConflictBodyHasMessageAndCurrentAsync(staleUpdate);
    }

    private static async Task<ProfileDto> GetProfileAsync(HttpClient client)
    {
        var profile = await client.GetFromJsonAsync<ProfileDto>("/api/profile");
        Assert.NotNull(profile);

        return profile;
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
