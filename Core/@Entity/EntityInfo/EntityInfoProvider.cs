public class EntityInfoProvider : PRMonoBehaviour, IEntityInfoProvider
{
    public EntityInfoBase EntityInfoData { get; private set; }

    public IEntityInfo EntityInfo => EntityInfoData;
}
