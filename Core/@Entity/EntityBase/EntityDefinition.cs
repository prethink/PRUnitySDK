using UnityEngine;

public abstract class EntityDefinition<TDefinition> : EntityBase, IEntityDefinitionReceiver 
    where TDefinition : IEntityInfo
{
    [field: SerializeField, Header("Definition")] public TDefinition Definition { get; private set; }
    public override string Name => Definition?.Name ?? "NotInitialized";

    protected override void InitializeEntityInfo()
    {
        Info = new EntityInfoContainer(Definition);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Пересобирает <c>Info</c>: он строится в <c>Awake</c> по тому определению, что лежит
    /// в префабе, и без пересборки сущность осталась бы с прежним именем и иконкой.
    /// </remarks>
    public bool TryAssignDefinition(IEntityInfo definition)
    {
        if (definition is not TDefinition typed)
            return false;

        Definition = typed;
        InitializeEntityInfo();
        return true;
    }
}
