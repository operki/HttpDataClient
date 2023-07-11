namespace HttpDataClient.Utils;

internal static class StringUtils
{
	public static string? ToLowerFirstChar(this string? str)
	{
		return string.IsNullOrEmpty(str)
			? str
			: char.ToLowerInvariant(str![0]) + str.Substring(1);
	}

	public static bool IsSignificant(this string? value)
	{
		return !string.IsNullOrWhiteSpace(value);
	}

	public static bool SignificantContains(this string? value, string? sub)
	{
		return value != null && sub != null && value.IndexOf(sub, StringComparison.OrdinalIgnoreCase) >= 0;
	}
}
