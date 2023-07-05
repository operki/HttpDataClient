using HttpDataClient.LoadStat;
using HttpDataClient.Providers;

namespace HttpDataClient.Requests;

internal struct HttpRequest
{
    public ILogProvider? LogProvider { get; }
    public IMetricProvider? MetricProvider { get; }
    public HttpDataFactory HttpDataFactory { get; }
    public LoadStatCalc? LoadStatCalc { get; }
    public string? TraceId { get; }
    public string Url { get; }
    public bool OnlyHttps { get; }
    public int PreLoadTimeout { get; }
    public int RetriesCount { get; }
    public Func<Exception, bool>? StopDownload { get; } = exception => exception.ToString().Contains("416");

    public HttpRequest(HttpDataLoaderSettings settings, string? traceId, string url, HttpDataFactory? httpDataFactory = null)
    {
        LogProvider = settings.LogProvider;
        MetricProvider = settings.MetricProvider;
        HttpDataFactory = httpDataFactory ?? new HttpDataFactory(settings);
        if(settings.BaseUrl != null && settings.LogProvider != null)
            LoadStatCalc = new LoadStatCalc(settings.LogProvider, settings.BaseUrl);
        TraceId = traceId;
        Url = url;
        OnlyHttps = settings.OnlyHttps;
        PreLoadTimeout = settings.PreLoadTimeout;
        RetriesCount = settings.RetriesCount;
    }
}
