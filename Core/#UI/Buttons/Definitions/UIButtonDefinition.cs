using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Описание экранной кнопки. Состояние наблюдателя и анимации хранится в созданном UI.
/// </summary>
[CreateAssetMenu(menuName = "PRUnitySDK/UI/Button", fileName = "UIButton")]
public class UIButtonDefinition : ScriptableObject, IIconProvider, ILocalizationProvider
{
    [Header("Внешний вид")]
    [SerializeField, SpritePreview(80)] private Sprite icon;
    [Tooltip("Размер иконки относительно подложки: 1 — 100%, 0.6 — 60%. Пропорции спрайта сохраняются.")]
    [SerializeField, Min(0f)] private float iconSize = 0.74f;
    [SerializeField] private Sprite background;
    [Tooltip("Картинка между подложкой и иконкой, например лучи. Пусто — без промежуточного слоя.")]
    [SerializeField, SpritePreview(80)] private Sprite decoration;
    [Tooltip("Размер промежуточной картинки относительно подложки: 1 — 100%. Значение больше 1 выводит лучи за края подложки.")]
    [SerializeField, Min(0f)] private float decorationSize = 1f;
    [SerializeField] private Color color = new(0.18f, 0.65f, 1f, 1f);
    [SerializeField] private bool showButton = true;
    [SerializeField] private bool showDescription = true;
    [SerializeField] private LocalizationControl description = new();

    [Header("Нажатие")]
    [SerializeField] private ActionBase action;
    [Tooltip("Ключ метрики нажатия: отправляется как button → click → ключ. Пусто — без метрики.")]
    [SerializeField] private string metricKey;
    [SerializeField] private bool openWindow;
    [SerializeField] private EnumerationReference<MonoWindowKeyEnumerations> window = new();
    [Tooltip("Необязательная клавиша общего экранного интерфейса. None — только нажатие мышью или касанием.")]
    [SerializeField] private KeyCode hotkey = KeyCode.None;
    [Tooltip("Показывать обозначение клавиши на кнопке. Саму клавишу это не отключает.")]
    [SerializeField] private bool showHotkey = true;

    [Header("Уведомление")]
    [Tooltip("Когда привлекать внимание. Пусто — без уведомления. AssetCondition позволяет выбрать общее условие-ассет.")]
    [SerializeReference, ReferenceSelector] private ICondition attentionCondition;
    [SerializeField, Min(0.1f)] private float attentionInterval = 0.5f;
    [Tooltip("Своя картинка индикатора; пусто — красный значок с восклицательным знаком.")]
    [SerializeField] private Sprite attentionIcon;

    [Header("Таймер")]
    [Tooltip("Обратный отсчёт на кнопке, пока открываемое ею ещё недоступно. Пусто — без таймера.")]
    [SerializeReference, ReferenceSelector] private IButtonTimer timer;

    [Header("Эффекты")]
    [SerializeField] private UIButtonEffects effects = new();

    /// <summary>
    /// Спрайт иконки; без него слой иконки не отображается.
    /// </summary>
    public Sprite Icon => icon;
    /// <summary>
    /// Размер иконки относительно подложки; 1 соответствует всей подложке.
    /// </summary>
    public float IconSize => Mathf.Max(0f, iconSize);
    /// <summary>
    /// Подложка кнопки; без спрайта вид использует стандартное скругление.
    /// </summary>
    public Sprite Background => background;
    /// <summary>
    /// Необязательная картинка между подложкой и иконкой.
    /// </summary>
    public Sprite Decoration => decoration;
    /// <summary>
    /// Размер промежуточной картинки относительно подложки; значения больше 1 выходят за её края.
    /// </summary>
    public float DecorationSize => Mathf.Max(0f, decorationSize);
    /// <summary>
    /// Цвет подложки.
    /// </summary>
    public Color Color => color;
    /// <summary>
    /// Скрывает экранную кнопку, оставляя горячую клавишу рабочей.
    /// </summary>
    public bool ShowButton => showButton;
    /// <summary>
    /// Показывать локализованную подпись под иконкой.
    /// </summary>
    public bool ShowDescription => showDescription;
    /// <summary>
    /// Ключ нажатия в событии button с параметром click; пустое значение отключает отправку.
    /// </summary>
    public string MetricKey => metricKey;
    /// <summary>
    /// Необязательная клавиша общего HUD.
    /// </summary>
    public KeyCode Hotkey => hotkey;
    /// <summary>
    /// Показывать подсказку назначенной клавиши.
    /// </summary>
    public bool ShowHotkey => showHotkey && hotkey != KeyCode.None;
    /// <summary>
    /// Условие индикатора внимания, проверяемое без контекста отдельного игрока.
    /// </summary>
    public ICondition AttentionCondition => attentionCondition;
    /// <summary>
    /// Период опроса условия в секундах реального времени.
    /// </summary>
    public float AttentionInterval => Mathf.Max(0.1f, attentionInterval);
    /// <summary>
    /// Необязательная картинка уведомления.
    /// </summary>
    public Sprite AttentionIcon => attentionIcon;
    /// <summary>
    /// Необязательный источник обратного отсчёта.
    /// </summary>
    public IButtonTimer Timer => timer;
    /// <summary>
    /// Эффекты каждого созданного экземпляра кнопки.
    /// </summary>
    public UIButtonEffects Effects => effects;
    /// <inheritdoc />
    public string LocalizationKey => description?.LocalizationKey;
    /// <inheritdoc />
    public IReadOnlyDictionary<LangType, string> LocalizationValues => description?.LocalizationValues;
    /// <summary>
    /// Подпись с запасным переводом для ещё не заполненного ассета.
    /// </summary>
    public ILocalizationProvider Label => new UIButtonLabelProvider(this);

    /// <summary>
    /// Проверяет действие непосредственно перед нажатием.
    /// </summary>
    public virtual bool CanInvoke() => (action != null || openWindow) && (action == null || action.CanExecute().IsSuccess);

    /// <summary>
    /// Нажимает кнопку: выполняет её и сообщает о нажатии в шину.
    /// </summary>
    /// <remarks>
    /// Событие <see cref="IUIButtonPressedEvent"/> уходит на каждое нажатие, в том числе
    /// несработавшее: слушатель сам решает, считать ли его. Поэтому метод не виртуальный —
    /// своё поведение наследник задаёт в <see cref="Execute"/>, а событие не теряется.
    /// </remarks>
    /// <returns>Кнопка сработала: действие выполнено и окно открыто.</returns>
    public bool Invoke()
    {
        bool executed = Execute();

        UIButtonEvents.RaisePressed(this, executed);

        return executed;
    }

    /// <summary>
    /// Отправляет метрику допустимого нажатия, выполняет действие и открывает окно. Отказ действия не открывает окно.
    /// </summary>
    protected virtual bool Execute()
    {
        if (!CanInvoke())
            return false;

        if (!string.IsNullOrWhiteSpace(metricKey))
            PRUnitySDK.Metric?.Send("button", "click", metricKey);

        if (action != null && action.Execute().IsFailed)
            return false;

        return !openWindow || PRUnitySDK.Trackers.MonoWindows.TryShowWindow(window.ToEnumeration());
    }
}
