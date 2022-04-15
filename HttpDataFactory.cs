using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;

namespace Bingo.DataMining_Utils.HttpDataClient;

/// <summary>
/// Обработчик HttpClient'ов, ставит стандартные хедеры маскируясь под браузер
/// </summary>
public class HttpDataFactory : IHttpClientFactory
{
	public readonly HttpClient Client;
	public readonly HttpClientHandler ClientHandler;

	private readonly Action<HttpClient> modifyClient;
	private readonly Uri baseUri;
	private readonly int downloadTimeout;

	public HttpDataFactory(int downloadTimeout, Uri baseUri = null, IWebProxy proxy = null, CookieContainer cookieContainer = null, Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> certValidation = null, X509Certificate2 serverCert = null, ICredentials credentials = null, HttpClientHandler clientHandler = null, Action<HttpClient> modifyClient = null)
	{
		this.baseUri = baseUri;
		certValidation ??= serverCert == null
			? CertValidationDefault
			: new Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool>((_, certificate2, _, _) =>
				certificate2.Equals(serverCert));
		ClientHandler = clientHandler ??
						new HttpClientHandler
						{
							AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
							AllowAutoRedirect = true,
							CookieContainer = cookieContainer ?? new CookieContainer(),
							Credentials = credentials,
							MaxAutomaticRedirections = 3,
							Proxy = proxy,
							ServerCertificateCustomValidationCallback = certValidation,
							UseProxy = proxy != null
						};
		this.modifyClient = modifyClient;
		this.downloadTimeout = downloadTimeout;
		Client = CreateClient(null);
	}

	public HttpClient CreateClient(string _)
	{
		var client = new HttpClient(ClientHandler)
		{
			BaseAddress = baseUri,
			Timeout = TimeSpan.FromMilliseconds(downloadTimeout)
		};
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
		client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.82 Safari/537.36");
		modifyClient?.Invoke(client);
		return client;
	}

	private static bool CertValidationDefault(HttpRequestMessage f, X509Certificate2 ff, X509Chain fff, SslPolicyErrors sslErrors)
	{
		return sslErrors == SslPolicyErrors.None;
	}
}
