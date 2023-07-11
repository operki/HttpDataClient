﻿using System.Globalization;

namespace HttpDataClient.Utils;

internal static class DateUtils
{
	public static string ToSortableDotedString(this DateTime dateTime)
	{
		return dateTime.ToString("yyyy-MM-ddTHH.mm.ss", CultureInfo.InvariantCulture);
	}
}
