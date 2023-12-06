namespace DataTools.Consts;

internal class GlobalConsts
{
	public const string DownloadTempDir = "tempDownloads";
	public const int SkipFilesWhenClear = 20;

	public const int DownloadTimeoutDefault = 1_000 * 60 * 15;
	public const int PreLoadTimeoutDefault = 1_000;
	public const int RetriesCountDefault = 5;

	public const int MaxReadLength = 1048576 * 1024;
	public const int RetriesStopGrowing = 8;

	public const int LoadStatMaxCapacity = 100000;
}
