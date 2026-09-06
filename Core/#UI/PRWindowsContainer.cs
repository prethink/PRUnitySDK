using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
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

        EnsureEventSystem();

        PRLog.WriteDebug(typeof(PRUnitySDK), $"Initialize Windows complete. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
        stopwatch.Stop();
    }

    /// <summary>
    /// Заводит систему UI-событий, если её нет ни в одной загруженной сцене.
    /// </summary>
    /// <remarks>
    /// Canvas и GraphicRaycaster сами по себе бесполезны: без EventSystem UI не получает
    /// ни одного указательного события, и окна выглядят рабочими, но не реагируют на клики.
    /// Раз canvas окон создаёт SDK, он же отвечает и за EventSystem — иначе окна работают
    /// только в тех сценах, где систему событий не забыли положить руками (в dev-сценах
    /// её обычно нет).
    /// </remarks>
    private void EnsureEventSystem()
    {
        if (EventSystem.current != null || Object.FindObjectOfType<EventSystem>() != null)
            return;

        PRContainer eventSystem = MonoBehaviourUtils.CreateContainer("Windows.EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        PRLog.WriteDebug(typeof(PRUnitySDK), "EventSystem не найден в сцене - создан SDK.");
    }

    private void InitializeNotifiers()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Notifiers = MonoBehaviourUtils.CreateContainer("Notifiers");

        PRLog.WriteDebug(typeof(PRUnitySDK), $"Initialize Notifiers complete. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
        stopwatch.Stop();
    }
}
