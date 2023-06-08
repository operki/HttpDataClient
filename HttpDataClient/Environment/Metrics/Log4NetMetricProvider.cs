using System.Collections.Concurrent;
using HttpDataClient.Helpers;
using log4net;

namespace HttpDataClient.Environment.Metrics;

public class Log4NetMetrics : IMetricProvider
{
    private readonly ConcurrentDictionary<string, long> metricsStorage = new();
    private readonly ILog logger;

    public Log4NetMetrics(ILog log)
    {
        logger = log;
    }

    public void Inc<T>(T key) where T : Enum
    {
        Inc(key.ToString().ToLowerFirstChar());
    }

    private void Inc(string key)
    {
        metricsStorage.AddOrUpdate(key, 1, (_, value) => value + 1);
    }

    public void Flush()
    {
        foreach(var key in metricsStorage.Keys.OrderBy(key => key))
            if(metricsStorage.TryRemove(key, out var value))
                logger.Info($"CHICKEN_DELTA {key} {value}");
    }
}
