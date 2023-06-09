using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using HttpDataClient.Environment.Logs;
using HttpDataClient.Environment.Metrics;
using HttpDataClient.Helpers;
using HttpDataClient.Requests;
using HttpDataClient.Results;

namespace HttpDataClient;

public partial class HttpDataLoader
{
    private const string TempDir = "tempDownloads";
    private const int SkipFilesWhenClear = 20;
    private const int RetriesStopGrowing = 8;
    private const int MaxReadLength = 1048576 * 1024;
    private readonly string? cookiesPath;
    private readonly HttpDataFactory httpDataFactory;
    private readonly ILogProvider? logProvider;
    private readonly IMetricProvider? metricProvider;
    private readonly HttpDataLoaderSettings settings;
    private readonly DownloadStrategyFileName strategyFileName;

    ~HttpDataLoader()
    {
        metricProvider?.Flush();
        LocalHelper.TryClearDir(TempDir);
        SaveCookies();
    }

    private void SaveCookies()
    {
        if(cookiesPath == null)
            return;

        using var outStream = File.Create(cookiesPath);
        JsonSerializer.Serialize(outStream, httpDataFactory.ClientHandler.CookieContainer);
    }

    private static async Task<DataResult> GetAsyncInternal(HttpRequest httpRequest)
    {
        var logProvider = httpRequest.LogProvider;
        var httpDataFactory = httpRequest.HttpDataFactory;
        var traceId = httpRequest.TraceId;
        var url = httpRequest.Url;

        var httpResponse = await GetWithRetriesInternalAsync(httpRequest, () => httpDataFactory.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead));
        var responseResult = httpResponse.Result;
        var responseMessage = httpResponse.Message;

        var result = new DataResult(responseMessage);
        switch(responseResult)
        {
            case HttpResponseResult.Fail:
                logProvider?.Info($"{IdGenerator.GetPrefix(traceId)}Failed get '{url.HideSecrets()}'");
                break;
            case HttpResponseResult.Success:
                logProvider?.Info($"{IdGenerator.GetPrefix(traceId)}Get '{url.HideSecrets()}' ({(int)responseMessage!.StatusCode} {responseMessage.StatusCode}): result length {result.Content?.Length}, elapsed {httpResponse.ElapsedTime}");
                break;
            case HttpResponseResult.StopException:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return result;
    }

    private static async Task<DataResult> PostAsyncInternal(HttpRequest httpRequest, byte[] body)
    {
        var logProvider = httpRequest.LogProvider;
        var httpDataFactory = httpRequest.HttpDataFactory;
        var traceId = httpRequest.TraceId;
        var url = httpRequest.Url;

        var httpResponse = await GetWithRetriesInternalAsync(httpRequest, () =>
        {
            var httpContent = new ByteArrayContent(body);
            httpDataFactory.ModifyContent?.Invoke(httpContent);
            return httpDataFactory.Client.PostAsync(url, httpContent);
        });
        var responseResult = httpResponse.Result;
        var responseMessage = httpResponse.Message;

        var result = new DataResult(responseMessage);
        switch(responseResult)
        {
            case HttpResponseResult.Fail:
                logProvider?.Info($"{IdGenerator.GetPrefix(traceId)}Failed post '{url.HideSecrets()}'");
                break;
            case HttpResponseResult.Success:
                logProvider?.Info($"{IdGenerator.GetPrefix(traceId)}Post '{url.HideSecrets()}' ({(int)responseMessage!.StatusCode} {responseMessage.StatusCode}): result length {result.Content?.Length}, elapsed {httpResponse.ElapsedTime}");
                break;
            case HttpResponseResult.StopException:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return result;
    }

    private async Task<HttpStreamResult> GetStreamAsync(string url, string? fileName = null, string? traceId = null)
    {
        return await GetStreamAsyncInternal(new HttpRequest(logProvider, metricProvider, settings, traceId, url, httpDataFactory), strategyFileName, fileName);
    }

    private static async Task<HttpStreamResult> GetStreamAsyncInternal(HttpRequest httpRequest, DownloadStrategyFileName strategyFileName = DownloadStrategyFileName.PathGet, string? fileName = null)
    {
        var logProvider = httpRequest.LogProvider;
        var httpDataFactory = httpRequest.HttpDataFactory;
        var traceId = httpRequest.TraceId;
        var url = httpRequest.Url;

        var tracePrefix = IdGenerator.GetPrefix(traceId);
        var tmpFileName = GetFileName(strategyFileName, url, fileName);
        long totalSize = 0;

        logProvider?.Info($"{tracePrefix}Start download from '{url.HideSecrets()}'...");
        var sw = Stopwatch.StartNew();
        while(true)
        {
            var resumeDownload = File.Exists(tmpFileName);
            if(resumeDownload)
            {
                totalSize = new FileInfo(tmpFileName).Length;
                logProvider?.Info($"{tracePrefix}Already downloaded {totalSize} bytes from '{url.HideSecrets()}'. Continue download...");
            }

            var httpResponse = await GetWithRetriesInternalAsync(httpRequest, () =>
            {
                try
                {
                    var currentRequest = new HttpRequestMessage(HttpMethod.Get, url);
                    currentRequest.Headers.Range = resumeDownload
                        ? new RangeHeaderValue(new FileInfo(tmpFileName).Length, new FileInfo(tmpFileName).Length + MaxReadLength)
                        : new RangeHeaderValue(0, MaxReadLength);

                    return httpDataFactory.Client.SendAsync(currentRequest);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }).ConfigureAwait(false);
            var responseResult = httpResponse.Result;
            var responseMessage = httpResponse.Message;

            switch(responseResult)
            {
                case HttpResponseResult.StopException:
                    return totalSize == 0
                        ? new HttpStreamResult(responseMessage)
                        : new HttpStreamResult(tmpFileName, responseMessage, true);
                case HttpResponseResult.Fail:
                    return new HttpStreamResult(responseMessage);
                case HttpResponseResult.Success:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            bool endDownload;
            using(var httpReadStream = responseMessage!.Content.ReadAsStreamAsync())
            {
                using(var fileWriteStream = File.Open(tmpFileName, FileMode.Append))
                {
                    var data = new byte[MaxReadLength];
                    var dataLength = await httpReadStream.Result.ReadAsync(data, 0, data.Length);
                    fileWriteStream.Write(data, 0, dataLength);
                    totalSize += dataLength;
                    endDownload = dataLength < data.Length;
                }
            }

            if(!endDownload)
                continue;

            var elapsed = sw.Elapsed;
            var result = new HttpStreamResult(tmpFileName, responseMessage);
            var downloadRate = (decimal)(result.Length / (elapsed.TotalSeconds + 0.0001) / 1000000);
            logProvider?.Info(result.ResponseMessage == null
                ? $"{tracePrefix}Downloaded '{url.HideSecrets()}' Undefined error: result length {result.Length}, elapsed {elapsed}, rate {downloadRate::0.00 MB/s}"
                : $"{tracePrefix}Downloaded '{url.HideSecrets()}' ({(int)result.ResponseMessage.StatusCode} {result.ResponseMessage.StatusCode}): result length {result.Length}, elapsed {elapsed}, rate {downloadRate::0.00 MB/s}");
            return result;
        }
    }

    private static string GetFileName(DownloadStrategyFileName strategyFileName, string url, string? fileName)
    {
        if(fileName.IsSignificant())
            return Path.Combine(TempDir, LocalHelper.GetSafeFileName(fileName!));

        return strategyFileName switch
        {
            DownloadStrategyFileName.PathGet => Path.Combine(TempDir, LocalHelper.GetSafeFileName(url)),
            DownloadStrategyFileName.Random => Path.Combine(TempDir, Guid.NewGuid().ToString()),
            DownloadStrategyFileName.Specify => throw new Exception("FileName must be significant or use other download strategy"),
            _ => throw new ArgumentOutOfRangeException(nameof(strategyFileName), strategyFileName, null)
        };
    }

    private static async Task<HttpResponse> GetWithRetriesInternalAsync(HttpRequest httpRequest, Func<Task<HttpResponseMessage>> httpGetter)
    {
        var logProvider = httpRequest.LogProvider;
        var metricProvider = httpRequest.MetricProvider;
        var httpDataFactory = httpRequest.HttpDataFactory;
        var loadStatCalc = httpRequest.LoadStatCalc;
        var traceId = httpRequest.TraceId;
        var url = httpRequest.Url;
        var onlyHttps = httpRequest.OnlyHttps;
        var preLoadTimeout = httpRequest.PreLoadTimeout;
        var retriesCount = httpRequest.RetriesCount;
        var stopDownload = httpRequest.StopDownload;

        var tracePrefix = IdGenerator.GetPrefix(traceId);
        Directory.CreateDirectory(TempDir);
        var response = new HttpResponse(HttpResponseResult.Fail, null, null);
        try
        {
            UrlCheckOrThrow(httpDataFactory.BaseUrl, url, onlyHttps);
            var sleepTime = preLoadTimeout;
            for(var i = 0; i < retriesCount; i++)
            {
                if(response.Result == HttpResponseResult.StopException)
                    break;

                Exception? exception = null;
                Thread.Sleep(sleepTime);
                loadStatCalc?.Inc();
                metricProvider?.Inc(DownloadMetrics.UrlTotalRequests);
                var sw = Stopwatch.StartNew();
                try
                {
                    var responseMessage = await httpGetter.Invoke();
                    response.Message = responseMessage;
                    response.ElapsedTime = sw.Elapsed;

                    if(!responseMessage.IsSuccessStatusCode)
                    {
                        exception = new HttpRequestException($"{tracePrefix}({(int)responseMessage.StatusCode} {responseMessage.StatusCode})");
                    }
                    else
                    {
                        metricProvider?.Inc(DownloadMetrics.UrlGoodRequests);
                        response.Result = HttpResponseResult.Success;
                        return response;
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
                        metricProvider?.Inc(DownloadMetrics.UrlBadRequests);
                        if(stopDownload != null && stopDownload.Invoke(exception))
                        {
                            response.Result = HttpResponseResult.StopException;
                            logProvider?.Info($"{tracePrefix}Stop '{url.HideSecrets()}': elapsed {sw.Elapsed}", exception);
                        }
                        else
                        {
                            sleepTime = preLoadTimeout * ((i > RetriesStopGrowing ? RetriesStopGrowing : i) + 2);
                            logProvider?.Error($"{tracePrefix}Failed '{url.HideSecrets()}': elapsed {sw.Elapsed}, try again after {sleepTime} milliseconds", exception);
                        }
                    }
                }
            }

            return response;
        }
        catch(Exception e)
        {
            logProvider?.Fatal($"{tracePrefix}Failed '{url.HideSecrets()}'", e);
            return response;
        }
    }

    private static void UrlCheckOrThrow(string? baseSite, string url, bool onlyHttps)
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
}
