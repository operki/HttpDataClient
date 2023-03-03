using System.Collections.Concurrent;
using HttpDataClient;
using HttpDataClient.Providers;
using log4net;

namespace HttpDataClientExample.Log4NetProviders;

public class MetricProvider : IMetricProvider
{
    private readonly ILog logger;

    public MetricProvider(ILog logger)
    {
        this.logger = logger;
    }

    private readonly ConcurrentDictionary<string, long> metricsStorage = new();

    public void Inc(DownloadMetrics key)
    {
        Inc(ToLowerFirstChar(key.ToString()));
    }

    public void Add(DownloadMetrics key, long addValue)
    {
        Add(ToLowerFirstChar(key.ToString()), addValue);
    }

    public void Inc(string key)
    {
        metricsStorage.AddOrUpdate(key, 1, (_, value) => value + 1);
    }

    public void Add(string key, long addValue)
    {
        metricsStorage.AddOrUpdate(key, addValue, (_, value) => value + addValue);
    }

    public void Flush()
    {
        foreach(var key in metricsStorage.Keys.OrderBy(key => key))
            if(metricsStorage.TryRemove(key, out var value))
                logger.Info($"DELTA {key} {value}");
    }

    private static string ToLowerFirstChar(string str)
    {
        return string.IsNullOrEmpty(str)
            ? str
            : char.ToLowerInvariant(str[0]) + str[1..];
    }
}
