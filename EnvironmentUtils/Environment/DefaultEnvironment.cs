using EnvironmentUtils.Providers;

namespace EnvironmentUtils.Environment;

public class DefaultEnvironment
{
    public ILogProvider Log { get; }
    public IMetricProvider MetricProvider { get; }

    public DefaultEnvironment(ILogProvider log, IMetricProvider metricProvider)
    {
        Log = log ?? throw new Exception("Empty ILog in DefaultEnvironment");
        MetricProvider = metricProvider ?? throw new Exception("Empty IChickenMetrics in DefaultEnvironment");
    }
}
