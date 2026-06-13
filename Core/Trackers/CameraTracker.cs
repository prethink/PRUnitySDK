using System.Collections.Generic;

public class CameraTracker : SingletonProviderBase<CameraTracker>
{
    protected Stack<CameraControllerBase> cameraStack = new();

    public void Push(CameraControllerBase cameraController)
    {
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

    public CameraControllerBase Peek()
    {
        return cameraStack.Count > 0 ? cameraStack.Peek() : null;
    }
}