using UnityEngine;

public abstract class CameraControllerBase : PRMonoBehaviour, IGameplayEvent
{
    [field: SerializeField] public Camera CurrentCamera { get; protected set; }

    protected override void PRUpdate()
    {
        if (CurrentCamera.IsNull())
            return;

        HandleCamera();
    }
    protected abstract void HandleCamera();

    public virtual void SetMainCamera(bool pushInStack = true)
    {
        if (this.IsNull())
            return;

        CurrentCamera = CameraEvents.InvokeChangeCamera(this.gameObject);
        if(pushInStack)
            CameraTracker.Instance.Push(this);
    }

    public void ClearCamera()
    {
        CurrentCamera = null;
    }

    public void Restore()
    {
        var tracker = CameraTracker.Instance;
        if (tracker.IsEmptyStack())
            return;

        var previous = tracker.Peek();
        if (previous.IsNull())
        {
            tracker.Pop();
            Restore();
        }

        if (previous == this && tracker.IsLastStack())
            return;

        if(previous == this)
        {
            var current = tracker.Pop();
            current.ClearCamera();
            Restore();
            return;
        }

        previous.SetMainCamera(false);
    }

    public void Track(GameplayEventArgsBase args)
    {
        if(args is CameraChangerEvent cameraArgs && cameraArgs.Executer != this.gameObject)
            ClearCamera();
    }
}
