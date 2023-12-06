using DataTools.Providers;
using log4net;

namespace HttpDataClientExample;

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

	public void Info(string message, Exception exception)
	{
		logger.Info(message, exception);
	}

	public void Error(string message, Exception exception)
	{
		logger.Error(message, exception);
	}

	public void Fatal(string message, Exception exception)
	{
		logger.Fatal(message, exception);
	}
}
