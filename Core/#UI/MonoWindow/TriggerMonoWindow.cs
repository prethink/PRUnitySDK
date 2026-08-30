using UnityEngine;

public class TriggerMonoWindow : PRMonoBehaviour
{
    [SerializeField] private EnumerationReference<MonoWindowKeyEnumerations> windowKey;

    protected override void PROnTriggerEnter(Collider other)
    {
        if (!other.TryGetLocalPlayer(out PlayerLocal player))
            return;

        var args = new MonoWindowArgsEmpty
        {
            Executor = player.PlayerId
        };

        PRUnitySDK.Trackers.MonoWindows.TryShowWindow(windowKey.ToEnumeration(), args);
    }
}
