using HttpDataClient.Environment.Logs;
using HttpDataClient.Environment.Metrics;

namespace HttpDataClient.Environment;

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
