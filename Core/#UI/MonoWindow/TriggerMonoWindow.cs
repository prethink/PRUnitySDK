using NaughtyAttributes;
using UnityEngine;

public class TriggerMonoWindow : PRMonoBehaviour
{
    [SerializeField] private EnumerationReference<MonoWindowKeyEnumerationProvider> windowKey;

    protected override void PROnTriggerEnter(Collider other)
    {
        if(other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent<PlayerLocal>(out var player))
        {
            PRUnitySDK.Trackers.MonoWindows.TryShowWindow(windowKey.ToEnumeration());
        }
    }
}
