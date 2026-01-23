using System;
using UnityEngine;

/// <summary>
/// Сущность.
/// </summary>
public partial interface IEntity 
{
    /// <summary>
    /// Идентификатор сущности.
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// 
    /// </summary>
    public IEntityInfo Info { get; }

    /// <summary>
    /// Тип сущности.
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// Время жизни сущности.
    /// </summary>
    public EntityLifeTime LifeTime { get; }

    /// <summary>
    /// Находится сущность на сцене.
    /// </summary>
    public bool OnScene { get; }

    /// <summary>
    /// Находится сущность на пуле.
    /// </summary>
    public bool InPool { get; }

    /// <summary>
    /// Генерация идентификатора сущности.
    /// </summary>
    /// <param name="register">Регистратор.</param>
    public void GenerateId(Func<int> register);

    /// <summary>
    /// Уничтожения сущности.
    /// </summary>
    public void DestroyEntity();

    /// <summary>
    /// Уничтожения сущности.
    /// </summary>
    /// <param name="options">Параметры</param>
    public void DestroyEntity(EntityDestroyOptions options);

    /// <summary>
    /// Игровой объект от MonoBehaviour.
    /// В Unity gameObject идет с маленькой буквы.
    /// </summary>
    public GameObject gameObject { get; }

    /// <summary>
    /// Инициализация сущности.
    /// </summary>
    public void EntityInitialize();
}
