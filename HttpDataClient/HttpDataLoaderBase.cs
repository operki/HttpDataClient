using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using HttpDataClient.Environment;
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
    private readonly string cookiesPath;
    private readonly TrackEnvironment environment;
    private readonly HttpDataFactory httpDataFactory;
    private readonly bool onlyHttps;
    private readonly HttpDataLoaderSettings settings;
    private readonly DownloadStrategyFileName strategyFileName;

    ~HttpDataLoader()
    {
        environment.Metrics.Flush();
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
        var environment = httpRequest.Environment;
        var httpDataFactory = httpRequest.HttpDataFactory;
        var traceId = httpRequest.TraceId;
        var url = httpRequest.Url;

        var (getResult, response, elapsed) = await GetWithRetriesInternalAsync(httpRequest, () => httpDataFactory.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead));
        var result = new DataResult(response);
        switch(getResult)
        {
            case GetResult.Fail:
                environment.Log.Info($"{IdGenerator.GetPrefixAnyway(traceId)}Failed get '{url.HideSecrets()}'");
                break;
            case GetResult.Success:
                environment.Log.Info($"{IdGenerator.GetPrefixAnyway(traceId)}Get '{url.HideSecrets()}' ({(int)response.StatusCode} {response.StatusCode}): result length {result.Content?.Length}, elapsed {elapsed}");
                break;
            case GetResult.StopException:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return result;
    }

    private static async Task<DataResult> PostAsyncInternal(HttpRequest httpRequest, byte[] body)
    {
        var environment = httpRequest.Environment;
        var httpDataFactory = httpRequest.HttpDataFactory;
        var traceId = httpRequest.TraceId;
        var url = httpRequest.Url;

        var (getResult, response, elapsed) = await GetWithRetriesInternalAsync(httpRequest, () =>
        {
            var httpContent = new ByteArrayContent(body);
            httpDataFactory.ModifyContent?.Invoke(httpContent);
            return httpDataFactory.Client.PostAsync(url, httpContent);
        });
        var result = new DataResult(response);
        switch(getResult)
        {
            case GetResult.Fail:
                environment.Log.Info($"{IdGenerator.GetPrefixAnyway(traceId)}Failed post '{url.HideSecrets()}'");
                break;
            case GetResult.Success:
                environment.Log.Info($"{IdGenerator.GetPrefixAnyway(traceId)}Post '{url.HideSecrets()}' ({(int)response.StatusCode} {response.StatusCode}): result length {result.Content?.Length}, elapsed {elapsed}");
                break;
            case GetResult.StopException:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return result;
    }

    private async Task<HttpStreamResult> GetStreamAsync(string url, string fileName = null, string traceId = null)
    {
        return await GetStreamAsyncInternal(new HttpRequest(environment, settings, traceId, url, httpDataFactory), strategyFileName, fileName);
    }

    private static async Task<HttpStreamResult> GetStreamAsyncInternal(HttpRequest httpRequest, DownloadStrategyFileName strategyFileName = DownloadStrategyFileName.PathGet, string fileName = null)
    {
        var environment = httpRequest.Environment;
        var httpDataFactory = httpRequest.HttpDataFactory;
        var traceId = httpRequest.TraceId;
        var url = httpRequest.Url;

        var tracePrefix = IdGenerator.GetPrefixAnyway(traceId);
        var tmpFileName = GetFileName(strategyFileName, url, fileName);
        long totalSize = 0;

        environment.Log.Info($"{tracePrefix}Start download from '{url.HideSecrets()}'...");
        var sw = Stopwatch.StartNew();
        while(true)
        {
            var resumeDownload = File.Exists(tmpFileName);
            if(resumeDownload)
            {
                totalSize = new FileInfo(tmpFileName).Length;
                environment.Log.Info($"{tracePrefix}Already downloaded {totalSize} bytes from '{url.HideSecrets()}'. Continue download...");
            }

            var (getResult, response, _) = await GetWithRetriesInternalAsync(httpRequest, () =>
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

            switch(getResult)
            {
                case GetResult.StopException:
                    return totalSize == 0
                        ? new HttpStreamResult(response)
                        : new HttpStreamResult(tmpFileName, response, true);
                case GetResult.Fail:
                    return new HttpStreamResult(response);
                case GetResult.Success:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            bool endDownload;
            using(var httpReadStream = response.Content.ReadAsStreamAsync())
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
            var result = new HttpStreamResult(tmpFileName, response);
            environment.Log.Info($"{tracePrefix}Downloaded '{url.HideSecrets()}' ({(int)result.ResponseMessage.StatusCode} {result.ResponseMessage.StatusCode}): result length {result.Length}, elapsed {elapsed}, rate {(decimal)(result.Length / (elapsed.TotalSeconds + 0.0001) / 1000000)::0.00 MB/s}");
            return result;
        }
    }

    private static string GetFileName(DownloadStrategyFileName strategyFileName, string url, string fileName)
    {
        if(fileName.IsSignificant())
            return Path.Combine(TempDir, LocalHelper.GetSafeFileName(fileName));

        return strategyFileName switch
        {
            DownloadStrategyFileName.PathGet => Path.Combine(TempDir, LocalHelper.GetSafeFileName(url)),
            DownloadStrategyFileName.Random => Path.Combine(TempDir, Guid.NewGuid().ToString()),
            DownloadStrategyFileName.Specify => throw new Exception("FileName must be significant or use other download strategy"),
            _ => throw new ArgumentOutOfRangeException(nameof(strategyFileName), strategyFileName, null)
        };
    }

    private static async Task<(GetResult, HttpResponseMessage, TimeSpan?)> GetWithRetriesInternalAsync(HttpRequest httpRequest, Func<Task<HttpResponseMessage>> httpGetter)
    {
        var environment = httpRequest.Environment;
        var httpDataFactory = httpRequest.HttpDataFactory;
        var loadStatCalc = httpRequest.LoadStatCalc;
        var traceId = httpRequest.TraceId;
        var url = httpRequest.Url;
        var onlyHttps = httpRequest.OnlyHttps;
        var preLoadTimeout = httpRequest.PreLoadTimeout;
        var retriesCount = httpRequest.RetriesCount;
        var stopDownload = httpRequest.StopDownload;

        var tracePrefix = IdGenerator.GetPrefixAnyway(traceId);
        Directory.CreateDirectory(TempDir);
        HttpResponseMessage response = null;
        var getResult = GetResult.Fail;
        try
        {
            UrlCheckOrThrow(httpDataFactory.BaseUrl, url, onlyHttps);
            var sleepTime = preLoadTimeout;
            for(var i = 0; i < retriesCount; i++)
            {
                if(getResult == GetResult.StopException)
                    break;

                Exception exception = null;
                Thread.Sleep(sleepTime);
                loadStatCalc?.Inc();
                environment.Metrics.Inc(DownloadMetrics.UrlTotalRequests);
                var sw = Stopwatch.StartNew();
                try
                {
                    response = await httpGetter.Invoke();

                    if(!response.IsSuccessStatusCode)
                    {
                        exception = new HttpRequestException($"{tracePrefix}({(int)response.StatusCode} {response.StatusCode})");
                    }
                    else
                    {
                        environment.Metrics.Inc(DownloadMetrics.UrlGoodRequests);
                        return (GetResult.Success, response, sw.Elapsed);
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
                        environment.Metrics.Inc(DownloadMetrics.UrlBadRequests);
                        if(stopDownload != null && stopDownload.Invoke(exception))
                        {
                            getResult = GetResult.StopException;
                            environment.Log.Info($"{tracePrefix}Stop '{url.HideSecrets()}': elapsed {sw.Elapsed}", exception);
                        }
                        else
                        {
                            sleepTime = preLoadTimeout * ((i > RetriesStopGrowing ? RetriesStopGrowing : i) + 2);
                            environment.Log.Error($"{tracePrefix}Failed '{url.HideSecrets()}': elapsed {sw.Elapsed}, try again after {sleepTime} milliseconds", exception);
                        }
                    }
                }
            }

            return (getResult, response, null);
        }
        catch(Exception e)
        {
            environment.Log.Fatal($"{tracePrefix}Failed '{url.HideSecrets()}'", e);
            return (GetResult.Fail, response, null);
        }
    }

    private static void UrlCheckOrThrow(string baseSite, string url, bool onlyHttps)
    {
        var uri = HttpDataFactory.GetUri(url, out var uriKind);
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
        {
            throw new Exception($"Can't get request with '{url}', only site '{baseSite}' allowed");
        }
    }

    private enum GetResult
    {
        Fail,
        Success,
        StopException
    }
}
