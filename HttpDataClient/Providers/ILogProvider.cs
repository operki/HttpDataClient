namespace HttpDataClient.Providers;

public interface ILogProvider
{
    public void Info(string message);
    public void Info(string message, Exception exception);
    public void Error(string message, Exception exception);
    public void Fatal(string message, Exception exception);
}
