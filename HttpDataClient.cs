using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bingo.Utils;
using Bingo.Utils.Chickens;
using JetBrains.Annotations;
using log4net;

namespace Bingo.DataMining_Utils.HttpDataClient;

public class HttpDataClientSettings
{
	public const int DownloadTimeoutDefault = 1_000 * 60 * 15;
	public const int PreLoadTimeoutDefault = 1_000;
	public const int RetriesCountDefault = 5;

	/// <summary>
	/// Добавляет префикс к запросам с  относительными путями
	/// </summary>
	public string BaseUrl { get; set; } = null;
	/// <summary>
	/// Не учитывается если указан Proxy
	/// </summary>
	public bool OnlyHttps { get; set; } = true;
	public IWebProxy Proxy { get; set; } = null;
	public int DownloadTimeout { get; set; } = DownloadTimeoutDefault;
	/// <summary>
	/// Используется при инициализации клиента
	/// </summary>
	public CookieContainer CookieContainer { get; set; } = null;
	/// <summary>
	/// Локальный путь к кукам, применяется при инициализации если не указан CookieContainer и при сохранении куков в Dispose
	/// </summary>
	public string CookiesPath { get; set; } = null;
	/// <summary>
	/// Сертификат сервера, не учитывается если указан SslValidation
	/// </summary>
	public X509Certificate2 ServerCert { get; set; } = null;
	public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> SslValidation { get; set; } = null;
	public HttpClientHandler ClientHandler { get; set; } = null;
	public Action<HttpClient> ModifyClient { get; set; } = null;
	public ICredentials Credentials { get; set; } = null;
	/// <summary>
	/// Можно менять после инициализации
	/// </summary>
	public int PreLoadTimeout { get; set; } = PreLoadTimeoutDefault;
	/// <summary>
	/// Можно менять после инициализации
	/// </summary>
	public int RetriesCount { get; set; } = RetriesCountDefault;
}

/// <summary>
/// Public part of HttpDataClient
/// </summary>
public partial class HttpDataClient
{
	public int PreLoadTimeout { get; set; }
	public int RetriesCount { get; set; }

	public CookieContainer CookieContainer => httpDataFactory.ClientHandler.CookieContainer;

	public HttpDataClient(HttpDataClientSettings settings)
	{
		TryClearTempDir();
		Uri baseUri = null;
		var proxy = settings.Proxy;
		onlyHttps = settings.OnlyHttps || proxy != null;
		PreLoadTimeout = settings.PreLoadTimeout;
		RetriesCount = settings.RetriesCount;
		cookiesPath = settings.CookiesPath;
		var cookieContainer = settings.CookieContainer ?? LoadCookies();

		if(settings.BaseUrl != null)
		{
			baseUri = GetUri(settings.BaseUrl, out var uriKind);
			if(uriKind == UriKind.Relative)
				throw new Exception($"Can't init HttpDataClient with '{baseUri}', need absolute path");

			site = baseUri.GetLeftPart(UriPartial.Authority);
			if(baseUri.Scheme != Uri.UriSchemeHttps)
			{
				if(onlyHttps)
					throw new Exception($"Can't init HttpDataClient with '{baseUri}', only {Uri.UriSchemeHttps} allowed with parameters");
				if(proxy != null)
					throw new Exception($"Can't init HttpDataClient with '{baseUri}', only {Uri.UriSchemeHttps} allowed with proxy");
				if(baseUri.Scheme != Uri.UriSchemeHttp)
					throw new Exception($"Can't init HttpDataClient with '{baseUri}', only {Uri.UriSchemeHttp} and {Uri.UriSchemeHttps} allowed");
			}
		}

		httpDataFactory = new HttpDataFactory(settings.DownloadTimeout, baseUri, proxy, cookieContainer, settings.SslValidation, settings.ServerCert, settings.Credentials, settings.ClientHandler, settings.ModifyClient);
	}

	public static HttpDataResult JustGet(string url, string traceId = null, bool onlyHttps = true, int downloadTimeout = HttpDataClientSettings.DownloadTimeoutDefault, int preLoadTimeout = HttpDataClientSettings.PreLoadTimeoutDefault, int retriesCount = HttpDataClientSettings.RetriesCountDefault)
	{
		return GetAsyncInternal(url, null, new HttpDataFactory(downloadTimeout), traceId, onlyHttps, preLoadTimeout, retriesCount).ConfigureAwait(false).GetAwaiter().GetResult();
	}

	public HttpDataResult Get(string url, string traceId = null)
	{
		return GetAsync(url, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
	}

	public async Task<HttpDataResult> GetAsync(string url, string traceId = null)
	{
		return await GetAsyncInternal(url, traceId, httpDataFactory, site, onlyHttps, PreLoadTimeout, RetriesCount);
	}

	public HttpDataResult Post(string url, byte[] body, string traceId = null)
	{
		return PostAsync(url, body, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
	}

	public async Task<HttpDataResult> PostAsync(string url, byte[] body, string traceId = null)
	{
		return await PostAsyncInternal(url, body, traceId, httpDataFactory, site, onlyHttps, PreLoadTimeout, RetriesCount);
	}

	public HttpStreamResult GetStream(string url, string traceId = null)
	{
		return GetStreamAsync(url, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
	}

	private async Task<HttpStreamResult> GetStreamAsync(string url, string traceId = null)
	{
		return await GetStreamAsyncInternal(url, traceId, httpDataFactory, site, onlyHttps, PreLoadTimeout, RetriesCount);
	}
}

/// <summary>
/// Private part of HttpDataClient
/// </summary>
public partial class HttpDataClient
{
	private const string TempDir = "tempDownloads";
	private const int RetriesStopGrowing = 8;
	private static readonly ILog Log = LogManager.GetLogger(typeof(HttpDataClient));
	private readonly HttpDataFactory httpDataFactory;
	private readonly string site;
	private readonly bool onlyHttps;
	private readonly string cookiesPath;

	~HttpDataClient()
	{
		TryClearTempDir();
		SaveCookies();
	}

	[CanBeNull] private CookieContainer LoadCookies()
	{
		if(cookiesPath == null || !File.Exists(cookiesPath))
			return null;

		using var stream = File.Open(cookiesPath, FileMode.Open);
		return (CookieContainer)new BinaryFormatter().Deserialize(stream);
	}

	private void SaveCookies()
	{
		if(cookiesPath == null)
			return;

		using var outStream = File.Create(cookiesPath);
		new BinaryFormatter().Serialize(outStream, httpDataFactory.ClientHandler.CookieContainer);
	}

	private static async Task<HttpDataResult> GetAsyncInternal(string url, string traceId, HttpDataFactory httpDataFactory, string site = null, bool onlyHttps = true, int preLoadTimeout = HttpDataClientSettings.PreLoadTimeoutDefault, int retriesCount = HttpDataClientSettings.RetriesCountDefault)
	{
		var tracePrefix = GetTracePrefix(traceId);
		var (response, elapsed) = await GetWithRetriesInternalAsync(() => httpDataFactory.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead), tracePrefix, url, httpDataFactory, site, onlyHttps, preLoadTimeout, retriesCount);
		var result = new HttpDataResult(response);
		Log.Info($"{tracePrefix}Get '{url}' ({(int)response.StatusCode} {response.StatusCode}): result length {result.Content?.Length}, elapsed {elapsed}");
		return result;
	}

	private static async Task<HttpDataResult> PostAsyncInternal(string url, byte[] body, string traceId, HttpDataFactory httpDataFactory, string site = null, bool onlyHttps = true, int preLoadTimeout = HttpDataClientSettings.PreLoadTimeoutDefault, int retriesCount = HttpDataClientSettings.RetriesCountDefault)
	{
		var tracePrefix = GetTracePrefix(traceId);
		var (response, elapsed) = await GetWithRetriesInternalAsync(() => httpDataFactory.Client.PostAsync(url, new ByteArrayContent(body)), tracePrefix, url, httpDataFactory, site, onlyHttps, preLoadTimeout, retriesCount);
		var result = new HttpDataResult(response);
		Log.Info($"{tracePrefix}Post '{url}' ({(int)response.StatusCode} {response.StatusCode}): result length {result.Content?.Length}, elapsed {elapsed}");
		return result;
	}

	private static async Task<HttpStreamResult> GetStreamAsyncInternal(string url, string traceId, HttpDataFactory httpDataFactory, string site = null, bool onlyHttps = true, int preLoadTimeout = HttpDataClientSettings.PreLoadTimeoutDefault, int retriesCount = HttpDataClientSettings.RetriesCountDefault)
	{
		var tracePrefix = GetTracePrefix(traceId);
		var tmpFileName = Path.Combine(TempDir, Guid.NewGuid().ToString());

		Log.Info($"{tracePrefix}Start download from '{url}'...");
		var (response, elapsedHeaders) = await GetWithRetriesInternalAsync(() => httpDataFactory.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead), tracePrefix, url, httpDataFactory, site, onlyHttps, preLoadTimeout, retriesCount);
		if(elapsedHeaders == null)
			return new HttpStreamResult(response);

		var sw = Stopwatch.StartNew();
		long totalSize;
		using(var httpReadStream = await response.Content.ReadAsStreamAsync())
		{
			using(var fileWriteStream = File.Open(tmpFileName, FileMode.Create))
			{
				totalSize = httpReadStream.CopyToUpTo(fileWriteStream, long.MaxValue);
			}
		}

		var elapsed = sw.Elapsed + elapsedHeaders.Value;
		Log.Info($"{tracePrefix}Downloaded '{url}' ({(int)response.StatusCode} {response.StatusCode}): result length {totalSize}, elapsed {elapsed}, rate {(decimal)(totalSize / (elapsed.TotalSeconds + 0.0001) / 1000000)::0.00 MB/s}");

		var result = new HttpStreamResult(response)
		{
			Stream = File.OpenRead(tmpFileName)
		};
		return result;
	}

	private static string GetTracePrefix(string traceId)
	{
		return traceId == null
			? null
			: $"[{traceId}] ";
	}

	private static async Task<(HttpResponseMessage, TimeSpan?)> GetWithRetriesInternalAsync(Func<Task<HttpResponseMessage>> httpGetter, string tracePrefix, string url, HttpDataFactory httpDataFactory, string baseSite, bool onlyHttps, int preLoadTimeout, int retriesCount)
	{
		HttpResponseMessage response = null;
		try
		{
			UrlCheckOrThrow(baseSite, url, onlyHttps);
			var sleepTime = preLoadTimeout;
			for(var i = 0; i < retriesCount; i++)
			{
				Exception exception = null;
				ChickenMetrics.Add(ChickenMetrics.DefaultMetrics.urlTotalRequests, 1);
				Thread.Sleep(sleepTime);
				var sw = Stopwatch.StartNew();
				try
				{
					response = await httpGetter.Invoke();

					if(!response.IsSuccessStatusCode)
					{
						using var httpReadStream = await response.Content.ReadAsStreamAsync();
						var contentBuffer = new byte[2048];
						await httpReadStream.ReadAsync(contentBuffer, 0, 2048);
						exception = new Exception($"{tracePrefix}({(int)response.StatusCode} {response.StatusCode}), content: {Encoding.UTF8.GetString(contentBuffer)}...");
					}
					else
					{
						ChickenMetrics.Add(ChickenMetrics.DefaultMetrics.urlGoodRequests, 1);
						return (response, sw.Elapsed);
					}
				}
				catch(Exception e)
				{
					sw.Stop();
					exception = e;
				}
				finally
				{
					sw.Stop();
					if(exception != null)
					{
						ChickenMetrics.Add(ChickenMetrics.DefaultMetrics.urlBadRequests, 1);
						sleepTime = preLoadTimeout * ((i > RetriesStopGrowing ? RetriesStopGrowing : i) + 2);
						Log.Error($"{tracePrefix}Failed '{url}': elapsed {sw.Elapsed}, try again after {sleepTime} milliseconds", exception);
					}
				}
			}

			return (response, null);
		}
		catch(Exception e)
		{
			Log.Fatal($"{tracePrefix}Failed '{url}'", e);
			return (response, null);
		}
	}

	private static Uri GetUri(string url, out UriKind uriKind)
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

	private static void UrlCheckOrThrow(string baseSite, string url, bool onlyHttps)
	{
		var uri = GetUri(url, out var uriKind);
		if(baseSite == null)
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
			throw new Exception($"Can't get request with '{url}', only site '{baseSite}' allowed");
	}

	private static void TryClearTempDir()
	{
		try
		{
			if(Directory.Exists(TempDir))
				Directory.GetFiles(TempDir)
					.ForEach(File.Delete);
		}
		catch(UnauthorizedAccessException) { }
		catch(IOException) { }
		catch(Exception e)
		{
			Log.Fatal($"Can't clear folder '{TempDir}'. Exception: {e}");
		}
	}
}

/// <summary>
/// Ответ по урлу, хранится в памяти
/// </summary>
public class HttpDataResult
{
	public HttpDataResult(HttpResponseMessage responseMessage)
	{
		ResponseMessage = responseMessage;
	}

	public HttpResponseMessage ResponseMessage { get; }
	public bool IsSuccess => ResponseMessage is { IsSuccessStatusCode: true };
	public string Content => ResponseMessage?.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
	public byte[] Data => ResponseMessage?.Content.ReadAsByteArrayAsync().ConfigureAwait(false).GetAwaiter().GetResult();
}

/// <summary>
/// Ответ по урлу, хранится на диске в папке tempDownloads, доступ по Stream
/// </summary>
public class HttpStreamResult
{
	public HttpStreamResult(HttpResponseMessage responseMessage)
	{
		ResponseMessage = responseMessage;
	}

	public HttpResponseMessage ResponseMessage { get; }
	public bool IsSuccess => ResponseMessage is { IsSuccessStatusCode: true };
	public Stream Stream;
}

