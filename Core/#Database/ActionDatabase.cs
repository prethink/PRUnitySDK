using System;

[Serializable]
[DatabaseExternalEditor(
    "PRUnitySDK/Windows/Common",
    WindowName = "Общее",
    Description = "Действия, спрайты и звуки правятся там.")]
public class ActionDatabase : Database<KeyValueWrapper<string, ActionBase>>
{
    public static ActionDatabase Instance => PRUnitySDK.Database.Actions;
}
