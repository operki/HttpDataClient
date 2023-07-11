namespace HttpDataClient.Consts;

internal class GlobalConsts
{
	public const string LocalHelperTempDir = "tempDownloads";
	public const int LocalHelperSkipFilesWhenClear = 20;

	public const int HttpDataLoaderSettingsDownloadTimeoutDefault = 1_000 * 60 * 15;
	public const int HttpDataLoaderSettingsPreLoadTimeoutDefault = 1_000;
	public const int HttpDataLoaderSettingsRetriesCountDefault = 5;

	public const int HttpDataLoaderMaxReadLength = 1048576 * 1024;
	public const int HttpDataLoaderRetriesStopGrowing = 8;

	public const int LoadStatCalcMaxCapacity = 100000;
}
