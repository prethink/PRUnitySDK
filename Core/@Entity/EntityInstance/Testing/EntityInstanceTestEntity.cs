#if PRSDK_TESTS
/// <summary>
/// Настоящая сущность с записью экземпляра для проверок жизненного цикла.
/// </summary>
/// <remarks>
/// В редакторе <c>Awake</c> у компонента не вызывается, поэтому начало жизни и выдачу
/// из пула проверка запускает сама — теми же методами, что зовёт Unity и пул.
/// </remarks>
public class EntityInstanceTestEntity : Entity, IEntityInstance
{
    /// <summary>
    /// Определение, от имени которого сущность просит запись.
    /// </summary>
    public string DefinitionId;

    /// <inheritdoc />
    public EntityInstanceData Instance { get; private set; }

    /// <summary>
    /// Запись, которая стояла у сущности во время последней <c>InitializeEntity</c>.
    /// </summary>
    public EntityInstanceData InstanceSeenByInitialization { get; private set; }

    /// <inheritdoc />
    public EntityInstanceData CreateInstance()
    {
        return EntityInstanceData.Create(DefinitionId);
    }

    /// <inheritdoc />
    public void SetInstance(EntityInstanceData data)
    {
        Instance = data;
    }

    /// <summary>
    /// То, что делает <c>Awake</c>: первое появление сущности.
    /// </summary>
    public void AwakeForTest(EntityMetadata metadata)
    {
        Metadata = metadata;
        InitializationComponents();
    }

    /// <summary>
    /// То, что делает пул при повторной выдаче.
    /// </summary>
    public void ShowFromPoolForTest()
    {
        InitializeFromPool(isFirstPool: false);
    }

    protected override void InitializeEntity()
    {
        InstanceSeenByInitialization = Instance;
        base.InitializeEntity();
    }

    protected override void RegisterEventsOnCreated()
    {
    }
}
#endif
