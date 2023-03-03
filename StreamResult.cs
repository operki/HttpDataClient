using System;
using System.IO;
using System.Net.Http;

namespace Focus.Utils.Chicken.Downloaders.HttpClient;

public class HttpStreamResult : IDisposable
{
	public HttpStreamResult(string filePath, HttpResponseMessage responseMessage, bool isSuccess)
	{
		Stream = File.OpenRead(filePath);
		FileInfo = new FileInfo(filePath);
		ResponseMessage = responseMessage;
		IsSuccess = isSuccess;
	}

	public HttpStreamResult(string filePath, HttpResponseMessage responseMessage)
	{
		Stream = File.OpenRead(filePath);
		FileInfo = new FileInfo(filePath);
		ResponseMessage = responseMessage;
		IsSuccess = ResponseMessage.IsSuccessStatusCode;
	}

	public HttpStreamResult(HttpResponseMessage responseMessage)
	{
		ResponseMessage = responseMessage;
		IsSuccess = false;
	}

	public HttpResponseMessage ResponseMessage { get; }
	public bool IsSuccess { get; }

	public Stream Stream { get; }
	public long Length => FileInfo.Length;

	public FileInfo FileInfo { get; }

	public void Dispose()
	{
		((IDisposable)Stream)?.Dispose();
		((IDisposable)ResponseMessage)?.Dispose();
	}
}
