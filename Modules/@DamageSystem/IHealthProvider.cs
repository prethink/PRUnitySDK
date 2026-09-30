/// <summary>
/// Предоставляет компонент здоровья владельца.
/// </summary>
public interface IHealthProvider
{
    HealthComponent Health { get; }
}
