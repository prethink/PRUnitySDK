using System;
using UnityEngine;

[Serializable]
public class DefaultSettings 
{
    /// <summary>
    /// Чувствительность мыши.
    /// </summary>
    [field: SerializeField] public float Sensitivity { get; protected set; } = 0.3f;

    /// <summary>
    /// Инвертировать горизонтальный ввод.
    /// </summary>
    [field: SerializeField] public bool InvertHorizontalInput { get; protected set; } = false;

    /// <summary>
    /// Инвертировать вертикальный ввод.
    /// </summary>
    [field: SerializeField] public bool InvertVerticalInput { get; protected set; } = false;

    /// <summary>
    /// Громкость общая.
    /// </summary>
    [field: SerializeField] public float MasterVolume { get; protected set; } = 1;

    /// <summary>
    /// Громкость музыка.
    /// </summary>
    [field: SerializeField] public float MusicVolume { get; protected set; } = 1;

    /// <summary>
    /// Громкость эффектов.
    /// </summary>
    [field: SerializeField] public float EffectVolume { get; protected set; } = 1;

    /// <summary>
    /// Признак, что музыка отключена.
    /// </summary>
    [field: SerializeField] public bool OffMusic { get; protected set; } = false;

    /// <summary>
    /// Признак, что звук выключен.
    /// </summary>
    [field: SerializeField] public bool OffSound { get; protected set; } = false;

    /// <summary>
    /// Громкость UI.
    /// </summary>
    [field: SerializeField] public float UIVolume { get; protected set; } = 1;

    /// <summary>
    /// Признак, что музыка отключена.
    /// </summary>
    [field: SerializeField] public bool OffEffect { get; protected set; } = false;

    /// <summary>
    /// Признак, что требуется показать курсор.
    /// </summary>
    [field: SerializeField] public bool IsShowCursor { get; protected set; } = true;
}
