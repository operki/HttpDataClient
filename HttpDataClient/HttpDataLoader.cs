﻿using System.Net;
using HttpDataClient.Environment.Logs;
using HttpDataClient.Environment.Metrics;
using HttpDataClient.Helpers;
using HttpDataClient.Requests;
using HttpDataClient.Results;

namespace HttpDataClient;

/// <summary>
///     Загрузчик данных, оперирует get и post запросами
/// </summary>
public partial class HttpDataLoader
{
    public HttpDataLoader(HttpDataLoaderSettings? settings = null, ILogProvider? logProvider = null, IMetricProvider? metricProvider = null)
    {
        this.logProvider = logProvider;
        this.metricProvider = metricProvider;
        this.settings = settings ?? new HttpDataLoaderSettings();
        strategyFileName = this.settings.StrategyFileName;
        LocalHelper.TryClearDir(TempDir, strategyFileName == DownloadStrategyFileName.Random
            ? 0
            : SkipFilesWhenClear);

        cookiesPath = this.settings.CookiesPath;
        httpDataFactory = new HttpDataFactory(this.settings);
    }

    public CookieContainer CookieContainer => httpDataFactory.ClientHandler.CookieContainer;

	/// <summary>
	///     Обычный get-запрос
	/// </summary>
	/// <param name="onceLogProvider">Лог загрузчика</param>
	/// <param name="onceMetricProvider">Метрики загрузчика</param>
	/// <param name="url">Ссылка на скачивание, может быть без хоста если он указан в settings.BaseUrl</param>
	/// <param name="settings">Настройки для HttpDataFactory</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public static DataResult JustGet(string url, HttpDataLoaderSettings? settings = null, ILogProvider? onceLogProvider = null, IMetricProvider? onceMetricProvider = null, string? traceId = null)
    {
        settings ??= new HttpDataLoaderSettings();
        return GetAsyncInternal(new HttpRequest(onceLogProvider, onceMetricProvider, settings, traceId, url)).ConfigureAwait(false).GetAwaiter().GetResult();
    }

	/// <summary>
	///     Get-запрос, при неудаче бросает Exception
	/// </summary>
	/// <param name="onceLogProvider">Лог загрузчика</param>
	/// <param name="onceMetricProvider">Метрики загрузчика</param>
	/// <param name="url">Ссылка на скачивание, может быть без хоста если он указан в settings.BaseUrl</param>
	/// <param name="settings">Настройки для HttpDataFactory</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public static DataResult JustGetSuccess(string url, HttpDataLoaderSettings? settings = null, ILogProvider? onceLogProvider = null, IMetricProvider? onceMetricProvider = null, string? traceId = null)
    {
        settings ??= new HttpDataLoaderSettings();
        var result = GetAsyncInternal(new HttpRequest(onceLogProvider, onceMetricProvider, settings, traceId, url)).ConfigureAwait(false).GetAwaiter().GetResult();
        if(!result.IsSuccess)
            throw new Exception($"{IdGenerator.GetPrefix(traceId)}Can't download data from '{url}'");

        return result;
    }

	/// <summary>
	///     Обычный post-запрос
	/// </summary>
	/// <param name="onceLogProvider">Лог загрузчика</param>
	/// <param name="onceMetricProvider">Метрики загрузчика</param>
	/// <param name="url">Ссылка на скачивание, может быть без хоста если он указан в settings.BaseUrl</param>
	/// <param name="body">Тело запроса</param>
	/// <param name="settings">Настройки для HttpDataFactory</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public static DataResult JustPost(string url, byte[] body, HttpDataLoaderSettings? settings = null, ILogProvider? onceLogProvider = null, IMetricProvider? onceMetricProvider = null, string? traceId = null)
    {
        settings ??= new HttpDataLoaderSettings();
        return PostAsyncInternal(new HttpRequest(onceLogProvider, onceMetricProvider, settings, traceId, url), body).ConfigureAwait(false).GetAwaiter().GetResult();
    }

	/// <summary>
	///     Обычный get-запрос, при неудаче бросает Exception
	/// </summary>
	/// <param name="url">Ссылка на скачивание, хоста либо нет либо он должен совпадать с указанным в settings.BaseUrl</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public DataResult GetSuccess(string url, string? traceId = null)
    {
        var result = GetAsync(url, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
        if(!result.IsSuccess)
            throw new Exception($"{IdGenerator.GetPrefix(traceId)}Can't download data from '{url}'");

        return result;
    }

	/// <summary>
	///     Обычный get-запрос
	/// </summary>
	/// <param name="url">Ссылка на скачивание, хоста либо нет либо он должен совпадать с указанным в settings.BaseUrl</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public DataResult Get(string url, string? traceId = null)
    {
        return GetAsync(url, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
    }

	/// <summary>
	///     Асинхронный get-запрос
	/// </summary>
	/// <param name="url">Ссылка на скачивание, хоста либо нет либо он должен совпадать с указанным в settings.BaseUrl</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public async Task<DataResult> GetAsync(string url, string? traceId = null)
    {
        return await GetAsyncInternal(new HttpRequest(logProvider, metricProvider, settings, traceId, url, httpDataFactory));
    }

	/// <summary>
	///     Обычный post-запрос
	/// </summary>
	/// <param name="url">Ссылка на скачивание, хоста либо нет либо он должен совпадать с указанным в settings.BaseUrl</param>
	/// <param name="body">Тело запроса</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public DataResult PostSuccess(string url, byte[] body, string? traceId = null)
    {
        var result = PostAsync(url, body, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
        if(!result.IsSuccess)
            throw new Exception($"{IdGenerator.GetPrefix(traceId)}Can't download post data from '{url}'");

        return result;
    }

	/// <summary>
	///     Обычный post-запрос
	/// </summary>
	/// <param name="url">Ссылка на скачивание, хоста либо нет либо он должен совпадать с указанным в settings.BaseUrl</param>
	/// <param name="body">Тело запроса</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public DataResult Post(string url, byte[] body, string? traceId = null)
    {
        return PostAsync(url, body, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
    }

	/// <summary>
	///     Асинхронный post-запрос
	/// </summary>
	/// <param name="url">Ссылка на скачивание, хоста либо нет либо он должен совпадать с указанным в settings.BaseUrl</param>
	/// <param name="body">Тело запроса</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public async Task<DataResult> PostAsync(string url, byte[] body, string? traceId = null)
    {
        return await PostAsyncInternal(new HttpRequest(logProvider, metricProvider, settings, traceId, url, httpDataFactory), body);
    }

	/// <summary>
	///     Get-запрос, возвращающий Stream, бросает Exception при неудаче
	/// </summary>
	/// <param name="url">Ссылка на скачивание, хоста либо нет либо он должен совпадать с указанным в settings.BaseUrl</param>
	/// <param name="fileName">Имя скачиваемого файла, если не указан то будет выбран в соответствии с settings.StrategyFileName</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public HttpStreamResult GetStreamSuccess(string url, string? fileName = null, string? traceId = null)
    {
        var result = GetStreamAsync(url, fileName, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
        if(!result.IsSuccess)
            throw new Exception($"{IdGenerator.GetPrefix(traceId)}Can't download data from '{url}'");

        return result;
    }

	/// <summary>
	///     Get-запрос, возвращающий Stream
	/// </summary>
	/// <param name="url">Ссылка на скачивание, хоста либо нет либо он должен совпадать с указанным в settings.BaseUrl</param>
	/// <param name="fileName">Имя скачиваемого файла, если не указан то будет выбран в соответствии с settings.StrategyFileName</param>
	/// <param name="traceId">Префикс для логов, будет присвоен автоматически если не указан</param>
	/// <returns>Результат скачивания</returns>
	public HttpStreamResult GetStream(string url, string? fileName = null, string? traceId = null)
    {
        return GetStreamAsync(url, fileName, traceId).ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
