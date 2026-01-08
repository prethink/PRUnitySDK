using UnityEditor;

public partial class PRGameSettings : ResourceScriptableObject
{
    [MenuItem("Assets/Create/PRUnitySDK/Game Settings", false, 40)]
    public static void CreateGameSettings()
    {
        CreateGameSettings<PRGameSettings>();
    }
}
