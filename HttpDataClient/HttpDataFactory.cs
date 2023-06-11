using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using HttpDataClient.Settings;

namespace HttpDataClient;

internal class HttpDataFactory : IHttpClientFactory
{
    private readonly Uri? baseUri;
    public readonly string? BaseUrl;
    public readonly HttpClient Client;
    public readonly HttpClientHandler ClientHandler;
    private readonly int downloadTimeout;
    private readonly Action<HttpClient>? modifyClient;
    public readonly Action<ByteArrayContent>? ModifyContent;
    private readonly bool useDefaultBrowserSettings;

    internal HttpDataFactory(HttpDataLoaderSettings settings)
    {
        downloadTimeout = settings.DownloadTimeout;
        modifyClient = settings.ModifyClient;
        ModifyContent = settings.ModifyContent;
        useDefaultBrowserSettings = settings.UseDefaultBrowserSettings;

        var sslValidation = settings.SslValidation ?? (settings.ServerCert == null
            ? CertValidationDefault
            : new Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool>((_, certificate2, _, _) =>
                certificate2.Equals(settings.ServerCert)));
        ClientHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            AllowAutoRedirect = true,
            CookieContainer = settings.CookieContainer ?? LoadCookies(settings.CookiesPath) ?? new CookieContainer(),
            Credentials = settings.Credentials,
            MaxAutomaticRedirections = 3,
            Proxy = settings.Proxy,
            ServerCertificateCustomValidationCallback = sslValidation,
            UseProxy = settings.Proxy != null
        };
        settings.ModifyClientHandler?.Invoke(ClientHandler);

        baseUri = null;
        if(settings.BaseUrl != null)
        {
            baseUri = GetUri(settings.BaseUrl, out var uriKind);
            if(uriKind == UriKind.Relative)
                throw new Exception($"Can't init HttpDataClient with '{baseUri}', need absolute path");

            BaseUrl = baseUri.GetLeftPart(UriPartial.Authority);
            if(baseUri.Scheme != Uri.UriSchemeHttps)
            {
                if(settings.OnlyHttps)
                    throw new Exception($"Can't init HttpDataClient with '{baseUri}', only {Uri.UriSchemeHttps} allowed with parameters");
                if(settings.Proxy != null)
                    throw new Exception($"Can't init HttpDataClient with '{baseUri}', only {Uri.UriSchemeHttps} allowed with proxy");
                if(baseUri.Scheme != Uri.UriSchemeHttp)
                    throw new Exception($"Can't init HttpDataClient with '{baseUri}', only {Uri.UriSchemeHttp} and {Uri.UriSchemeHttps} allowed");
            }
        }

        Client = CreateClient(string.Empty);
    }

    public HttpClient CreateClient(string _)
    {
        var client = new HttpClient(ClientHandler)
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromMilliseconds(downloadTimeout)
        };
        if(useDefaultBrowserSettings)
        {
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
            client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
            client.DefaultRequestHeaders.Add("Sec-CH-UA", "\" Not A;Brand\";v=\"99\", \"Chromium\";v=\"99\", \"Google Chrome\";v=\"99\"");
            client.DefaultRequestHeaders.Add("Sec-CH-UA-Mobile", "?0");
            client.DefaultRequestHeaders.Add("Sec-CH-UA-Platform", "Windows");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
        }

        modifyClient?.Invoke(client);
        return client;
    }

    internal static Uri GetUri(string url, out UriKind uriKind)
    {
        if(Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            uriKind = UriKind.Absolute;
            return uri;
        }

        if(Uri.TryCreate(url, UriKind.Relative, out uri))
        {
            uriKind = UriKind.Relative;
            return uri;
        }

        throw new Exception($"Incorrect uri: '{url}'");
    }

    private static CookieContainer? LoadCookies(string? cookiesPath)
    {
        if(cookiesPath == null || !File.Exists(cookiesPath))
            return null;

        using var stream = File.Open(cookiesPath, FileMode.Open);
        return JsonSerializer.Deserialize<CookieContainer>(stream);
    }

    private static bool CertValidationDefault(HttpRequestMessage f, X509Certificate2 ff, X509Chain fff, SslPolicyErrors sslErrors)
    {
        return sslErrors == SslPolicyErrors.None;
    }
}
