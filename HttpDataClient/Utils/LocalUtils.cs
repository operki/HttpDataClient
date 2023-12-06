using System.Text.RegularExpressions;
using DataTools.Consts;

namespace DataTools.Utils;

internal static class LocalUtils
{
	private static readonly Regex RemoveInvalidRegex = new($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))}]");

	public static void CreateTempDir()
	{
		Directory.CreateDirectory(GlobalConsts.DownloadTempDir);
	}

	public static void TryClearDir(DownloadStrategyFileName? strategyFileName = null)
	{
		var skipFiles = strategyFileName is null or DownloadStrategyFileName.Random
			? null
			: (int?)GlobalConsts.SkipFilesWhenClear;

		try
		{
			if(!Directory.Exists(GlobalConsts.DownloadTempDir))
			{
				Directory.CreateDirectory(GlobalConsts.DownloadTempDir);
				return;
			}

			var dirFiles = Directory.GetFiles(GlobalConsts.DownloadTempDir);
			if(skipFiles == null)
				foreach(var file in dirFiles)
					File.Delete(file);
			else
				foreach(var file in dirFiles.OrderByDescending(file => file)
						    .Skip(skipFiles.Value))
					File.Delete(file);
		}
		catch(UnauthorizedAccessException) { }
		catch(IOException) { }
		catch(Exception e)
		{
			throw new Exception($"Can't clear folder '{GlobalConsts.DownloadTempDir}'. Exception: {e}");
		}
	}

	public static string GetFileName(DownloadStrategyFileName strategyFileName, string url, string? fileName)
	{
		if(fileName.IsSignificant())
			return Path.Combine(GlobalConsts.DownloadTempDir, GetSafeFileName(fileName!));

		return strategyFileName switch
		{
			DownloadStrategyFileName.PathGet => Path.Combine(GlobalConsts.DownloadTempDir, GetSafeFileName(url)),
			DownloadStrategyFileName.Random => Path.Combine(GlobalConsts.DownloadTempDir, Guid.NewGuid().ToString()),
			DownloadStrategyFileName.Specify => throw new Exception("FileName must be significant or use other download strategy"),
			_ => throw new ArgumentOutOfRangeException(nameof(strategyFileName), strategyFileName, null)
		};
	}

	public static string GetSafeFileName(string fileName)
	{
		return RemoveInvalidRegex.Replace(Path.GetFileName(fileName), "");
	}
}
