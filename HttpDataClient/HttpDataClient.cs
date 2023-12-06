using System.Net;
using System.Text.Json;
using DataTools.Consts;
using DataTools.LoadStat;
using DataTools.Requests;
using DataTools.Results;
using DataTools.Utils;

namespace DataTools;

/// <summary>
///     Http data loader, works with get and post requests
/// </summary>
public class HttpDataClient
{
	private readonly string? cookiesPath;
	private readonly HttpDataFactory httpDataFactory;
	private readonly LoadStatCalc? loadStatCalc;
	private readonly HttpDataClientSettings settings;
	private readonly DownloadStrategyFileName strategyFileName;

	public HttpDataClient(HttpDataClientSettings? settings = null)
	{
		this.settings = settings ?? new HttpDataClientSettings();
		strategyFileName = this.settings.StrategyFileName;
		LocalUtils.TryClearDir(strategyFileName);

		cookiesPath = this.settings.CookiesPath;
		httpDataFactory = new HttpDataFactory(this.settings);
		if(settings.BaseUrl != null && settings.LogProvider != null)
			loadStatCalc = new LoadStatCalc(settings.LogProvider, settings.BaseUrl);
	}

	private CookieContainer CookieContainer => httpDataFactory.ClientHandler.CookieContainer;

	~HttpDataClient()
	{
		settings.MetricProvider?.Flush();
		LocalUtils.TryClearDir();
		SaveCookies();
	}

	/// <summary>
	///     Simple get request
	/// </summary>
	/// <param name="url">Download url, can be without host if he exists in settings.BaseUrl</param>
	/// <param name="settings">Custom settings for HttpDataFactory</param>
	/// <param name="onceLogProvider">Provider for logs, can be null</param>
	/// <param name="onceMetricProvider">Provider for metrics, can be null</param>
	/// <param name="traceId">Prefix for logs, will be added automatic if is null</param>
	/// <returns>Download result</returns>
	public static DataResult JustGet(string url, HttpDataClientSettings? settings = null, string? traceId = null)
	{
		settings ??= new HttpDataClientSettings();
		return HttpDataClientInternal.GetAsync(new HttpRequest(settings, null, traceId, url)).ConfigureAwait(false).GetAwaiter().GetResult();
	}

	/// <summary>
	///     Simple get request, if failed throw exception
	/// </summary>
	/// <param name="url">Download url, can be without host if he exists in settings.BaseUrl</param>
	/// <param name="settings">Custom settings for HttpDataFactory</param>
	/// <param name="onceLogProvider">Provider for logs, can be null</param>
	/// <param name="onceMetricProvider">Provider for metrics, can be null</param>
	/// <param name="traceId">Prefix for logs, will be added automatic if is null</param>
	/// <returns>Download result</returns>
	public static DataResult JustGetSuccess(string url, HttpDataClientSettings? settings = null, string? traceId = null)
	{
		settings ??= new HttpDataClientSettings();
		var result = HttpDataClientInternal.GetAsync(new HttpRequest(settings, null, traceId, url)).ConfigureAwait(false).GetAwaiter().GetResult();
		if(!result.IsSuccess)
			throw new Exception($"{IdUtils.GetPrefix(traceId)}Can't download data from '{url}'");

		return result;
	}

	/// <summary>
	///     Simple post request
	/// </summary>
	/// <param name="url">Download url, can be without host if he exists in settings.BaseUrl</param>
	/// <param name="body">Body of request</param>
	/// <param name="settings">Custom settings for HttpDataFactory</param>
	/// <param name="onceLogProvider">Provider for logs, can be null</param>
	/// <param name="onceMetricProvider">Provider for metrics, can be null</param>
	/// <param name="traceId">Prefix for logs, will be added automatic if is null</param>
	/// <returns>Download result</returns>
	public static DataResult JustPost(string url, byte[] body, HttpDataClientSettings? settings = null, string? traceId = null)
	{
		settings ??= new HttpDataClientSettings();
		return HttpDataClientInternal.PostAsync(new HttpRequest(settings, null, traceId, url), body).ConfigureAwait(false).GetAwaiter().GetResult();
	}

	/// <summary>
	///     Simple get request, if failed throw exception
	/// </summary>
	/// <param name="url">Download url, can be without host if he exists in settings.BaseUrl</param>
	/// <param name="traceId">Prefix for logs, will be added automatic if is null</param>
	/// <returns>Download result</returns>
	public DataResult GetSuccess(string url, string? traceId = null)
	{
		var result = GetAsync(url, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
		if(!result.IsSuccess)
			throw new Exception($"{IdUtils.GetPrefix(traceId)}Can't download data from '{url}'");

		return result;
	}

	/// <summary>
	///     Simple get request
	/// </summary>
	/// <param name="url">Download url, can be without host if he exists in settings.BaseUrl</param>
	/// <param name="traceId">Prefix for logs, will be added automatic if is null</param>
	/// <returns>Download result</returns>
	public DataResult Get(string url, string? traceId = null)
	{
		return GetAsync(url, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
	}

	/// <summary>
	///     Simple async get request
	/// </summary>
	/// <param name="url">Download url, can be without host if he exists in settings.BaseUrl</param>
	/// <param name="traceId">Prefix for logs, will be added automatic if is null</param>
	/// <returns>Download result</returns>
	public async Task<DataResult> GetAsync(string url, string? traceId = null)
	{
		return await HttpDataClientInternal.GetAsync(new HttpRequest(settings, null, traceId, url, httpDataFactory));
	}

	/// <summary>
	///     Simple post request, if failed throw exception
	/// </summary>
	/// <param name="url">Download url, can be without host if he exists in settings.BaseUrl</param>
	/// <param name="body">Body of request</param>
	/// <param name="traceId">Prefix for logs, will be added automatic if is null</param>
	/// <returns>Download result</returns>
	public DataResult PostSuccess(string url, byte[] body, string? traceId = null)
	{
		var result = PostAsync(url, body, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
		if(!result.IsSuccess)
			throw new Exception($"{IdUtils.GetPrefix(traceId)}Can't download post data from '{url}'");

		return result;
	}

	/// <summary>
	///     Simple post request
	/// </summary>
	/// <param name="url">Download url, can be without host if he exists in settings.BaseUrl</param>
	/// <param name="body">Body of request</param>
	/// <param name="traceId">Prefix for logs, will be added automatic if is null</param>
	/// <returns>Download result</returns>
	public DataResult Post(string url, byte[] body, string? traceId = null)
	{
		return PostAsync(url, body, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
	}

	/// <summary>
	///     Simple async post request
	/// </summary>
	/// <param name="url">Download url, can be without host if he exists in settings.BaseUrl</param>
	/// <param name="body">Body of request</param>
	/// <param name="traceId">Prefix for logs, will be added automatic if is null</param>
	/// <returns>Download result</returns>
	public async Task<DataResult> PostAsync(string url, byte[] body, string? traceId = null)
	{
		return await HttpDataClientInternal.PostAsync(new HttpRequest(settings, loadStatCalc, traceId, url, httpDataFactory), body);
	}

	/// <summary>
	///     Simple get request, returns stream of data, if failed throw exception
	/// </summary>
	/// <param name="url">Download url, can be without host if he exists in settings.BaseUrl</param>
	/// <param name="fileName">Name of download file, if null will be sets depends on settings.StrategyFileName</param>
	/// <param name="traceId">Prefix for logs, will be added automatic if is null</param>
	/// <returns>Download stream result</returns>
	public HttpStreamResult GetStreamSuccess(string url, string? fileName = null, string? traceId = null)
	{
		var result = GetStreamAsync(url, fileName, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
		if(!result.IsSuccess)
			throw new Exception($"{IdUtils.GetPrefix(traceId)}Can't download data from '{url}'");

		return result;
	}

	/// <summary>
	///     Simple get request, returns stream of data
	/// </summary>
	/// <param name="url">Download url, can be without host if he exists in settings.BaseUrl</param>
	/// <param name="fileName">Name of download file, if null will be sets depends on settings.StrategyFileName</param>
	/// <param name="traceId">Prefix for logs, will be added automatic if is null</param>
	/// <returns>Download stream result</returns>
	public HttpStreamResult GetStream(string url, string? fileName = null, string? traceId = null)
	{
		return GetStreamAsync(url, fileName, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
	}

	private void SaveCookies()
	{
		if(cookiesPath == null)
			return;

		using var outStream = File.Create(cookiesPath);
		JsonSerializer.Serialize(outStream, httpDataFactory.ClientHandler.CookieContainer);
	}

	private async Task<HttpStreamResult> GetStreamAsync(string url, string? fileName = null, string? traceId = null)
	{
		return await HttpDataClientInternal.GetStreamAsync(new HttpRequest(settings, loadStatCalc, traceId, url, httpDataFactory), strategyFileName, fileName);
	}
}
