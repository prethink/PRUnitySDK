using UnityEngine;

public class ScriptableEntity : EntityBase
{
    public override Enumeration EntityType => new Enumeration(entityType);

    public override string Name => entityInfoData.Name;

    [SerializeField, Header("EntityInfo")] protected EntityInfoBase entityInfoData;
    [SerializeField] protected string entityType;

    protected override void InitializeEntityInfo()
    {
        Info = new EntityInfoContainer(entityInfoData);
    }
}
