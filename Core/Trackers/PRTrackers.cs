using System.Collections.Generic;

public partial class PRTrackers
{
    public WatcherTracker Watchers => WatcherService.Instance;

    public PlayerTracker Players => PlayerService.Instance;

    public EntityTracker Entities => EntityService.Instance;

    public MonoWindowsTracker MonoWindows => MonoWindowsService.Instance;
    public NotifierTracker Notifiers => NotifierService.Instance;

    public CameraTracker CameraTracker => CameraTracker.Instance;

    public HashSet<ISaveable> Saveables = new HashSet<ISaveable>();
}
