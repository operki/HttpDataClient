using EnvironmentUtils.Providers;
using log4net;

namespace EnvironmentUtils.Log4NetProviders;

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

    public void Info(Exception exception, string message)
    {
        logger.Info(message, exception);
    }

    public void Error(string message)
    {
        logger.Error(message);
    }

    public void Error(Exception exception, string message)
    {
        logger.Error(message, exception);
    }

    public void Fatal(string message)
    {
        logger.Fatal(message);
    }

    public void Fatal(Exception exception, string message)
    {
        logger.Fatal(message, exception);
    }
}
