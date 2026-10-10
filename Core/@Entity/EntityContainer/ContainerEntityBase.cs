using UnityEngine;

/// <summary>
/// Контейнер для хранения эффекта.
/// </summary>
[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public abstract class ContainerEntityBase : RuntimeEntityBase
{
    [SerializeField] protected PlayerTypeFlags canPickup;
    [SerializeField] protected Rigidbody rb;

    protected bool isTaken;
    public virtual bool CanPickup(PlayerBase player)
    {
        return (canPickup & PlayerBase.ConvertToFlag(player.PlayerType)) != 0 && !isTaken;
    }

    protected override void Awake()
    {
        base.Awake();
        
        rb ??= GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Подбор при входе в триггер.
    /// </summary>
    /// <remarks>
    /// Именно PR-хук, а не Unity-метод <c>OnTriggerEnter</c>: собственный Unity-callback
    /// обходит проверку логической паузы, и контейнеры подбирались бы при открытом окне.
    /// </remarks>
    protected override void PROnTriggerEnter(Collider other)
    {
        TryHandleCollision(other.gameObject);
    }

    /// <summary>
    /// Подбор при столкновении.
    /// </summary>
    protected override void PROnCollisionEnter(Collision collision)
    {
        TryHandleCollision(collision.gameObject);
    }

    protected void TryHandleCollision(GameObject obj)
    {
        if (!TryFindPlayer(obj, out PlayerBase player) || !CanPickup(player))
            return;

        isTaken = TryPickup(player);
        if (isTaken)
            DestroyEntity();
    }

    /// <summary>
    /// Игрок, которому принадлежит задетый объект.
    /// </summary>
    /// <remarks>
    /// Сущность игрока не всегда лежит на объекте с коллайдером или под ним: у составного
    /// игрока она стоит на корне, а коллайдер — на теле ниже. С тела на сущность ведёт
    /// <see cref="EntityLinkBase"/>; без этой ветки такого игрока контейнер не замечал.
    /// </remarks>
    protected virtual bool TryFindPlayer(GameObject obj, out PlayerBase player)
    {
        if (obj.TryGetComponentInChildren(out player))
            return true;

        player = obj.TryGetComponent(out EntityLinkBase link) ? link.Entity as PlayerBase : null;

        return player != null;
    }

    protected abstract bool TryPickup(PlayerBase player);

    public override void InitializationPoolObject()
    {
        isTaken = false;
        base.InitializationPoolObject();
    }
}

public abstract class ContainerEntityBase<T> : ContainerEntityBase
    where T : IIconProvider
{
    /// <summary>
    /// Фабрика для создания эффекта.
    /// </summary>
    [SerializeField] protected T containerItem;

    /// <summary>
    /// Обновить спрайт.
    /// </summary>
    protected override void InitializationComponents()
    {
        UpdateIcon();
        base.InitializationComponents();
    }

    protected virtual void UpdateIcon()
    {
        var spriteRender = gameObject.GetComponentInChildren<SpriteRenderer>();
        if (spriteRender == null)
            return;

        if(containerItem != null)
            spriteRender.sprite = containerItem.Icon;
    }
}

public abstract class PickupContainerBase<T> : ContainerEntityBase 
    where T : EntityBase
{
    [field: SerializeField] public Transform Container { get; protected set; }

    [SerializeField] protected T EntityContain;

    protected bool isActivate;

    public override bool CanPickup(PlayerBase player)
    {
        return base.CanPickup(player) && isActivate;
    }

    public void SetEntity(T entity)
    {
        EntityContain = entity;
        EntityContain.transform.SetParent(Container, false);
        EntityContain.transform.localPosition = Vector3.zero;
        EntityContain.transform.localRotation = Quaternion.identity;
        EntityContain.gameObject.SetActive(true);
        this.DelayAction(2f, (t) => 
        {
            rb.isKinematic = true;
            isActivate = true;
        });
    }

    public override void DestroyEntity()
    {
        EntityContain = null;
        base.DestroyEntity();
    }
}