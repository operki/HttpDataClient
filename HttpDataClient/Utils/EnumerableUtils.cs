namespace DataTools.Utils;

internal static class EnumerableUtils
{
	public static bool IsSignificant<T>(this IEnumerable<T> enumerable, Func<T, bool>? action = null)
	{
		return enumerable.EmptyIfNull().Any(action ?? (t => t != null && !t.Equals(default(T))));
	}

	private static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? enumerable)
	{
		return enumerable ?? Enumerable.Empty<T>();
	}
}
