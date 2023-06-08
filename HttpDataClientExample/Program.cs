using HttpDataClient;
using HttpDataClient.Environment;
using HttpDataClient.Environment.Logs;
using HttpDataClient.Environment.Metrics;
using log4net;
using log4net.Config;

Console.WriteLine("Hello, World!");

XmlConfigurator.Configure();
var logger = LogManager.GetLogger("root");
var environment = new TrackEnvironment(new Log4NetProvider(logger), new Log4NetMetrics(logger));

var httpDataClient = new HttpDataLoader(environment, new HttpDataLoaderSettings { BaseUrl = "https://www.google.com" });
var downloadResult = httpDataClient.GetSuccess("/images/branding/googlelogo/2x/googlelogo_color_92x30dp.png");
File.WriteAllBytes("1imageExample.jpg", downloadResult.Data!);
Console.WriteLine("File downloaded successfully!");