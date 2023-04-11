using System.Collections.Concurrent;
using EnvironmentUtils.Providers;
using log4net;

namespace EnvironmentUtils.Log4NetProviders;

public class Log4NetMetricProvider : IMetricProvider
{
    private readonly ILog logger;

    public Log4NetMetricProvider(ILog logger)
    {
        this.logger = logger;
    }

    private readonly ConcurrentDictionary<string, long> metricsStorage = new();

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
            : char.ToLowerInvariant(str[0]) + str.Substring(1);
    }
}
