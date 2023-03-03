namespace HttpDataClient.Environment;

public interface ILogProvider
{
    public void Info(string message);
    public void Info(Exception exception, string message);
    public void Error(string message);
    public void Error(Exception exception, string message);
    public void Fatal(string message);
    public void Fatal(Exception exception, string message);
}
