using HttpDataClient.Helpers;

namespace HttpDataClient.Settings.LoadStat;

internal class LoadStatRange
{
    private readonly DateTime endDt;
    private readonly LoadStatType loadStatType;
    private readonly DateTime startDt;

    public LoadStatRange(LoadStatType loadStatType, DateTime startDt)
    {
        this.loadStatType = loadStatType;
        this.startDt = startDt;
        endDt = GetEndDt(loadStatType, startDt);
        Count = 1;
    }

    public long Count { get; private set; }
    public string Id => string.Join("_", loadStatType, startDt.ToSortableDotedString(), endDt.ToSortableDotedString());

    public string GetStat()
    {
        return $"requests per {loadStatType.ToString().ToLowerFirstChar()} is {Count} from {startDt.ToSortableDotedString()} to {endDt.ToSortableDotedString()}";
    }

    private static DateTime GetEndDt(LoadStatType loadStatType, DateTime startDt)
    {
        switch(loadStatType)
        {
            case LoadStatType.Minutes:
                return startDt.AddMinutes(1);
            case LoadStatType.Hours:
                return startDt.AddHours(1);
            case LoadStatType.Days:
                return startDt.AddDays(1);
            case LoadStatType.Unknown:
            default:
                throw new ArgumentOutOfRangeException(nameof(loadStatType), loadStatType, null);
        }
    }

    public void Inc()
    {
        Count += 1;
    }

    public bool Exists(DateTime dt)
    {
        return startDt <= dt && dt <= endDt;
    }
}
