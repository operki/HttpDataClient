namespace EnvironmentUtils.Logs;

public interface ILogProvider
{
    public void Info(string message);
    public void Info(Exception exception);
    public void Info(string message, Exception exception);
    public void Warn(string message);
    public void Warn(Exception exception);
    public void Warn(string message, Exception exception);
    public void Error(string message);
    public void Error(Exception exception);
    public void Error(string message, Exception exception);
    public void Fatal(string message);
    public void Fatal(Exception exception);
    public void Fatal(string message, Exception exception);
    public void ChickenDelta(string metric, long count);
    public void ChickenDelta(IDictionary<string, long> metrics);
}
