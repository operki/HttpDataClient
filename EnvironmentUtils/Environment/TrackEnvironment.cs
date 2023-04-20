using EnvironmentUtils.Logs;
using EnvironmentUtils.Metrics;

namespace EnvironmentUtils.Environment;

public class TrackEnvironment
{
    public ILogProvider Log { get; }
    public IMetricProvider Metrics { get; }

    public TrackEnvironment(ILogProvider log, IMetricProvider metrics)
    {
        Log = log;
        Metrics = metrics;
    }
}
