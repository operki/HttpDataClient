namespace DataTools.Results;

public class DataResult
{
	private string? content;
	private byte[]? data;

	public DataResult(HttpResponseMessage? responseMessage)
	{
		ResponseMessage = responseMessage;
	}

	public HttpResponseMessage? ResponseMessage { get; }
	public bool IsSuccess => ResponseMessage?.IsSuccessStatusCode ?? false;

	public string? Content
	{
		get
		{
			try
			{
				return content ??= ResponseMessage?.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
			}
			catch(Exception)
			{
				return null;
			}
		}
	}

	public byte[]? Data
	{
		get
		{
			try
			{
				return data ??= ResponseMessage?.Content.ReadAsByteArrayAsync().ConfigureAwait(false).GetAwaiter().GetResult();
			}
			catch(Exception)
			{
				return null;
			}
		}
	}
}
