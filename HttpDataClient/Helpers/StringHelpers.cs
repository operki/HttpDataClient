namespace HttpDataClient.Helpers;

internal static class StringHelpers
{
    internal static bool IsSignificant(this string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }
    
    internal static string ToLowerFirstChar(this string str)
    {
        return string.IsNullOrEmpty(str)
            ? str
            : char.ToLowerInvariant(str[0]) + str[1..];
    }
}
