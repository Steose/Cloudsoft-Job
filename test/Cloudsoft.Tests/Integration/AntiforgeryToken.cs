using System.Text.RegularExpressions;

namespace Cloudsoft.Tests.Integration;

internal static partial class AntiforgeryToken
{
    public static async Task<string> ReadAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var match = TokenRegex().Match(html);

        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not find antiforgery token on {path}.");
        }

        return match.Groups["token"].Value;
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<token>[^\"]+)\"", RegexOptions.CultureInvariant)]
    private static partial Regex TokenRegex();
}
