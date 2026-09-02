using UnityEngine;

public class CameraChangerEvent : GameplayEventArgsBase
{
    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "CameraChanger");
    }

    public GameObject Executer;

    public CameraChangerEvent(GameObject executer)
    {
        this.Executer = executer;
    }
}
