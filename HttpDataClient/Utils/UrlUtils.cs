using System.Web;

namespace DataTools.Utils;

internal static class UrlUtils
{
	private static readonly List<string> SecretParams = new()
	{
		"key",
		"token",
		"secret",
		"pass",
		"pswd"
	};

	public static void UrlCheckOrThrow(string? baseSite, string url, bool onlyHttps)
	{
		var uri = HttpDataFactory.GetUri(url, out var uriKind);
		if(!baseSite.IsSignificant())
		{
			if(uriKind == UriKind.Relative)
				throw new Exception($"Can't get request with '{url}', need absolute path");

			if(uri.Scheme == Uri.UriSchemeHttps)
				return;

			if(onlyHttps)
				throw new Exception($"Can't get request with '{url}', only {Uri.UriSchemeHttps} allowed");
			if(uri.Scheme != Uri.UriSchemeHttp)
				throw new Exception($"Can't get request with '{url}', only {Uri.UriSchemeHttp} and {Uri.UriSchemeHttps} allowed");
		}
		else if(uriKind == UriKind.Absolute && uri.GetLeftPart(UriPartial.Authority) != baseSite)
		{
			throw new Exception($"Can't get request with '{url}', only site '{baseSite}' allowed");
		}
	}

	public static string? GetHost(string? url)
	{
		return IsCorrectUrl(url)
			? new Uri(url!).Host
			: null;
	}

	private static bool IsCorrectUrl(string? url)
	{
		return Uri.TryCreate(url, UriKind.Absolute, out _);
	}

	public static string HideSecrets(this string url, bool needHideSecretsFromUrls)
	{
		if(!needHideSecretsFromUrls || !url.IsSignificant())
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
}
