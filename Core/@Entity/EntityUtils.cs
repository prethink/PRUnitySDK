using System;

public static class EntityUtils
{
    public static EntityMetadataContainer GetEntityMetadata(ref IEntityMetadata baseEntityMetadata, ref IEntityMetadata overrideEntityMetadata, IEntityMetadata entity)
    {
        baseEntityMetadata = entity;
        overrideEntityMetadata = entity.GetComponent<IEntityMetadataProvider>()?.EntityMetadata;

        EntityMetadataContainer currentInfo; 

        if (baseEntityMetadata != null && overrideEntityMetadata != null)
        {
            currentInfo = new EntityMetadataContainer(baseEntityMetadata, overrideEntityMetadata);
        }
        else if (baseEntityMetadata != null)
        {
            currentInfo = new EntityMetadataContainer(baseEntityMetadata);
        }
        else if (overrideEntityMetadata != null)
        {
            currentInfo = new EntityMetadataContainer(overrideEntityMetadata);
        }
        else
            throw new InvalidOperationException("У сущности нет описания.");

        return currentInfo;
    }
}
