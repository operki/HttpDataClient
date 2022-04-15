using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Bingo.Utils;
using log4net;

namespace Bingo.DataMining_Utils.HttpDataClient
{
	public static class ProxyShuffler
	{
		public static int ProxyCheckMultiplierSeconds { get; set; } = 5;
		public static int RequestTimeoutMilliseconds { get; set; } = 2 * 1_000;
		public static int TotalWaitTimeToFatalMilliseconds { get; set; } = 1_000 * 60 * 60;

		private static readonly ILog Log = LogManager.GetLogger(typeof(ProxyShuffler));
		private static readonly char[] Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
		private static readonly Random Random = new();

		private static ConcurrentQueue<ProxyContext> Proxies { get; set; } = new();
		private static int Waiters;
		private static string LogPrefix { get; set; }

		private static string StartLogPrefix => LogPrefix == null ? null : "[" + LogPrefix + "] ";
		private static string TaskLogPrefix => LogPrefix == null ? null : LogPrefix + ".";

		public static void Init(IReadOnlyCollection<string> proxiesList, string user, string pass, string logPfx = null)
		{
			LogPrefix = logPfx;
			if(!proxiesList.IsSignificant())
			{
				Log.Warn($"{StartLogPrefix}Empty proxies list. Try do requests with single direct connection");
				Proxies.Enqueue(new ProxyContext(null, null, null));
			}
			else
			{
				Proxies = new ConcurrentQueue<ProxyContext>(proxiesList
					.Select(proxy => new ProxyContext(proxy, user, pass)));
				Log.Info($"{StartLogPrefix}Get {Proxies.Count} proxies: {proxiesList.ToJsonString()}");
			}
		}

		public static bool TryGetData(string url, out HttpDataResult dataResult)
		{
			var proxyContext = GetProxyContext();
			var downloadResultBool = proxyContext.TryGetContextData(url, out dataResult);
			proxyContext.Dispose();
			return downloadResultBool;
		}

		private static ProxyContext GetProxyContext(string taskId = null)
		{
			Interlocked.Increment(ref Waiters);
			taskId ??= GetRndId();
			var curWaiters = Waiters;
			var totalWaitTime = 0;
			var deqResult = Proxies.TryDequeue(out var proxyContext);
			while(!deqResult)
			{
				if(totalWaitTime > TotalWaitTimeToFatalMilliseconds)
					Log.Fatal($"[{TaskLogPrefix}{taskId}] Waiting free proxy already {TimeSpan.FromMilliseconds(totalWaitTime):g}. Maybe it stuck?");

				if(Waiters < curWaiters)
					curWaiters = Waiters;
				var waitTime = curWaiters * 1_000 * ProxyCheckMultiplierSeconds;

				Log.Info($"[{TaskLogPrefix}{taskId}] Waiting free proxy for {TimeSpan.FromMilliseconds(waitTime):g}");
				Thread.Sleep(waitTime);
				totalWaitTime += waitTime;
				deqResult = Proxies.TryDequeue(out proxyContext);
			}

			Interlocked.Decrement(ref Waiters);
			proxyContext.TaskId = taskId;
			return proxyContext;
		}

		private static string GetRndId(int count = 6)
		{
			var sb = new StringBuilder(count);
			for(var i = 0; i < count; i++)
				sb.Append(Base62Chars[Random.Next(62)]);
			return sb.ToString();
		}

		private class ProxyContext
		{
			public string TaskId { get; set; }
			private CookieContainer Cookies { get; set; }
			private string ContextId { get; }
			private Uri Uri { get; }
			private WebProxy WebProxy { get; }
			private NetworkCredential Credentials { get; }

			internal ProxyContext(string url, string user, string pass)
			{
				ContextId = GetRndId();
				if(url == null)
					return;

				Credentials = new NetworkCredential(user, pass);
				Uri = new Uri(url);
				WebProxy = new WebProxy(Uri)
				{
					BypassProxyOnLocal = false,
					UseDefaultCredentials = false,
					Credentials = Credentials
				};
			}

			internal bool TryGetContextData(string url, out HttpDataResult dataResult)
			{
				Log.Info($"[{TaskLogPrefix}{TaskId}] Start download with proxy '{Uri}'");
				var httpDataClient = new HttpDataClient(new HttpDataClientSettings
				{
					Proxy = WebProxy,
					SslValidation = (_, _, _, _) => true,
					Credentials = Credentials,
					CookieContainer = Cookies,
					RetriesCount = 3,
					PreLoadTimeout = RequestTimeoutMilliseconds
				});
				dataResult = httpDataClient.Get(url, TaskLogPrefix + TaskId);
				Cookies = httpDataClient.CookieContainer;
				Log.Info($"[{TaskLogPrefix}{TaskId}] End download");
				return dataResult.IsSuccess;
			}

			internal void Dispose()
			{
				if(Proxies.All(proxy => proxy.ContextId != ContextId))
					Proxies.Enqueue(this);
			}
		}
	}
}
