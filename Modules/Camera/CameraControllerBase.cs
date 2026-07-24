using UnityEngine;

public abstract class CameraControllerBase : PRMonoBehaviour, IGameplayEvent
{
    [field: SerializeField] public Camera CurrentCamera { get; protected set; }
    public bool IsCurrent { get; protected set; }

    public abstract bool CanPushStack { get; }

    protected override void PRUpdate()
    {
        if (CurrentCamera.IsNull())
            return;

        HandleCamera();
    }
    protected abstract void HandleCamera();

    public virtual void SetMain(bool pushInStack = true)
    {
        if (this.IsNull())
            return;

        SetCameraHandler();

        CurrentCamera = CameraEvents.InvokeChangeCamera(this.gameObject);
        if(pushInStack)
            CameraTracker.Instance.Push(this);
    }

    protected virtual void SetCameraHandler()
    {
        CurrentCamera = CameraTracker.Instance.MainCamera;
        CameraTracker.Instance.SetCurrent(this, CurrentCamera);
        CameraTracker.Instance.ShowMainCamera();
    }

    public void SetCurrent(bool value)
    {
        IsCurrent = value;
    }

    public void ClearCamera()
    {
        IsCurrent = false;
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

        previous.SetMain(false);
    }

    public void Track(GameplayEventArgsBase args)
    {
        if(args is CameraChangerEvent cameraArgs && cameraArgs.Executer != this.gameObject)
            ClearCamera();
    }
}
