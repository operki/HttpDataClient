namespace HttpDataClient.Providers;

public interface IMetricProvider
{
    public void Inc<T>(T key) where T : Enum;
    public void Flush();
}
