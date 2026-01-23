using UnityEngine;

public interface IHealthEntity
{
    /// <summary>
    /// Максимальное количество здоровья.
    /// </summary>
    public float MaxHealth { get; }

    /// <summary>
    /// Текущее здоровье.
    /// </summary>
    public float Health { get; }

    /// <summary>
    /// Игровой объект.
    /// </summary>
    public EntityBase Entity { get; }

    /// <summary>
    /// Игровой объект.
    /// </summary>
    public GameObject GameObject { get; }

    /// <summary>
    /// Убийца
    /// </summary>
    public IEntity Killer { get; }

    /// <summary>
    /// Убить сущность.
    /// </summary>
    public bool Kill();

    /// <summary>
    /// Убить сущность..
    /// </summary>
    /// <param name="killer">Убийца.</param>
    public bool Kill(IEntity killer);

    /// <summary>
    /// Оживить entity.
    /// </summary>
    public void Revive();


    /// <summary>
    /// Оживить entity.
    /// </summary>
    /// <param name="transform">transform.</param>
    public void Revive(Transform transform);

    /// <summary>
    /// Оживить entity.
    /// </summary>
    /// <param name="position">Позиция.</param>
    public void Revive(Vector3 position);


    /// <summary>
    /// Оживить entity.
    /// </summary>
    /// <param name="health">Количество жизней при оживление.</param>
    public void Revive(float health);

    /// <summary>
    /// Оживить entity.
    /// </summary>
    /// <param name="health">Количество жизней при оживление.</param>
    /// <param name="transform">transform.</param>
    public void Revive(float health, Transform transform);

    /// <summary>
    /// Оживить entity.
    /// </summary>
    /// <param name="health">Количество жизней при оживление.</param>
    /// <param name="position">Позиция.</param>
    public void Revive(float health, Vector3 position);

    /// <summary>
    /// Оживить entity.
    /// </summary>
    /// <param name="reviver">Кто оживляет.</param>
    /// <param name="health">Количество жизней при оживление.</param>
    /// <param name="transform">transform.</param>
    public void Revive(IEntity reviver, float health, Transform transform);

    /// <summary>
    /// Оживить entity.
    /// </summary>
    /// <param name="reviver">Кто оживляет.</param>
    /// <param name="health">Количество жизней при оживление.</param>
    /// <param name="position">Позиция.</param>
    /// <param name="rotation">Поворот.</param>
    public void Revive(IEntity reviver, float health, Vector3 position, Quaternion rotation);

    /// <summary>
    /// Заспавнить сущность.
    /// </summary>
    /// <param name="spawnPosition">Точка спавна.</param>
    public void Spawn(Vector3 spawnPosition);

    /// <summary>
    /// Признак, что сущность жива.
    /// </summary>
    /// <returns>True - жива, False - мертва.</returns>
    public bool IsAlive();
}
