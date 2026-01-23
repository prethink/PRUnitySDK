using System;
using UnityEngine;

[Serializable]
public partial class DefaultControlSettings
{
    /// <summary>
    /// Чувствительность мыши.
    /// </summary>
    [field: SerializeField] public float Sensitivity { get; protected set; }

    /// <summary>
    /// Инвертировать горизонтальный ввод.
    /// </summary>
    [field: SerializeField] public bool InvertHorizontalInput { get; protected set; } = false;

    /// <summary>
    /// Инвертировать вертикальный ввод.
    /// </summary>
    [field: SerializeField] public bool InvertVerticalInput { get; protected set; } = false;
}