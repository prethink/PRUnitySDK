using UnityEngine;

public abstract class EntityDefinition<TDefinition> : EntityBase<EntityMetadata> 
    where TDefinition : IEntityMetadata
{
    [field: SerializeField, Header("Definition")] public TDefinition Definition { get; private set; }
    public override string Name => GetName();

    protected override void InitializeEntityMetadata()
    {
        Description = new EntityDescription(Definition != null ? Definition : Metadata, this.GetComponent<IEntityMetadataProvider>()?.EntityMetadata);
    }

    protected virtual string GetName()
    {
        if(Definition != null)
            return Definition.GetTranslate();

        if(Metadata != null) 
            return Metadata.GetTranslate();

        return "NotInitialized";
    }
}
