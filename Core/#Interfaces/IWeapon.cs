/// <summary>
/// Базовый интерфейс для оружия.
/// </summary>
public partial interface IWeapon : IDevName, IDamageProvider
{
    /// <summary>
    /// Сущность, владеющая данным оружием (игрок, бот и т.п.).
    /// Используется как источник урона.
    /// </summary>
    EntityBase Owner { get; }
}
