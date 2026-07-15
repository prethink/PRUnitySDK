using System;

[Serializable]
public class ActionDatabase : Database<KeyValueWrapper<string, ActionBase>>
{
    public static ActionDatabase Instance => PRUnitySDK.Database.Actions;
}
