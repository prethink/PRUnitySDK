using UnityEngine;

public class EntityLinkBase<T> 
    : EntityLinkBase where T : EntityBase
{
    #region Поля и свойства

    /// <summary>
    /// Ссылка на сущность.
    /// </summary>
    [field: SerializeField] public T LinkedEntity { get; private set; }

    public override EntityBase Entity => LinkedEntity;

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
        LinkedEntity ??= GetComponentInParent<T>();
        LinkedEntity ??= GetComponent<T>();
    }

    /// <summary>
    /// Попытаться получить сущность определенного типа. Если сущность не соответствует типу, возвращает false и null.
    /// </summary>
    /// <typeparam name="T">Тип сущности.</typeparam>
    /// <param name="entity">Сущность.</param>
    /// <returns>True - удалось найти, false - не удалось найти.</returns>
    public bool TryGetEntity(out T entity) 
    {
        if (Entity is T typedEntity)
        {
            entity = typedEntity;
            return true;
        }

        entity = default;
        return false;
    }

    #endregion
}

public abstract class EntityLinkBase : PRMonoBehaviour
{
    public abstract EntityBase Entity { get; }
}
