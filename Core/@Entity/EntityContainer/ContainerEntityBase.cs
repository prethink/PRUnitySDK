using UnityEngine;

/// <summary>
/// Контейнер для хранения эффекта.
/// </summary>
[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public abstract class ContainerEntityBase : EntityBase 
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

    protected virtual void OnTriggerEnter(Collider other)
    {
        TryHandleCollision(other.gameObject);
    }

    protected override void PROnCollisionEnter(Collision collision)
    {
        TryHandleCollision(collision.gameObject);
    }

    protected void TryHandleCollision(GameObject obj)
    {
        if (obj.TryGetComponentInChildren<PlayerBase>(out var player))
        {
            if (!CanPickup(player))
                return;

            isTaken = TryPickup(player);
            if (isTaken)
                DestroyEntity();
        }
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