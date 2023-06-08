using System.Web;

namespace HttpDataClient.Helpers;

internal static class UrlHelper
{
    public static string GetHost(string url)
    {
        return IsCorrectUrl(url)
            ? new Uri(url).Host
            : null;
    }

    private static bool IsCorrectUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    public static string HideSecrets(this string url)
    {
        if(!url.IsSignificant())
            return url;

        var queryParams = HttpUtility.ParseQueryString(url);
        if(!queryParams.HasKeys())
            return url;

        var paramsToClear = queryParams.AllKeys
            .Where(param => param != null && SecretParams.Any(param.SignificantContains))
            .ToList();
        if(paramsToClear.Count == 0)
            return url;

        foreach(var param in paramsToClear)
            queryParams[param] = "***";

        var separator = url.IndexOf('?');
        var leftPart = IsCorrectUrl(queryParams.ToString())
            ? null
            : url.Substring(0, separator + 1);
        return separator <= 0
            ? throw new Exception($"Impossible url: '{url}'")
            : $"{leftPart}{queryParams}";
    }

    private static readonly List<string> SecretParams = new()
    {
        "key",
        "token",
        "secret",
        "pass",
        "pswd"
    };
}
