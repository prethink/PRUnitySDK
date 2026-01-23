public partial class PRTrackers
{
    public WatcherTracker Watchers => WatcherService.Instance;

    public PlayerTracker Players => PlayerService.Instance;

    public EntityTracker Entities => EntityService.Instance;

    public MonoWindowsTracker MonoWindows => MonoWindowsService.Instance;
}
