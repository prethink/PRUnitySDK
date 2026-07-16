using System;

public static class EntityUtils
{
    public static EntityInfoContainer GetEntityInfo(ref IEntityInfo baseEntityInfo, ref IEntityInfo overrideEntityInfo, IEntityInfo entity)
    {
        baseEntityInfo = entity;
        overrideEntityInfo = entity.GetComponent<IEntityInfoProvider>()?.EntityInfo;

        EntityInfoContainer currentInfo; 

        if (baseEntityInfo != null && overrideEntityInfo != null)
        {
            currentInfo = new EntityInfoContainer(baseEntityInfo, overrideEntityInfo);
        }
        else if (baseEntityInfo != null)
        {
            currentInfo = new EntityInfoContainer(baseEntityInfo);
        }
        else if (overrideEntityInfo != null)
        {
            currentInfo = new EntityInfoContainer(overrideEntityInfo);
        }
        else
            throw new InvalidOperationException("Not have entity info");

        return currentInfo;
    }
}
