using System.Net;
using HttpDataClient.Environment;
using HttpDataClient.Helpers;
using HttpDataClient.Results;

namespace HttpDataClient;

/// <summary>
/// Загрузчик данных, оперирует get и post запросами
/// </summary>
public partial class HttpDataClient
{
	public int PreLoadTimeout { get; set; }
	public int RetriesCount { get; set; }

	public CookieContainer CookieContainer => httpDataFactory.ClientHandler.CookieContainer;

	public HttpDataClient(DefaultEnvironment environment, HttpClientSettings settings = null)
	{
		Environment = environment;
		settings ??= new HttpClientSettings();
		onlyHttps = settings.OnlyHttps || settings.Proxy != null;
		strategyFileName = settings.StrategyFileName;
		LocalHelper.TryClearDir(TempDir, strategyFileName == DownloadStrategyFileName.Random
			? 0
			: SkipFilesWhenClear);

		PreLoadTimeout = settings.PreLoadTimeout;
		RetriesCount = settings.RetriesCount;
		cookiesPath = settings.CookiesPath;
		httpDataFactory = new HttpDataFactory(settings);
	}

	/// <summary>
	/// Обычный get-запрос
	/// </summary>
	/// <param name="environment">Окружение курочки</param>
	/// <param name="url">Ссылка на скачивание, может быть без хоста если он указан в settings.BaseUrl</param>
	/// <param name="settings">Настройки для HttpDataFactory</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public static DataResult JustGet(DefaultEnvironment environment, string url, HttpClientSettings settings = null, string traceId = null)
	{
		settings ??= new HttpClientSettings();
		return GetAsyncInternal(environment, url, traceId, new HttpDataFactory(settings), settings.OnlyHttps, settings.PreLoadTimeout, settings.RetriesCount).ConfigureAwait(false).GetAwaiter().GetResult();
	}

	/// <summary>
	/// Get-запрос, при неудаче бросает Exception
	/// </summary>
	/// <param name="environment">Окружение курочки</param>
	/// <param name="url">Ссылка на скачивание, может быть без хоста если он указан в settings.BaseUrl</param>
	/// <param name="settings">Настройки для HttpDataFactory</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public static DataResult JustGetSuccess(DefaultEnvironment environment, string url, HttpClientSettings settings = null, string traceId = null)
	{
		settings ??= new HttpClientSettings();
		var result = GetAsyncInternal(environment, url, traceId, new HttpDataFactory(settings), settings.OnlyHttps, settings.PreLoadTimeout, settings.RetriesCount).ConfigureAwait(false).GetAwaiter().GetResult();
		if(!result.IsSuccess)
			throw new Exception($"{IdGenerator.GetPrefix(traceId)}Can't download data from '{url}'");

		return result;
	}

	/// <summary>
	/// Обычный post-запрос
	/// </summary>
	/// <param name="environment">Окружение курочки</param>
	/// <param name="url">Ссылка на скачивание, может быть без хоста если он указан в settings.BaseUrl</param>
	/// <param name="body">Тело запроса</param>
	/// <param name="settings">Настройки для HttpDataFactory</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public static DataResult JustPost(DefaultEnvironment environment, string url, byte[] body, HttpClientSettings settings = null, string traceId = null)
	{
		settings ??= new HttpClientSettings();
		return PostAsyncInternal(environment, url, body, traceId, new HttpDataFactory(settings), settings.OnlyHttps, settings.PreLoadTimeout, settings.RetriesCount).ConfigureAwait(false).GetAwaiter().GetResult();
	}

	/// <summary>
	/// Обычный get-запрос, при неудаче бросает Exception
	/// </summary>
	/// <param name="url">Ссылка на скачивание, хоста либо нет либо он должен совпадать с указанным в settings.BaseUrl</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public DataResult GetSuccess(string url, string traceId = null)
	{
		var result = GetAsync(url, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
		if(!result.IsSuccess)
			throw new Exception($"{IdGenerator.GetPrefix(traceId)}Can't download data from '{url}'");

		return result;
	}

	/// <summary>
	/// Обычный get-запрос
	/// </summary>
	/// <param name="url">Ссылка на скачивание, хоста либо нет либо он должен совпадать с указанным в settings.BaseUrl</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public DataResult Get(string url, string traceId = null)
	{
		return GetAsync(url, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
	}

	/// <summary>
	/// Асинхронный get-запрос
	/// </summary>
	/// <param name="url">Ссылка на скачивание, хоста либо нет либо он должен совпадать с указанным в settings.BaseUrl</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public async Task<DataResult> GetAsync(string url, string traceId = null)
	{
		return await GetAsyncInternal(Environment, url, traceId, httpDataFactory, onlyHttps, PreLoadTimeout, RetriesCount);
	}

	/// <summary>
	/// Обычный post-запрос
	/// </summary>
	/// <param name="url">Ссылка на скачивание, хоста либо нет либо он должен совпадать с указанным в settings.BaseUrl</param>
	/// <param name="body">Тело запроса</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public DataResult PostSuccess(string url, byte[] body, string traceId = null)
	{
		var result = PostAsync(url, body, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
		if(!result.IsSuccess)
			throw new Exception($"{IdGenerator.GetPrefix(traceId)}Can't download post data from '{url}'");

		return result;
	}

	/// <summary>
	/// Обычный post-запрос
	/// </summary>
	/// <param name="url">Ссылка на скачивание, хоста либо нет либо он должен совпадать с указанным в settings.BaseUrl</param>
	/// <param name="body">Тело запроса</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public DataResult Post(string url, byte[] body, string traceId = null)
	{
		return PostAsync(url, body, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
	}

	/// <summary>
	/// Асинхронный post-запрос
	/// </summary>
	/// <param name="url">Ссылка на скачивание, хоста либо нет либо он должен совпадать с указанным в settings.BaseUrl</param>
	/// <param name="body">Тело запроса</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public async Task<DataResult> PostAsync(string url, byte[] body, string traceId = null)
	{
		return await PostAsyncInternal(Environment, url, body, traceId, httpDataFactory, onlyHttps, PreLoadTimeout, RetriesCount);
	}

	/// <summary>
	/// Get-запрос, возвращающий Stream, бросает Exception при неудаче
	/// </summary>
	/// <param name="url">Ссылка на скачивание, хоста либо нет либо он должен совпадать с указанным в settings.BaseUrl</param>
	/// <param name="fileName">Имя скачиваемого файла, если не указан то будет выбран в соответствии с settings.StrategyFileName</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public HttpStreamResult GetStreamSuccess(string url, string fileName = null, string traceId = null)
	{
		var result = GetStreamAsync(url, fileName, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
		if(!result.IsSuccess)
			throw new Exception($"{IdGenerator.GetPrefix(traceId)}Can't download data from '{url}'");

		return result;
	}

	/// <summary>
	/// Get-запрос, возвращающий Stream
	/// </summary>
	/// <param name="url">Ссылка на скачивание, хоста либо нет либо он должен совпадать с указанным в settings.BaseUrl</param>
	/// <param name="fileName">Имя скачиваемого файла, если не указан то будет выбран в соответствии с settings.StrategyFileName</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public HttpStreamResult GetStream(string url, string fileName = null, string traceId = null)
	{
		return GetStreamAsync(url, fileName, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
	}
}
