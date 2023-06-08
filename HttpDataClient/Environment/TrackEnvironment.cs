using HttpDataClient.Environment.Logs;
using HttpDataClient.Environment.Metrics;

namespace HttpDataClient.Environment;

public class TrackEnvironment
{
    public TrackEnvironment(ILogProvider log, IMetricProvider metrics)
    {
        Log = log;
        Metrics = metrics;
    }

    public ILogProvider Log { get; }
    public IMetricProvider Metrics { get; }
}
