using UnityEngine;

public class MainCamera : PRMonoBehaviour
{
    [field: SerializeField] public Camera Camera { get; protected set; }

    protected override void InitializationComponents()
    {
        Camera = GetComponent<Camera>();
        CameraTracker.Instance.SetMainCamera(this.Camera);
        base.InitializationComponents();
    }
}
