/// <summary>
/// Как живёт сущность, когда её не видно и она дальше ближней зоны камеры (<see cref="EntityLodLevel.Hidden"/>).
/// </summary>
public enum HiddenSimulation
{
    /// <summary>
    /// Как задано в настройках проекта: <see cref="EntityLodSettings.HiddenSimulation"/>.
    /// </summary>
    ProjectDefault,

    /// <summary>
    /// Как рядом с игроком: вся физика и все проверки.
    /// </summary>
    Full,

    /// <summary>
    /// Упрощённо: вода, лестницы и действия рядом ищутся реже, тело без сглаживания. Всё, от чего зависит исход
    /// физики (ступеньки, стены, склоны, столкновения), проверяется полностью — бот проходит там же, где видимый.
    /// </summary>
    Simplified,
}
