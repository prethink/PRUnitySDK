using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public partial class PRWindowsContainer 
{
    /// <summary>
    /// Контейнер для окон.   
    /// </summary>
    public PRContainer Container;

    /// <summary>
    /// Контейнер для окон.   
    /// </summary>
    public PRContainer SharedCanvas;

    /// <summary>
    /// Контейнер для уведомлений.   
    /// </summary>
    public PRContainer Notifiers;

    public RewardNotifier RewardNotifier;
    public ToastMessageNotifier ToastMessageNotifier;

    public void Initialize()
    {
        this.RunMethodHooks(MethodHookStage.PreOperation);

        InitializeWindows();
        InitializeNotifiers();

        this.RunMethodHooks(MethodHookStage.PostOperation);
    }

    private void InitializeWindows()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Container      = MonoBehaviourUtils.CreateContainer("Windows");

        SharedCanvas   = MonoBehaviourUtils.CreateContainer("Windows.SharedCanvas");

        var canvas = SharedCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var canvasScaler = SharedCanvas.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasScaler.referencePixelsPerUnit = 100;

        var graphicRaycaster = SharedCanvas.AddComponent<GraphicRaycaster>();

        var settingsWindows = new SettingsMonoWindowFactory().CreateMonoWindow();
        var test = new DashboardMessagesFactory().Create(SharedCanvas.transform);

        PRLog.WriteDebug(typeof(PRUnitySDK), $"Initialize Windows complete. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
        stopwatch.Stop();
    }

    private void InitializeNotifiers()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Notifiers = MonoBehaviourUtils.CreateContainer("Notifiers");

        RewardNotifier = new RewardNotifierFactory().Create();
        ToastMessageNotifier = new ToastMessageNotifierFactory().Create();

        PRLog.WriteDebug(typeof(PRUnitySDK), $"Initialize Notifiers complete. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
        stopwatch.Stop();
    }
}
