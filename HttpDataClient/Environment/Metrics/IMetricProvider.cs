namespace HttpDataClient.Environment.Metrics;

public interface IMetricProvider
{
    public void Inc<T>(T key) where T : Enum;
    public void Flush();
}
