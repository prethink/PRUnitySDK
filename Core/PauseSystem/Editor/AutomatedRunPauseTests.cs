#if PRSDK_TESTS
using NUnit.Framework;

/// <summary>
/// Пауза по фокусу и признак автоматической проверки.
/// </summary>
public class AutomatedRunPauseTests
{
    private IServerTime previousServerTime;

    [SetUp]
    public void SetUp()
    {
        // События паузы берут время у SDK, а в EditMode он не поднят.
        previousServerTime = PRUnitySDK.ServerTime;
        PRUnitySDK.ServerTime ??= new LocalServerTime();
    }

    [TearDown]
    public void TearDown()
    {
        PRUnitySDK.IsAutomatedRun = false;
        PRUnitySDK.PauseManager.SetFocusPaused(false, this);
        PRUnitySDK.ServerTime = previousServerTime;
    }

    [Test]
    public void FocusLoss_DuringAutomatedRun_DoesNotPause()
    {
        PRUnitySDK.IsAutomatedRun = true;

        PRUnitySDK.PauseManager.SetFocusPaused(true, this);

        Assert.That(PRUnitySDK.PauseManager.IsFocusPaused, Is.False);
    }

    [Test]
    public void AutomatedRun_ClearsFocusPauseAlreadySet()
    {
        PRUnitySDK.PauseManager.SetFocusPaused(true, this);

        PRUnitySDK.IsAutomatedRun = true;

        Assert.That(PRUnitySDK.PauseManager.IsFocusPaused, Is.False);
    }

    [Test]
    public void FocusLoss_WithoutAutomatedRun_Pauses()
    {
        PRUnitySDK.PauseManager.SetFocusPaused(true, this);

        Assert.That(PRUnitySDK.PauseManager.IsFocusPaused, Is.True);
    }

    [Test]
    public void AutomatedRun_DoesNotCancelLogicPause()
    {
        PRUnitySDK.IsAutomatedRun = true;
        PRUnitySDK.PauseManager.SetLogicPaused(true, this);

        bool paused = PRUnitySDK.PauseManager.IsLogicPaused;
        PRUnitySDK.PauseManager.SetLogicPaused(false, this);

        Assert.That(paused, Is.True);
    }
}
#endif
