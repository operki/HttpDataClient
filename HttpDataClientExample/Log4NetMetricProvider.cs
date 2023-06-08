using System.Collections.Concurrent;
using HttpDataClient.Environment.Metrics;
using log4net;

namespace HttpDataClientExample;

public class Log4NetMetrics : IMetricProvider
{
    private readonly ILog logger;
    private readonly ConcurrentDictionary<string, long> metricsStorage = new();

    public Log4NetMetrics(ILog log)
    {
        logger = log;
    }

    public void Inc<T>(T key) where T : Enum
    {
        Inc(ToLowerFirstChar(key.ToString()));
    }

    public void Flush()
    {
        foreach(var key in metricsStorage.Keys.OrderBy(key => key))
            if(metricsStorage.TryRemove(key, out var value))
                logger.Info($"CHICKEN_DELTA {key} {value}");
    }

    ~Log4NetMetrics()
    {
        Flush();
    }

    private void Inc(string key)
    {
        metricsStorage.AddOrUpdate(key, 1, (_, value) => value + 1);
    }

    private static string ToLowerFirstChar(string str)
    {
        return string.IsNullOrEmpty(str)
            ? str
            : char.ToLowerInvariant(str[0]) + str.Substring(1);
    }
}
