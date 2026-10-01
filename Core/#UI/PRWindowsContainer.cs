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
    /// Контейнер постоянного интерфейса: полосы, панель быстрого доступа и подобное.
    /// </summary>
    /// <remarks>
    /// Отдельный canvas со своим порядком, а не общий с окнами: внутри одного canvas
    /// порядок задаёт иерархия, и постоянный интерфейс, созданный позже окна, накрывал бы
    /// открытое окно собой.
    /// </remarks>
    public PRContainer HudCanvas;

    /// <summary>
    /// Контейнер постоянного интерфейса, который виден поверх части окон.
    /// </summary>
    /// <remarks>
    /// Шапка с уровнем и ресурсами: покупая в магазине, игрок должен видеть, сколько у него
    /// денег, а обычное окно (подарок, настройки) её перекрывает. Какие окна ниже шапки,
    /// а какие выше, решает порядок их canvas - см. <see cref="MonoWindowFactoryBase{T}.SortingOrder"/>.
    /// </remarks>
    public PRContainer PriorityHudCanvas;

    /// <summary>
    /// Порядок отрисовки постоянного интерфейса.
    /// </summary>
    public const int HudSortingOrder = 200;

    /// <summary>
    /// Порядок окон, которые лежат ниже приоритетного интерфейса: шапка с ресурсами
    /// видна поверх них. Магазин из таких.
    /// </summary>
    public const int UnderPriorityHudWindowsSortingOrder = 300;

    /// <summary>
    /// Порядок отрисовки постоянного интерфейса, который остаётся над частью окон.
    /// </summary>
    public const int PriorityHudSortingOrder = 400;

    /// <summary>
    /// Порядок отрисовки обычных окон: над приоритетным интерфейсом.
    /// </summary>
    public const int WindowsSortingOrder = 500;

    /// <summary>
    /// Базовый порядок отрисовки уведомлений: над всем остальным интерфейсом.
    /// </summary>
    public const int NotifiersSortingOrder = 1000;

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

    private readonly System.Collections.Generic.Dictionary<int, PRContainer> windowCanvases = new();

    /// <summary>
    /// Canvas для окон с указанным порядком отрисовки.
    /// </summary>
    /// <remarks>
    /// Окна с обычным порядком живут на общем canvas. Окну, которому нужен другой уровень
    /// (ниже шапки или выше уведомлений), заводится свой экранный canvas, по одному
    /// на каждый порядок. Именно корневой canvas, а не вложенный: у вложенного своя шкала,
    /// и текст в нём мылится.
    /// </remarks>
    public PRContainer GetWindowCanvas(int sortingOrder)
    {
        if (sortingOrder == WindowsSortingOrder)
            return SharedCanvas;

        if (!windowCanvases.TryGetValue(sortingOrder, out PRContainer canvas) || canvas == null)
        {
            canvas = CreateScreenCanvas($"Windows.Canvas.{sortingOrder}", sortingOrder);
            windowCanvases[sortingOrder] = canvas;
        }

        return canvas;
    }

    private void InitializeWindows()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Container      = MonoBehaviourUtils.CreateContainer("Windows");

        HudCanvas      = CreateScreenCanvas("Windows.HudCanvas", HudSortingOrder);

        // Canvas сам становится элементом постоянного интерфейса: прячет его тот же трекер,
        // что и полосы над сущностями, и состояние не расходится на два выключателя.
        HudCanvas.AddComponent<HudCanvasElement>();

        SharedCanvas   = CreateScreenCanvas("Windows.SharedCanvas", WindowsSortingOrder);

        // Над окнами, но под уведомлениями; прячется тем же трекером, что и остальной HUD.
        PriorityHudCanvas = CreateScreenCanvas("Windows.PriorityHudCanvas", PriorityHudSortingOrder);
        PriorityHudCanvas.AddComponent<HudCanvasElement>();

        EnsureEventSystem();

        PRLog.WriteDebug(typeof(PRUnitySDK), $"Initialize Windows complete. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
        stopwatch.Stop();
    }

    /// <summary>
    /// Создаёт экранный canvas с заданным порядком отрисовки.
    /// </summary>
    /// <remarks>
    /// Порядок задаётся явно: между canvas'ами он решает, кто кого перекрывает, и без него
    /// всё сводится к очерёдности создания — то есть к случайности.
    /// </remarks>
    /// <param name="name">Имя объекта на сцене.</param>
    /// <param name="sortingOrder">Порядок отрисовки; больше — выше.</param>
    /// <returns>Созданный контейнер.</returns>
    private PRContainer CreateScreenCanvas(string name, int sortingOrder)
    {
        PRContainer container = MonoBehaviourUtils.CreateContainer(name);

        var canvas = container.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var canvasScaler = container.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasScaler.referencePixelsPerUnit = 100;

        container.AddComponent<GraphicRaycaster>();

        return container;
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
