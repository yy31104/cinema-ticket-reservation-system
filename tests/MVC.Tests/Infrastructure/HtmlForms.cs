using System.Text.RegularExpressions;

namespace MVC.Tests.Infrastructure;

public static partial class HtmlForms
{
    /// <summary>Loads a server-rendered page and returns the antiforgery token from its form.</summary>
    public static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        var match = AntiforgeryField().Match(html);
        Assert.True(match.Success, $"{url} should render an antiforgery token.");
        return match.Groups["token"].Value;
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<token>[^\"]+)\"")]
    private static partial Regex AntiforgeryField();
}
