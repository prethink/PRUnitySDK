#if PRSDK_TESTS
/// <summary>
/// Настоящая сущность для проверок жизненного цикла: считает инициализации и держит запись экземпляра.
/// </summary>
/// <remarks>
/// В редакторе <c>Awake</c>, <c>OnEnable</c> и <c>OnDestroy</c> у компонента не вызываются, поэтому
/// эти моменты проверка запускает сама — теми же методами, что зовёт Unity.
/// </remarks>
public class EntityLifeTestEntity : Entity, IEntityInstance
{
    /// <summary>
    /// Определение, от имени которого сущность просит запись.
    /// </summary>
    public string DefinitionId = "life-test";

    /// <summary>
    /// Сколько раз прошла <c>InitializeEntity</c>.
    /// </summary>
    public int InitializeCount { get; private set; }

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
    /// Задаёт действие при уничтожении, как это делает инспектор.
    /// </summary>
    public void SetDisposeForTest(Enumeration action)
    {
        EntityDisposeAction.Set(action);
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
    /// То, что делает <c>Start</c>: регистрация в трекере.
    /// </summary>
    public void StartForTest()
    {
        RegisterEntity();
    }

    /// <summary>
    /// То, что делает <c>OnDestroy</c>: объект уничтожила Unity, а не <c>DestroyEntity</c>.
    /// </summary>
    public void UnityDestroyedForTest()
    {
        UnRegisterEventsOnDestroy();
    }

    /// <summary>
    /// Уничтожать объект сразу. По умолчанию он остаётся, как в игре до конца кадра: именно в это окно
    /// приходят повторные вызовы.
    /// </summary>
    public bool DestroyObjectNowForTest;

    /// <summary>
    /// Сколько раз сущность просила уничтожить свой объект.
    /// </summary>
    public int DestroyObjectRequests { get; private set; }

    protected override void DestroyEntityObject()
    {
        DestroyObjectRequests++;

        if (DestroyObjectNowForTest)
            base.DestroyEntityObject();
    }

    protected override void InitializeEntity()
    {
        InitializeCount++;
        InstanceSeenByInitialization = Instance;
        base.InitializeEntity();
    }

    protected override void RegisterEventsOnCreated()
    {
    }
}
#endif
