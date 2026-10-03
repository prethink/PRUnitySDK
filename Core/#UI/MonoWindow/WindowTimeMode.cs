using UnityEngine;

/// <summary>
/// Что происходит со временем игры, пока окно открыто.
/// </summary>
public enum WindowTimeMode
{
    /// <summary>
    /// Игра идёт как обычно.
    /// </summary>
    [InspectorName("Не останавливать")] None = 0,

    /// <summary>
    /// Логическая пауза: мир стоит.
    /// </summary>
    [InspectorName("Пауза")] Pause = 1,

    /// <summary>
    /// Мир замедляется множителем на глобальном слое времени.
    /// </summary>
    [InspectorName("Замедлить время")] SlowTime = 2
}
