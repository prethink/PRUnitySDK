using UnityEngine;

public class ScriptableEntity : EntityBase
{
    public override Enumeration EntityType => new Enumeration(entityType);

    public override string Name => entityInfoData.Name;

    [SerializeField, Header("EntityMetadata")] protected EntityMetadataBase entityInfoData;
    [SerializeField] protected string entityType;

    protected override void InitializeEntityMetadata()
    {
        Info = new EntityMetadataContainer(entityInfoData);
    }
}
