namespace EnvironmentUtils.Providers;

public interface IMetricProvider
{
    public void Inc(string key);
    public void Add(string key, long addValue);
    public void Flush();
}
