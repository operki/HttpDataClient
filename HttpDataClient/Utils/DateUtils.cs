using System.Globalization;

namespace Http.DataClient.Utils;

internal static class DateUtils
{
	public static string ToSortableDotedString(this DateTime dateTime)
	{
		return dateTime.ToString("yyyy-MM-ddTHH.mm.ss", CultureInfo.InvariantCulture);
	}
}
