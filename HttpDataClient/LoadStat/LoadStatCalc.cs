using System.Collections.Concurrent;
using HttpDataClient.Environment.Logs;
using HttpDataClient.Helpers;

namespace HttpDataClient.LoadStat;

internal class LoadStatCalc
{
    private const int MaxCapacity = 100000;
    private static Timer LogStatTimer;
    private readonly string id;
    private readonly ILogProvider log;
    private readonly string siteHost;
    private DateTime startDt;

    public LoadStatCalc(ILogProvider log, string baseUrl)
    {
        id = IdGenerator.GetId();
        this.log = log;
        siteHost = UrlHelper.GetHost(baseUrl).ToLowerFirstChar();
        LogStatTimer = new Timer(LogStat, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
        log.Info($"[HttpDataClient.LoadStats.{id}] Start calc load stats");
    }

    private ConcurrentDictionary<string, LoadStatRange> LoadStatMinutes { get; set; } = new();
    private ConcurrentDictionary<string, LoadStatRange> LoadStatHours { get; } = new();
    private ConcurrentDictionary<string, LoadStatRange> LoadStatDays { get; } = new();

    private void LogStat(object state)
    {
        try
        {
            if(!LoadStatMinutes.IsSignificant())
                return;

            var currentDt = DateTime.UtcNow;
            var mostLoad = LoadStatMinutes.Values.OrderBy(range => range.Count).Last();
            log.Info($"[HttpDataClient.LoadStats.{id}] Site {siteHost}: max {mostLoad.GetStat()}");
            var averageLoad = LoadStatMinutes.Values.Sum(range => range.Count) / LoadStatMinutes.Count;
            log.Info($"[HttpDataClient.LoadStats.{id}] Site {siteHost}: average requests per minutes is {averageLoad}");
            if(LoadStatMinutes.Count > MaxCapacity)
                LoadStatMinutes = new ConcurrentDictionary<string, LoadStatRange>(LoadStatMinutes
                    .OrderBy(kvp => kvp)
                    .Skip(MaxCapacity / 2));

            if(currentDt < startDt.AddHours(1))
                return;

            if(LoadStatHours.Count > 1)
            {
                var lastHourStat = LoadStatHours
                    .OrderBy(kvp => kvp.Key)
                    .Reverse().Take(2).Reverse()
                    .First().Value;
                log.Info($"[HttpDataClient.LoadStats.{id}] Site {siteHost}: last {lastHourStat.GetStat()}");
            }

            mostLoad = LoadStatHours.Values.OrderBy(range => range.Count).Last();
            log.Info($"[HttpDataClient.LoadStats.{id}] Site {siteHost}: max {mostLoad.GetStat()}");
            averageLoad = LoadStatHours.Values.Sum(range => range.Count) / LoadStatHours.Count;
            log.Info($"[HttpDataClient.LoadStats.{id}] Site {siteHost}: average requests per hours is {averageLoad}");
            if(currentDt < startDt.AddDays(1))
                return;

            if(LoadStatDays.Count > 1)
            {
                var lastDayStat = LoadStatDays
                    .OrderBy(kvp => kvp.Key)
                    .Reverse().Take(2).Reverse()
                    .First().Value;
                log.Info($"[HttpDataClient.LoadStats.{id}] Site {siteHost}: last {lastDayStat.GetStat()}");
            }

            mostLoad = LoadStatDays.Values.OrderBy(range => range.Count).Last();
            log.Info($"[HttpDataClient.LoadStats.{id}] Site {siteHost}: max {mostLoad.GetStat()}");
            averageLoad = LoadStatDays.Values.Sum(range => range.Count) / LoadStatDays.Count;
            log.Info($"[HttpDataClient.LoadStats.{id}] Site {siteHost}: average requests per days is {averageLoad}");
        }
        catch(Exception e)
        {
            log.Fatal($"[HttpDataClient.LoadStats.{id}] Exception while calc load stats", e);
        }
    }

    public void Inc()
    {
        var currentDt = DateTime.UtcNow;
        if(startDt == default)
            startDt = currentDt;

        IncInternal(LoadStatType.Minutes, currentDt);
        IncInternal(LoadStatType.Hours, currentDt);
        IncInternal(LoadStatType.Days, currentDt);
    }

    private void IncInternal(LoadStatType loadType, DateTime dt)
    {
        var loadStat = loadType switch
        {
            LoadStatType.Minutes => LoadStatMinutes,
            LoadStatType.Hours => LoadStatHours,
            LoadStatType.Days => LoadStatDays,
            LoadStatType.Unknown => throw new ArgumentOutOfRangeException(nameof(loadType), loadType, null),
            _ => throw new ArgumentOutOfRangeException(nameof(loadType), loadType, null)
        };
        var dRange = new LoadStatRange(loadType, dt);
        if(!loadStat.IsSignificant())
        {
            loadStat.TryAdd(dRange.Id, dRange);
            return;
        }

        var currentSourceLoad = loadStat
            .FirstOrDefault(kvp => kvp.Value.Exists(dt));
        if(currentSourceLoad.Key != null)
        {
            currentSourceLoad.Value.Inc();
            return;
        }

        loadStat.TryAdd(dRange.Id, dRange);
    }
}
