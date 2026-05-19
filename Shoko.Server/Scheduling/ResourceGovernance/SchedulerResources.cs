namespace Shoko.Server.Scheduling.ResourceGovernance;

public static class SchedulerResources
{
    public static readonly SchedulerResourceKey AniDBHttp = new("AniDB", "HTTP");

    public static readonly SchedulerResourceKey AniDBUdp = new("AniDB", "UDP");

    public static readonly SchedulerResourceKey TMDBApi = new("TMDB", "API");

    public static readonly SchedulerResourceKey TraktApi = new("Trakt", "API");

    public static readonly SchedulerResourceKey LocalCpu = new("Local", "CPU");

    public static readonly SchedulerResourceKey LocalDiskRead = new("Local", "DiskRead");

    public static readonly SchedulerResourceKey LocalDiskWrite = new("Local", "DiskWrite");

    public static readonly SchedulerResourceKey LocalDatabase = new("Local", "Database");
}
