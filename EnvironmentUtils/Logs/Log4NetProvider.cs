using log4net;

namespace EnvironmentUtils.Logs;

public class Log4NetProvider : ILogProvider
{
    private readonly ILog logger;

    public Log4NetProvider(ILog logger)
    {
        this.logger = logger;
    }

    public void Info(string message)
    {
        logger.Info(message);
    }

    public void Info(Exception exception)
    {
        logger.Info(exception);
    }

    public void Info(string message, Exception exception)
    {
        logger.Info(message, exception);
    }

    public void Warn(string message)
    {
        logger.Warn(message);
    }

    public void Warn(Exception exception)
    {
        logger.Warn(exception);
    }

    public void Warn(string message, Exception exception)
    {
        logger.Warn(message, exception);
    }

    public void Error(string message)
    {
        logger.Error(message);
    }

    public void Error(Exception exception)
    {
        logger.Error(exception);
    }

    public void Error(string message, Exception exception)
    {
        logger.Error(message, exception);
    }

    public void Fatal(string message)
    {
        logger.Fatal(message);
    }

    public void Fatal(Exception exception)
    {
        logger.Fatal(exception);
    }

    public void Fatal(string message, Exception exception)
    {
        logger.Fatal(message, exception);
    }

    public void ChickenDelta(string metric, long count)
    {
        logger.Info($"CHICKEN_DELTA {metric} {count}");
    }

    public void ChickenDelta(IDictionary<string, long> metrics)
    {
        foreach(var kvp in metrics)
            logger.Info($"CHICKEN_DELTA {kvp.Key} {kvp.Value}");
    }
}
