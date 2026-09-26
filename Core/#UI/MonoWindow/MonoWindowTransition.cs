using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Параметры перехода окна: сколько длится, по какой кривой, откуда вырастает и откуда въезжает.
/// </summary>
/// <remarks>
/// Готовые наборы — <see cref="FromPreset"/>. Значения по умолчанию — пресет <see cref="MonoWindowTransitionPreset.Pop"/>,
/// поэтому ручная настройка начинается с того, что стоит у окон по умолчанию.
/// <para>
/// Свойства только на чтение: готовые наборы — общие экземпляры, и правка одного окна
/// не должна менять переход у всех.
/// </para>
/// </remarks>
[Serializable]
public class MonoWindowTransition
{
    /// <summary>
    /// Выпрыгивает из центра с лёгким перелётом. Пресет по умолчанию.
    /// </summary>
    private static readonly MonoWindowTransition PopPreset =
        new(0.3f, Ease.OutBack, 0.15f, Ease.InBack, 0f, true, Vector2.zero);

    /// <summary>
    /// Мягко проявляется, чуть подрастая.
    /// </summary>
    private static readonly MonoWindowTransition SoftPreset =
        new(0.25f, Ease.OutCubic, 0.15f, Ease.InCubic, 0.9f, true, Vector2.zero);

    /// <summary>
    /// Только проявляется и гаснет.
    /// </summary>
    private static readonly MonoWindowTransition FadePreset =
        new(0.2f, Ease.OutQuad, 0.15f, Ease.InQuad, 1f, true, Vector2.zero);

    /// <summary>
    /// Въезжает снизу вверх.
    /// </summary>
    private static readonly MonoWindowTransition SlideUpPreset =
        new(0.3f, Ease.OutCubic, 0.2f, Ease.InCubic, 1f, true, new Vector2(0f, -0.35f));

    /// <summary>
    /// Въезжает сверху вниз.
    /// </summary>
    private static readonly MonoWindowTransition SlideDownPreset =
        new(0.3f, Ease.OutCubic, 0.2f, Ease.InCubic, 1f, true, new Vector2(0f, 0.35f));

    /// <summary>
    /// Пружинит, выскакивая из центра.
    /// </summary>
    private static readonly MonoWindowTransition ElasticPreset =
        new(0.6f, Ease.OutElastic, 0.15f, Ease.InBack, 0f, true, Vector2.zero);

    [field: SerializeField, Min(0f)]
    [field: Tooltip("Сколько длится появление, в секундах.")]
    public float ShowDuration { get; private set; }

    [field: SerializeField, EasePreview]
    [field: Tooltip("Кривая появления. OutBack чуть перелетает размер и возвращается — окно «выпрыгивает».")]
    public Ease ShowEase { get; private set; }

    [field: SerializeField, Min(0f)]
    [field: Tooltip("Сколько длится закрытие, в секундах.")]
    public float HideDuration { get; private set; }

    [field: SerializeField, EasePreview]
    [field: Tooltip("Кривая закрытия.")]
    public Ease HideEase { get; private set; }

    [field: SerializeField, Range(0f, 1f)]
    [field: Tooltip("Масштаб, из которого окно вырастает и до которого сжимается, — доля от обычного. 1 — размер не меняется.")]
    public float HiddenScale { get; private set; }

    [field: SerializeField]
    [field: Tooltip("Вместе с масштабом проявлять и гасить окно целиком, с затемнением фона.")]
    public bool Fade { get; private set; }

    [field: SerializeField]
    [field: Tooltip("Откуда окно въезжает и куда уезжает — в долях его размера. (0, -0.35) — снизу, " +
                    "на треть высоты. Ноль — на месте.")]
    public Vector2 SlideOffset { get; private set; }

    /// <summary>
    /// Переход с настройками пресета <see cref="MonoWindowTransitionPreset.Pop"/>.
    /// </summary>
    public MonoWindowTransition()
        : this(0.3f, Ease.OutBack, 0.15f, Ease.InBack, 0f, true, Vector2.zero)
    {
    }

    /// <summary>
    /// Переход с указанными параметрами.
    /// </summary>
    public MonoWindowTransition(
        float showDuration,
        Ease showEase,
        float hideDuration,
        Ease hideEase,
        float hiddenScale,
        bool fade,
        Vector2 slideOffset)
    {
        ShowDuration = Mathf.Max(0f, showDuration);
        ShowEase = showEase;
        HideDuration = Mathf.Max(0f, hideDuration);
        HideEase = hideEase;
        HiddenScale = Mathf.Clamp01(hiddenScale);
        Fade = fade;
        SlideOffset = slideOffset;
    }

    /// <summary>
    /// Готовый переход по пресету.
    /// </summary>
    /// <returns>
    /// <see langword="null"/> для <see cref="MonoWindowTransitionPreset.Custom"/>: у него
    /// готовых значений нет, они лежат в настройках.
    /// </returns>
    public static MonoWindowTransition FromPreset(MonoWindowTransitionPreset preset)
    {
        return preset switch
        {
            MonoWindowTransitionPreset.Pop => PopPreset,
            MonoWindowTransitionPreset.Soft => SoftPreset,
            MonoWindowTransitionPreset.Fade => FadePreset,
            MonoWindowTransitionPreset.SlideUp => SlideUpPreset,
            MonoWindowTransitionPreset.SlideDown => SlideDownPreset,
            MonoWindowTransitionPreset.Elastic => ElasticPreset,
            _ => null,
        };
    }
}
