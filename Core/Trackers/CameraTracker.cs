using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraTracker : SingletonProviderBase<CameraTracker>
{
    protected Stack<CameraControllerBase> cameraStack = new();
    protected HashSet<PlayerCamera> playerCameras = new();
    public Camera MainCamera { get; protected set; }
    public IEnumerable<PlayerCamera> PlayerCameras => playerCameras.ToList();

    public Camera Current { get; protected set; }

    public void Push(CameraControllerBase cameraController)
    {
        if (cameraStack.Count > 0 && cameraStack.Peek() == cameraController)
            return;

        cameraStack.Push(cameraController);
    }

    public bool IsLastStack() => cameraStack.Count == 1;

    public bool IsEmptyStack() => cameraStack.Count == 0;

    public int Count => cameraStack.Count;

    public CameraControllerBase Pop()
    {
        if (cameraStack.Count == 0)
            return null;

        return cameraStack.Pop();
    }

    public void ShowMainCamera()
    {
        foreach (var item in playerCameras)
            item.CurrentCamera?.gameObject.SetActive(false);
    }

    public void RestorePlayerCameras()
    {
        foreach (var item in playerCameras)
            item.CurrentCamera?.gameObject.SetActive(true);
    }

    public void AddPlayerCamera(PlayerCamera camera)
    {
        playerCameras.Add(camera);
    }

    public void RemovePlayerCamera(PlayerCamera camera)
    {
        playerCameras.Remove(camera);
    }

    public CameraControllerBase Peek()
    {
        return cameraStack.Count > 0 
            ? cameraStack.Peek() 
            : null;
    }

    internal void SetMainCamera(Camera camera)
    {
        MainCamera = camera;
    }

    internal void SetCurrent(CameraControllerBase cameraControllerBase, Camera camera)
    {
        cameraControllerBase.SetCurrent(true);
        Current = camera;

        foreach (var item in cameraStack)
        {
            item.SetCurrent(item == cameraControllerBase);
        }
    }
}