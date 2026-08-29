using UnityEngine;

public abstract class EntityDefinition<TDefinition> : EntityBase 
    where TDefinition : IEntityMetadata
{
    [field: SerializeField, Header("Definition")] public TDefinition Definition { get; private set; }
    public override string Name => Definition?.Name ?? "NotInitialized";

    protected override void InitializeEntityMetadata()
    {
        Info = new EntityMetadataContainer(Definition);
    }
}
