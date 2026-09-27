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
}
