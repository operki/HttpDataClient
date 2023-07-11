using System.Diagnostics;
using System.Net.Http.Headers;
using Http.DataClient.Consts;
using Http.DataClient.Requests;
using Http.DataClient.Results;
using Http.DataClient.Utils;

namespace Http.DataClient;

internal static class HttpDataClientInternal
{
	public static async Task<DataResult> GetAsync(HttpRequest httpRequest)
	{
		var logProvider = httpRequest.LogProvider;
		var httpDataFactory = httpRequest.HttpDataFactory;
		var traceId = httpRequest.TraceId;
		var url = httpRequest.Url;
		var hideSecrets = httpRequest.HideSecretsFromUrls;

		var httpResponse = await HttpDataClientrRetries.GetAsync(httpRequest, () => httpDataFactory.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead));
		var responseResult = httpResponse.Result;
		var responseMessage = httpResponse.Message;

		var result = new DataResult(responseMessage);
		switch(responseResult)
		{
			case HttpResponseResult.Fail:
				logProvider?.Info($"{IdUtils.GetPrefix(traceId)}Failed get '{url.HideSecrets(hideSecrets)}'");
				break;
			case HttpResponseResult.Success:
				logProvider?.Info($"{IdUtils.GetPrefix(traceId)}Get '{url.HideSecrets(hideSecrets)}' ({(int)responseMessage!.StatusCode} {responseMessage.StatusCode}): result length {result.Content?.Length}, elapsed {httpResponse.ElapsedTime}");
				break;
			case HttpResponseResult.StopException:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		return result;
	}

	public static async Task<DataResult> PostAsync(HttpRequest httpRequest, byte[] body)
	{
		var logProvider = httpRequest.LogProvider;
		var httpDataFactory = httpRequest.HttpDataFactory;
		var traceId = httpRequest.TraceId;
		var url = httpRequest.Url;
		var hideSecrets = httpRequest.HideSecretsFromUrls;

		var httpResponse = await HttpDataClientrRetries.GetAsync(httpRequest, () =>
		{
			var httpContent = new ByteArrayContent(body);
			httpDataFactory.ModifyContent?.Invoke(httpContent);
			return httpDataFactory.Client.PostAsync(url, httpContent);
		});
		var responseResult = httpResponse.Result;
		var responseMessage = httpResponse.Message;

		var result = new DataResult(responseMessage);
		switch(responseResult)
		{
			case HttpResponseResult.Fail:
				logProvider?.Info($"{IdUtils.GetPrefix(traceId)}Failed post '{url.HideSecrets(hideSecrets)}'");
				break;
			case HttpResponseResult.Success:
				logProvider?.Info($"{IdUtils.GetPrefix(traceId)}Post '{url.HideSecrets(hideSecrets)}' ({(int)responseMessage!.StatusCode} {responseMessage.StatusCode}): result length {result.Content?.Length}, elapsed {httpResponse.ElapsedTime}");
				break;
			case HttpResponseResult.StopException:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		return result;
	}

	public static async Task<HttpStreamResult> GetStreamAsync(HttpRequest httpRequest, DownloadStrategyFileName strategyFileName = DownloadStrategyFileName.PathGet, string? fileName = null)
	{
		var logProvider = httpRequest.LogProvider;
		var httpDataFactory = httpRequest.HttpDataFactory;
		var traceId = httpRequest.TraceId;
		var url = httpRequest.Url;
		var hideSecrets = httpRequest.HideSecretsFromUrls;

		var tracePrefix = IdUtils.GetPrefix(traceId);
		var tmpFileName = LocalUtils.GetFileName(strategyFileName, url, fileName);
		long totalSize = 0;

		logProvider?.Info($"{tracePrefix}Start download from '{url.HideSecrets(hideSecrets)}'...");
		var sw = Stopwatch.StartNew();
		while(true)
		{
			var resumeDownload = File.Exists(tmpFileName);
			if(resumeDownload)
			{
				totalSize = new FileInfo(tmpFileName).Length;
				logProvider?.Info($"{tracePrefix}Already downloaded {totalSize} bytes from '{url.HideSecrets(hideSecrets)}'. Continue download...");
			}

			var httpResponse = await HttpDataClientrRetries.GetAsync(httpRequest, () =>
			{
				try
				{
					var currentRequest = new HttpRequestMessage(HttpMethod.Get, url);
					currentRequest.Headers.Range = resumeDownload
						? new RangeHeaderValue(new FileInfo(tmpFileName).Length, new FileInfo(tmpFileName).Length + GlobalConsts.HttpDataLoaderMaxReadLength)
						: new RangeHeaderValue(0, GlobalConsts.HttpDataLoaderMaxReadLength);

					return httpDataFactory.Client.SendAsync(currentRequest);
				}
				catch(Exception e)
				{
					Console.WriteLine(e);
					throw;
				}
			}).ConfigureAwait(false);
			var responseResult = httpResponse.Result;
			var responseMessage = httpResponse.Message;

			switch(responseResult)
			{
				case HttpResponseResult.StopException:
					return totalSize == 0
						? new HttpStreamResult(responseMessage)
						: new HttpStreamResult(tmpFileName, responseMessage, true);
				case HttpResponseResult.Fail:
					return new HttpStreamResult(responseMessage);
				case HttpResponseResult.Success:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			bool endDownload;
			using(var httpReadStream = responseMessage!.Content.ReadAsStreamAsync())
			{
				using(var fileWriteStream = File.Open(tmpFileName, FileMode.Append))
				{
					var data = new byte[GlobalConsts.HttpDataLoaderMaxReadLength];
					var dataLength = await httpReadStream.Result.ReadAsync(data, 0, data.Length);
					fileWriteStream.Write(data, 0, dataLength);
					totalSize += dataLength;
					endDownload = dataLength < data.Length;
				}
			}

			if(!endDownload)
				continue;

			var elapsed = sw.Elapsed;
			var result = new HttpStreamResult(tmpFileName, responseMessage);
			var downloadRate = (decimal)(result.Length / (elapsed.TotalSeconds + 0.0001) / 1000000);
			logProvider?.Info(result.ResponseMessage == null
				? $"{tracePrefix}Downloaded '{url.HideSecrets(hideSecrets)}' Undefined error: result length {result.Length}, elapsed {elapsed}, rate {downloadRate::0.00 MB/s}"
				: $"{tracePrefix}Downloaded '{url.HideSecrets(hideSecrets)}' ({(int)result.ResponseMessage.StatusCode} {result.ResponseMessage.StatusCode}): result length {result.Length}, elapsed {elapsed}, rate {downloadRate::0.00 MB/s}");
			return result;
		}
	}
}
