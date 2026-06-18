using System.Net;
using System.Net.Http.Json;
using MVC.Models.Api;
using MVC.Tests.Infrastructure;

namespace MVC.Tests.Api;

public sealed class ScreeningMetadataTests : ApiTestBase
{
    [Fact]
    public async Task CreateScreening_WithoutMetadata_ReturnsNullMetadataFields()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);

        var response = await adminClient.PostAsJsonAsync("/api/screenings", BaseScreening());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var screening = await response.Content.ReadFromJsonAsync<ScreeningDetailsDto>();
        Assert.NotNull(screening);
        Assert.Null(screening.PosterUrl);
        Assert.Null(screening.Synopsis);
        Assert.Null(screening.DurationMinutes);
        Assert.Null(screening.Genre);
        Assert.Null(screening.AgeRating);
    }

    [Fact]
    public async Task CreateScreening_WithMetadata_ReturnsEchoedMetadata()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);

        var response = await adminClient.PostAsJsonAsync("/api/screenings", MetadataScreening());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var screening = await response.Content.ReadFromJsonAsync<ScreeningDetailsDto>();
        Assert.NotNull(screening);
        Assert.Equal("/posters/arrival.jpg", screening.PosterUrl);
        Assert.Equal("A linguist works to understand visitors from another world.", screening.Synopsis);
        Assert.Equal(116, screening.DurationMinutes);
        Assert.Equal("Sci-Fi", screening.Genre);
        Assert.Equal("PG-13", screening.AgeRating);
    }

    [Fact]
    public async Task GetScreenings_IncludesMetadata()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var created = await CreateScreeningAsync(adminClient, MetadataScreening());

        var screenings = await adminClient.GetFromJsonAsync<List<ScreeningListItemDto>>("/api/screenings");

        Assert.NotNull(screenings);
        var screening = Assert.Single(screenings, s => s.Id == created.Id);
        Assert.Equal("/posters/arrival.jpg", screening.PosterUrl);
        Assert.Equal("A linguist works to understand visitors from another world.", screening.Synopsis);
        Assert.Equal(116, screening.DurationMinutes);
        Assert.Equal("Sci-Fi", screening.Genre);
        Assert.Equal("PG-13", screening.AgeRating);
    }

    [Fact]
    public async Task GetScreening_IncludesMetadataIncludingSynopsis()
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);
        var created = await CreateScreeningAsync(adminClient, MetadataScreening());

        var screening = await adminClient.GetFromJsonAsync<ScreeningDetailsDto>($"/api/screenings/{created.Id}");

        Assert.NotNull(screening);
        Assert.Equal(created.Id, screening.Id);
        Assert.Equal("/posters/arrival.jpg", screening.PosterUrl);
        Assert.Equal("A linguist works to understand visitors from another world.", screening.Synopsis);
        Assert.Equal(116, screening.DurationMinutes);
        Assert.Equal("Sci-Fi", screening.Genre);
        Assert.Equal("PG-13", screening.AgeRating);
    }

    private static async Task<ScreeningDetailsDto> CreateScreeningAsync(
        HttpClient client,
        CreateScreeningRequestDto request)
    {
        var response = await client.PostAsJsonAsync("/api/screenings", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var screening = await response.Content.ReadFromJsonAsync<ScreeningDetailsDto>();
        Assert.NotNull(screening);

        return screening;
    }

    private static CreateScreeningRequestDto BaseScreening()
    {
        return new CreateScreeningRequestDto
        {
            FilmTitle = "No Metadata",
            StartTime = new DateTime(2026, 7, 2, 20, 0, 0),
            CinemaId = 1
        };
    }

    private static CreateScreeningRequestDto MetadataScreening()
    {
        return new CreateScreeningRequestDto
        {
            FilmTitle = "Arrival",
            StartTime = new DateTime(2026, 7, 3, 20, 0, 0),
            CinemaId = 1,
            PosterUrl = "/posters/arrival.jpg",
            Synopsis = "A linguist works to understand visitors from another world.",
            DurationMinutes = 116,
            Genre = "Sci-Fi",
            AgeRating = "PG-13"
        };
    }
}
