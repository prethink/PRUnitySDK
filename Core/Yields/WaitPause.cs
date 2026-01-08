using UnityEngine;

public class WaitPause : CustomYieldInstruction
{
    public override bool keepWaiting => PRUnitySDK.PauseManager.IsLogicPaused;
}

