namespace HttpDataClient.Providers;

public interface IMetricProvider
{
    public void Inc(DefaultMetrics key);
    public void Add(DefaultMetrics key, long addValue);
    public void Inc(string key);
    public void Add(string key, long addValue);
    public void Flush();
}
