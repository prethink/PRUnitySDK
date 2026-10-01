using System;
using UnityEngine;

/// <summary>
/// Настройки эффектов экранной кнопки; не содержат текущего состояния анимации.
/// </summary>
[Serializable]
public sealed class UIButtonEffects
{
    /// <summary>
    /// Звук успешного нажатия через общий SoundManager.
    /// </summary>
    [field: SerializeField] public AudioClip ClickSound { get; private set; }
    /// <summary>
    /// Угловая скорость иконки; ноль выключает вращение.
    /// </summary>
    [field: SerializeField, Tooltip("Градусы в секунду. 0 — не вращать иконку.")]
    public float IconRotationSpeed { get; private set; }
    /// <summary>
    /// Угловая скорость промежуточной картинки, независимая от вращения иконки.
    /// </summary>
    [field: SerializeField, Tooltip("Градусы в секунду. 0 — неподвижная картинка; знак меняет направление.")]
    public float DecorationRotationSpeed { get; private set; }
    /// <summary>
    /// Пульсировать, пока условие внимания выполнено.
    /// </summary>
    [field: SerializeField] public bool PulseWhenAttention { get; private set; } = true;
    /// <summary>
    /// Доля изменения масштаба при пульсации.
    /// </summary>
    [field: SerializeField, Range(0f, 0.2f)] public float PulseAmount { get; private set; } = 0.06f;
    /// <summary>
    /// Число циклов пульсации в секунду игрового времени.
    /// </summary>
    [field: SerializeField, Min(0.1f)] public float PulseSpeed { get; private set; } = 2f;
    /// <summary>
    /// Увеличивать кнопку, пока над ней курсор.
    /// </summary>
    /// <remarks>
    /// На телефоне наведения нет: касание даёт вход и выход курсора, и кнопка подрастает,
    /// пока палец на ней, — это работает как отклик на нажатие.
    /// </remarks>
    [field: SerializeField, Tooltip("Увеличивать кнопку, пока над ней курсор.")]
    public bool ScaleOnHover { get; private set; } = true;
    /// <summary>
    /// Во сколько раз увеличивается кнопка под курсором.
    /// </summary>
    [field: SerializeField, Range(1f, 1.3f), Tooltip("Во сколько раз увеличивается кнопка под курсором.")]
    public float HoverScale { get; private set; } = 1.08f;
    /// <summary>
    /// За сколько секунд кнопка доходит до размера наведения и возвращается обратно.
    /// </summary>
    [field: SerializeField, Min(0.01f), Tooltip("За сколько секунд кнопка увеличивается при наведении и возвращается обратно.")]
    public float HoverDuration { get; private set; } = 0.12f;
}
