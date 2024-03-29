﻿using DataTools;
using HttpDataClientExample;
using log4net;
using log4net.Config;

Console.WriteLine("Hello, World!");

XmlConfigurator.Configure();
var logger = LogManager.GetLogger("root");

var httpDataClient = new HttpDataClient(new HttpDataClientSettings
{
	BaseUrl = "https://www.google.com",
	LogProvider = new Log4NetProvider(logger),
	MetricProvider = new Log4NetMetrics(logger)
});
var downloadResult = httpDataClient.GetSuccess("/images/branding/googlelogo/2x/googlelogo_color_92x30dp.png");
File.WriteAllBytes("1imageExample.jpg", downloadResult.Data!);
Console.WriteLine("File downloaded successfully!");
