using UnityEngine;

/// <summary>
/// Сущность, обладающая здоровьем: её можно убить, воскресить и заспавнить.
/// <para>
/// Нанесение урона в контракт не входит - за него отвечает <see cref="IDamageable"/>.
/// Разделение позволяет объекту принимать урон, не имея здоровья (хитбокс передаёт
/// его владельцу), и наоборот - иметь здоровье, которое меняется только скриптами.
/// </para>
/// </summary>
public interface IHealthEntity
{
    /// <summary>
    /// Максимальное здоровье.
    /// </summary>
    public float MaxHealth { get; }

    /// <summary>
    /// Текущее здоровье.
    /// </summary>
    public float Health { get; }

    /// <summary>
    /// Сущность-владелец здоровья.
    /// </summary>
    public EntityBase Entity { get; }

    /// <summary>
    /// Игровой объект сущности.
    /// </summary>
    public GameObject GameObject { get; }

    /// <summary>
    /// Кто нанёс смертельный урон. Null, если сущность жива или погибла без источника.
    /// </summary>
    public IEntity Killer { get; }

    /// <summary>
    /// Убить сущность без указания источника.
    /// </summary>
    /// <returns>Была ли сущность убита этим вызовом.</returns>
    public bool Kill();

    /// <summary>
    /// Убить сущность с указанием источника - он попадёт в <see cref="Killer"/>
    /// и в событие смерти.
    /// </summary>
    /// <returns>Была ли сущность убита этим вызовом.</returns>
    public bool IsKill(IEntity killer);

    /// <summary>
    /// Воскресить на текущем месте с восстановлением здоровья.
    /// </summary>
    public void Revive();

    /// <summary>
    /// Воскресить в позиции и повороте указанного трансформа.
    /// </summary>
    public void Revive(Transform transform);

    /// <summary>
    /// Воскресить в указанной точке.
    /// </summary>
    public void Revive(Vector3 position);

    /// <summary>
    /// Воскресить на текущем месте с заданным здоровьем.
    /// </summary>
    public void Revive(float health);

    /// <summary>
    /// Воскресить с заданным здоровьем в позиции трансформа.
    /// </summary>
    public void Revive(float health, Transform transform);

    /// <summary>
    /// Воскресить с заданным здоровьем в указанной точке.
    /// </summary>
    public void Revive(float health, Vector3 position);

    /// <summary>
    /// Воскресить с указанием того, кто воскресил, - источник попадёт в событие.
    /// </summary>
    public void Revive(IEntity reviver, float health, Transform transform);

    /// <summary>
    /// Воскресить с указанием источника, здоровья, позиции и поворота.
    /// </summary>
    public void Revive(IEntity reviver, float health, Vector3 position, Quaternion rotation);

    /// <summary>
    /// Появление сущности в точке: в отличие от воскрешения применяется к живой
    /// сущности при входе в игру.
    /// </summary>
    public void Spawn(Vector3 spawnPosition);

    /// <summary>
    /// Жива ли сущность.
    /// </summary>
    public bool IsAlive();
}
