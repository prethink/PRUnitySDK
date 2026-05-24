using UnityEngine;

public class WaitPause : CustomYieldInstruction
{
    public static readonly WaitPause Instance = new WaitPause();

    public override bool keepWaiting => PRUnitySDK.PauseManager.IsLogicPaused;
}