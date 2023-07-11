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
	public bool HideSecretsFromUrls { get; }
	public int PreLoadTimeout { get; }
	public int RetriesCount { get; }
	public Func<Exception, bool>? StopDownload { get; } = exception => exception.ToString().Contains("416");

	public HttpRequest(HttpDataLoaderSettings settings, LoadStatCalc? loadStatCalc, string? traceId, string url, HttpDataFactory? httpDataFactory = null)
	{
		LogProvider = settings.LogProvider;
		MetricProvider = settings.MetricProvider;
		HttpDataFactory = httpDataFactory ?? new HttpDataFactory(settings);
		LoadStatCalc = loadStatCalc;
		TraceId = traceId;
		Url = url;
		OnlyHttps = settings.OnlyHttps;
		HideSecretsFromUrls = settings.HideSecretsFromUrls;
		PreLoadTimeout = settings.PreLoadTimeout;
		RetriesCount = settings.RetriesCount;
	}
}
