namespace EnvironmentUtils.Metrics;

public interface IMetricProvider
{
    public void Inc<T>(T key) where T : Enum;
    public void Inc(string key);
    public void Add<T>(T key, long addValue) where T : Enum;
    public void Add(string key, long addValue);
    public void Flush();
}
