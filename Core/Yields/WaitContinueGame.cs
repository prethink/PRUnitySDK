using UnityEngine;

public class WaitContinueGame : CustomYieldInstruction
{
    public override bool keepWaiting => !PRUnitySDK.PauseManager.IsLogicPaused;
}

