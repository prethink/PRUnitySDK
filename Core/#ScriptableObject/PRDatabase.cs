using UnityEditor;

public partial class PRDatabase : ResourceScriptableObject
{
    [MenuItem("Assets/Create/PRUnitySDK/Database", false, 40)]
    public static void CreateGameSettings()
    {
        Create<PRDatabase>();
    }

    protected override void SetDefaultSettings()
    {
        //TODO:
        this.RunMethodHooks(MethodHookStage.DefaultSettings);
    }
}
