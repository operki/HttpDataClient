namespace Http.DataClient.Results;

public class HttpStreamResult : IDisposable
{
	public HttpStreamResult(string filePath, HttpResponseMessage? responseMessage, bool isSuccess)
	{
		Stream = File.OpenRead(filePath);
		FileInfo = new FileInfo(filePath);
		ResponseMessage = responseMessage;
		IsSuccess = isSuccess;
	}

	public HttpStreamResult(string filePath, HttpResponseMessage? responseMessage)
	{
		Stream = File.OpenRead(filePath);
		FileInfo = new FileInfo(filePath);
		ResponseMessage = responseMessage;
		IsSuccess = ResponseMessage?.IsSuccessStatusCode ?? false;
	}

	public HttpStreamResult(HttpResponseMessage? responseMessage)
	{
		ResponseMessage = responseMessage;
		IsSuccess = false;
	}

	public HttpResponseMessage? ResponseMessage { get; }
	public bool IsSuccess { get; }

	public Stream? Stream { get; }
	public long Length => FileInfo?.Length ?? default;

	public FileInfo? FileInfo { get; }

	public void Dispose()
	{
		((IDisposable)Stream)?.Dispose();
		((IDisposable)ResponseMessage)?.Dispose();
	}
}
