namespace EnvironmentUtils.Helpers;

public static class StringUtils
{
    public static string ToLowerFirstChar(this string str)
    {
        return string.IsNullOrEmpty(str)
            ? str
            : char.ToLowerInvariant(str[0]) + str.Substring(1);
    }
}
