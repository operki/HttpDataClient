using System.Diagnostics;
using HttpDataClient.Consts;
using HttpDataClient.Helpers;
using HttpDataClient.Requests;
using HttpDataClient.Results;

namespace HttpDataClient;

internal static class HttpDataLoaderRetries
{
    public static async Task<HttpResponse> GetAsync(HttpRequest httpRequest, Func<Task<HttpResponseMessage>> httpGetter)
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
        LocalHelper.CreateTempDir();
        var response = new HttpResponse(HttpResponseResult.Fail, null, null);
        try
        {
            UrlHelper.UrlCheckOrThrow(httpDataFactory.BaseUrl, url, onlyHttps);
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
                            sleepTime = preLoadTimeout * ((i > GlobalConsts.HttpDataLoaderRetriesStopGrowing ? GlobalConsts.HttpDataLoaderRetriesStopGrowing : i) + 2);
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
}
