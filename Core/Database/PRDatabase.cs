using UnityEditor;

public partial class PRDatabase : ResourceScriptableObject
{
    [MenuItem("Assets/Create/PRUnitySDK/Database", false, 40)]
    public static void CreateGameSettings()
    {
        CreateGameSettings<PRDatabase>();
    }
}
