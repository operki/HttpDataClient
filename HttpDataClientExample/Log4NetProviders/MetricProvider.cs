using System.Collections.Concurrent;
using HttpDataClient.Environment;
using HttpDataClient.Helpers;
using log4net;

namespace HttpDataClient.Log4NetProviders;

public class MetricProvider : IMetricProvider
{
    private ILog logger;

    public MetricProvider(ILog logger)
    {
        this.logger = logger;}
    private readonly ConcurrentDictionary<string, long> metricsStorage = new();

    public void Inc(DefaultMetrics key)
    {
        Inc(key.ToString().ToLowerFirstChar());
    }

    public void Add(DefaultMetrics key, long addValue)
    {
        Add(key.ToString().ToLowerFirstChar(), addValue);
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
}
