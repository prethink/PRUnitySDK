using System;

[Serializable]
public class EntityInfoDatabase : Database<EntityInfoBase>
{
    public static EntityInfoDatabase Instance => PRUnitySDK.Database.EntityInfo;
}