using UnityEngine;

public static class CameraEvents
{
    public static Camera InvokeChangeCamera(GameObject executer)
    {
        EventBus.RaiseEvent<IGameplayEvent>(x => x.Track(new CameraChangerEvent(executer)));
        return Camera.main;
    }
}
