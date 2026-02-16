
using UnityEngine;

/// <summary>
/// Классы связи для сущностей. 
/// Позволяет легко находить сущность в текущем или родительских объектах, а также получать сущность определенного типа.
/// </summary>
public class EntityLink : PRMonoBehaviour 
{
    #region Поля и свойства

    /// <summary>
    /// Ссылка на сущность.
    /// </summary>
    [field: SerializeField] public EntityBase Entity { get; private set; }

    #endregion

    #region Базовый класс

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        TryFindEntity();
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void OnValidate()
    {
        base.OnValidate();

        TryFindEntity();
    }

    #endregion

    #region Методы

    /// <summary>
    /// Попытаться найти сущность в текущем или родительских объектах, если ссылка не была установлена вручную.
    /// </summary>
    private void TryFindEntity()
    {
        Entity ??= GetComponentInParent<EntityBase>();
    }

    /// <summary>
    /// Попытаться получить сущность определенного типа. Если сущность не соответствует типу, возвращает false и null.
    /// </summary>
    /// <typeparam name="T">Тип сущности.</typeparam>
    /// <param name="entity">Сущность.</param>
    /// <returns>True - удалось найти, false - не удалось найти.</returns>
    public bool TryGetEntity<T>(out T entity) where T : EntityBase
    {
        if (Entity is T typedEntity)
        {
            entity = typedEntity;
            return true;
        }

        entity = null;
        return false;
    }

    #endregion
}