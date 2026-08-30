using System;

public static class EntityUtils
{
    public static EntityDescription GetEntityMetadata(ref IEntityMetadata baseEntityMetadata, ref IEntityMetadata overrideEntityMetadata, IEntityMetadata entity)
    {
        baseEntityMetadata = entity;
        overrideEntityMetadata = entity.GetComponent<IEntityMetadataProvider>()?.EntityMetadata;

        EntityDescription currentInfo; 

        if (baseEntityMetadata != null && overrideEntityMetadata != null)
        {
            currentInfo = new EntityDescription(baseEntityMetadata, overrideEntityMetadata);
        }
        else if (baseEntityMetadata != null)
        {
            currentInfo = new EntityDescription(baseEntityMetadata);
        }
        else if (overrideEntityMetadata != null)
        {
            currentInfo = new EntityDescription(overrideEntityMetadata);
        }
        else
            throw new InvalidOperationException("У сущности нет описания.");

        return currentInfo;
    }
}
