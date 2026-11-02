/// <summary>
/// Насколько подробно живёт сущность: зависит от того, видно ли её и далеко ли она от камеры.
/// </summary>
/// <remarks>
/// Уровень считает <see cref="EntityLodService"/>, а что упростить, решает каждый компонент сам
/// (<see cref="IEntityLodListener"/>).
/// </remarks>
public enum EntityLodLevel
{
    /// <summary>
    /// Ближе <see cref="EntityLodSettings.NearDistance"/> к камере, видно её или нет: всё как задумано.
    /// </summary>
    Full,

    /// <summary>
    /// В кадре, но далеко. Упрощается только вид: тени, частота анимации.
    /// </summary>
    Reduced,

    /// <summary>
    /// Вне кадра и далеко. Не видно ничего; поведение упрощается, если это разрешено
    /// (<see cref="HiddenSimulation.Simplified"/>).
    /// </summary>
    Hidden,
}
