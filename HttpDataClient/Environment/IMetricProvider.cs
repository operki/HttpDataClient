namespace HttpDataClient.Environment;

public interface IMetricProvider
{
    public void Inc(DefaultMetrics key);
    public void Inc(string key);
    public void Add(DefaultMetrics key, long addValue);
    public void Add(string key, long addValue);
    public void Flush();
}
