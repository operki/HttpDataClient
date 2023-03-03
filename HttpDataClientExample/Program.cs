using HttpDataClient;
using HttpDataClient.Environment;
using HttpDataClient.Log4NetProviders;
using log4net;
using log4net.Config;

Console.WriteLine("Hello, World!");

XmlConfigurator.Configure();
var logger = LogManager.GetLogger("root");
var environment = new DefaultEnvironment(new LogProvider(logger), new MetricProvider(logger));

var httpDataClient = new HttpDataClient.HttpDataClient(environment, new HttpClientSettings { BaseUrl = "https://www.google.com" });
var downloadResult = httpDataClient.GetSuccess("/images/branding/googlelogo/2x/googlelogo_color_92x30dp.png");
File.WriteAllBytes("1imageExample.jpg", downloadResult.Data!);
Console.WriteLine("File downloaded successfully!");