using UnityEngine;

/// <summary>
/// Драйвер масштаба времени тела, берущий слой у сущности.
/// <para>
/// Сущность уже знает свой слой через <see cref="EntityBase.GetTimeScaleLayer"/> - тот же
/// источник использует анимация и расчёт скорости. Отдельное поле слоя на драйвере
/// пришлось бы держать в согласии с сущностью вручную, и рассинхрон проявился бы
/// как физика, идущая не в том темпе, что анимация.
/// </para>
/// </summary>
public class EntityTimeScaleDriver : RigidbodyTimeScaleDriverBase
{
    [Header("Сущность")]
    [Tooltip("Сущность - источник слоя времени. Если пусто, берётся из EntityLink, " +
        "а при его отсутствии ищется на этом объекте и в родителях.")]
    [SerializeField] private EntityBase entity;

    [Tooltip("Линк на сущность. Заполняется автоматически, если найден на объекте или в родителях.")]
    [SerializeField] private EntityLinkBase entityLink;

    /// <summary>
    /// Сущность, у которой драйвер спрашивает слой.
    /// </summary>
    public EntityBase Entity => GetEntity();

    protected override void InitializationComponents()
    {
        ResolveEntity();

        if (entity == null && entityLink == null)
        {
            PRLog.WriteWarning(this, $"{nameof(EntityTimeScaleDriver)} on '{name}' has no entity: " +
                "the body will follow the global time scale.");
        }

        base.InitializationComponents();
    }

    /// <inheritdoc />
    protected override Enumeration GetTimeScaleLayer()
    {
        var target = GetEntity();

        return target != null ? target.GetTimeScaleLayer() : null;
    }

    /// <summary>
    /// Сущность, у которой берётся слой.
    /// <para>
    /// Приоритет у явно заданной ссылки, затем у <see cref="EntityLinkBase"/>: линк
    /// переназначается в рантайме (объект из пула, смена владельца), поэтому его
    /// значение читается каждый раз, а не запоминается при инициализации.
    /// </para>
    /// </summary>
    private EntityBase GetEntity()
    {
        if (entity != null)
            return entity;

        return entityLink != null ? entityLink.Entity : null;
    }

    private void ResolveEntity()
    {
        if (entity != null)
            return;

        // Линк ищется раньше самой сущности: он и есть штатный способ связать
        // объект с сущностью, когда тело лежит не на её корне.
        if (entityLink == null)
        {
            entityLink = GetComponent<EntityLinkBase>();
            entityLink ??= GetComponentInParent<EntityLinkBase>();
        }

        if (entityLink != null && entityLink.Entity != null)
            return;

        entity = GetComponentInParent<EntityBase>();
    }

    /// <summary>
    /// Привязать драйвер к другой сущности в рантайме - например, при переиспользовании
    /// объекта из пула. Явно заданная сущность имеет приоритет над линком.
    /// </summary>
    public void SetEntity(EntityBase value)
    {
        entity = value;
        ApplyScaleChange();
    }

    /// <summary>
    /// Привязать драйвер к линку сущности.
    /// </summary>
    public void SetEntityLink(EntityLinkBase value)
    {
        entityLink = value;
        ApplyScaleChange();
    }
}
