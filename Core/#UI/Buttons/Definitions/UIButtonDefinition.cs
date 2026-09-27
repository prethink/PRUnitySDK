using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Описание экранной кнопки. Состояние наблюдателя и анимации хранится в созданном UI.
/// </summary>
[CreateAssetMenu(menuName = "PRUnitySDK/UI/Button", fileName = "UIButton")]
public class UIButtonDefinition : ScriptableObject, IIconProvider, ILocalizationProvider
{
    [Header("Внешний вид")]
    [SerializeField, IconPreview] private ScriptableObject iconProvider;
    [SerializeField, SpritePreview(80)] private Sprite icon;
    [SerializeField] private Sprite background;
    [SerializeField] private Color color = new(0.18f, 0.65f, 1f, 1f);
    [SerializeField] private bool showButton = true;
    [SerializeField] private bool showDescription = true;
    [SerializeField] private LocalizationControl description = new();

    [Header("Нажатие")]
    [SerializeField] private ActionBase action;
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

    [Header("Эффекты")]
    [SerializeField] private UIButtonEffects effects = new();

    /// <summary>
    /// Иконка от ассета с IIconProvider; отдельный Sprite служит запасным вариантом.
    /// </summary>
    public Sprite Icon => iconProvider is IIconProvider provider && provider.Icon != null ? provider.Icon : icon;
    /// <summary>
    /// Подложка кнопки; без спрайта вид использует стандартное скругление.
    /// </summary>
    public Sprite Background => background;
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
    public virtual bool CanInvoke() => (action != null || openWindow) && (action == null || action.CanExecute());

    /// <summary>
    /// Выполняет действие, затем открывает окно. Отказ действия не открывает окно.
    /// </summary>
    public virtual bool Invoke()
    {
        if (!CanInvoke() || action != null && !action.Execute())
            return false;

        return !openWindow || PRUnitySDK.Trackers.MonoWindows.TryShowWindow(window.ToEnumeration());
    }
}
