using System;
using System.Text;

namespace Focus.Utils.Chicken.Downloaders.HttpClient;

public static class IdGenerator
{
	private static readonly char[] Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
	private static readonly Random Random = new();

	public static string GetPrefix(string traceId = null)
	{
		return traceId == null
			? null
			: $"[{traceId}] ";
	}

	public static string GetPrefixAnyway(string traceId = null)
	{
		return traceId == null
			? $"[{GetId()}] "
			: $"[{traceId}] ";
	}

	public static string GetId(int length = 8)
	{
		var sb = new StringBuilder(length);
		for(var i = 0; i < length; i++)
			sb.Append(Base62Chars[Random.Next(62)]);
		return sb.ToString();
	}
}
