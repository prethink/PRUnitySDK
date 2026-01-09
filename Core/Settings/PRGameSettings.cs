using UnityEditor;

public partial class PRGameSettings : ResourceScriptableObject
{
    [MenuItem("Assets/Create/PRUnitySDK/Game Settings", false, 40)]
    public static void CreateGameSettings()
    {
        Create<PRGameSettings>();
    }

    protected override void SetDefaultSettings()
    {
        //TODO:
        this.RunMethodHooks(MethodHookStage.DefaultSettings);
    }
}
