using HttpDataClient.Providers;
using HttpDataClient.Settings;
using HttpDataClient.Settings.LoadStat;

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
    public Func<Exception, bool> StopDownload { get; } = exception => exception.ToString().Contains("416");

    public HttpRequest(ILogProvider? logProvider, IMetricProvider? metricProvider, HttpDataLoaderSettings settings, string? traceId, string url, HttpDataFactory? httpDataFactory = null)
    {
        LogProvider = logProvider;
        MetricProvider = metricProvider;
        HttpDataFactory = httpDataFactory ?? new HttpDataFactory(settings);
        LoadStatCalc = settings.LoadStatCalc;
        TraceId = traceId;
        Url = url;
        OnlyHttps = settings.OnlyHttps;
        PreLoadTimeout = settings.PreLoadTimeout;
        RetriesCount = settings.RetriesCount;
    }
}
