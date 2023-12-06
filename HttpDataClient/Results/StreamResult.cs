namespace DataTools.Results;

public class StreamResult : IDisposable
{
	public StreamResult(string filePath, HttpResponseMessage? responseMessage, bool isSuccess)
	{
		Stream = File.OpenRead(filePath);
		FileInfo = new FileInfo(filePath);
		ResponseMessage = responseMessage;
		IsSuccess = isSuccess;
	}

	public StreamResult(string filePath, HttpResponseMessage? responseMessage)
	{
		Stream = File.OpenRead(filePath);
		FileInfo = new FileInfo(filePath);
		ResponseMessage = responseMessage;
		IsSuccess = ResponseMessage?.IsSuccessStatusCode ?? false;
	}

	public StreamResult(HttpResponseMessage? responseMessage)
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
