namespace HttpDataClient.Results;

internal struct HttpResponse
{
	public HttpResponseResult Result;
	public HttpResponseMessage? Message;
	public TimeSpan? ElapsedTime;

	public HttpResponse(HttpResponseResult result, HttpResponseMessage? message, TimeSpan? elapsedTime)
	{
		Result = result;
		Message = message;
		ElapsedTime = elapsedTime;
	}
}
