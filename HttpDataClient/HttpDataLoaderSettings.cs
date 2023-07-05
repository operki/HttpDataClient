using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using HttpDataClient.Consts;
using HttpDataClient.Providers;

namespace HttpDataClient;

/// <summary>
///     Settings for HttpClientFactory and HttpDataLoader
/// </summary>
public class HttpDataLoaderSettings
{

	/// <summary>
	///     Provider for writing logs
	/// </summary>
	public ILogProvider? LogProvider { get; set; }

	/// <summary>
	///     Provider for writing metrics
	/// </summary>
	public IMetricProvider? MetricProvider { get; set; }

	/// <summary>
	///     Add prefix to query requests. If baseUrl exists and base host in url is different will throw exception
	/// </summary>
	public string? BaseUrl { get; set; }

	/// <summary>
	///     Strategy of naming file for downloads with methods returns HttpStreamResult
	///     PathGet - file will be named used Path.GetFileName(url), can download same file then connection lost
	///     Random - file will be named randomly, can avoid errors with parallel downloading, like: '01.01.2020\data.xml' and '05.06.2021\data.xml'
	///     Specify - require name of file for every request
	/// </summary>
	public DownloadStrategyFileName StrategyFileName { get; set; } = DownloadStrategyFileName.PathGet;

	/// <summary>
	///     Allow download only with https://
	/// </summary>
	public bool OnlyHttps { get; set; } = true;

	/// <summary>
	///     Proxy for all requests
	/// </summary>
	public IWebProxy? Proxy { get; set; } = null;

	/// <summary>
	///     Timeout for one internal request
	/// </summary>
	public int DownloadTimeout { get; set; } = GlobalConsts.HttpDataLoaderSettingsDownloadTimeoutDefault;

	/// <summary>
	///     true - use standard settings for HttpClient, imitate chrome web browser
	///     false - don't use standard settings for HttpClient, use it for some special requests or requests to api
	/// </summary>
	public bool UseDefaultBrowserSettings { get; set; } = true;

	/// <summary>
	///     Using in initialize client
	/// </summary>
	public CookieContainer? CookieContainer { get; set; } = null;

	/// <summary>
	///     Local path to cookies, apply on initialize if CookieContainer not specified and for save cookies then Dispose
	/// </summary>
	public string? CookiesPath { get; set; }

	/// <summary>
	///     Custom server certificate, don't apply if exists special SslValidation
	/// </summary>
	public X509Certificate2? ServerCert { get; set; } = null;

	/// <summary>
	///     Response validation
	/// </summary>
	public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool>? SslValidation { get; set; } = null;

	/// <summary>
	///     Modification for HttpClientHandler, using one time with initialize HttpClientFactory
	/// </summary>
	public Action<HttpClientHandler>? ModifyClientHandler { get; set; } = null;

	/// <summary>
	///     Modification for HttpClient, using one time with initialize HttpClientFactory
	/// </summary>
	public Action<HttpClient>? ModifyClient { get; set; } = null;

	/// <summary>
	///     Modification for HttpClient, using one time for every post request. In example, you can specify ContentType like this:
	///     content => content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded")
	/// </summary>
	public Action<ByteArrayContent>? ModifyContent { get; set; } = null;

	/// <summary>
	///     Credentials for HttpClientHandler, using one time with initialize HttpClientFactory
	/// </summary>
	public ICredentials? Credentials { get; set; } = null;

	/// <summary>
	///     Timeout before send request, can limit traffic to source host, can be changed after initialize
	/// </summary>
	public int PreLoadTimeout { get; set; } = GlobalConsts.HttpDataLoaderSettingsPreLoadTimeoutDefault;

	/// <summary>
	///     Retries count before request returns error, can be changed after initialize
	/// </summary>
	public int RetriesCount { get; set; } = GlobalConsts.HttpDataLoaderSettingsRetriesCountDefault;
}
