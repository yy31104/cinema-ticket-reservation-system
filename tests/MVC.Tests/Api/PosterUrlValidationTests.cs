using System.Net;
using System.Net.Http.Json;
using MVC.Models.Api;
using MVC.Tests.Infrastructure;

namespace MVC.Tests.Api;

public sealed class PosterUrlValidationTests : ApiTestBase
{
    [Theory]
    [InlineData("https://example.com/p.jpg")]
    [InlineData("http://example.com/p.jpg")]
    [InlineData("/posters/p.jpg")]
    public async Task CreateScreening_AcceptsSafePosterUrl(string posterUrl)
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);

        var response = await adminClient.PostAsJsonAsync("/api/screenings", CreateScreening(posterUrl));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var screening = await response.Content.ReadFromJsonAsync<ScreeningDetailsDto>();
        Assert.NotNull(screening);
        Assert.Equal(posterUrl, screening.PosterUrl);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("//example.com/p.jpg")]
    [InlineData("/\\evil.com/p.jpg")]
    public async Task CreateScreening_RejectsUnsafePosterUrl(string posterUrl)
    {
        var adminClient = await AuthTestHelpers.RegisterAdminAsync(Factory);

        var response = await adminClient.PostAsJsonAsync("/api/screenings", CreateScreening(posterUrl));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static CreateScreeningRequestDto CreateScreening(string posterUrl)
    {
        return new CreateScreeningRequestDto
        {
            FilmTitle = "Poster Test",
            StartTime = new DateTime(2026, 7, 1, 19, 0, 0),
            CinemaId = 1,
            PosterUrl = posterUrl
        };
    }
}
