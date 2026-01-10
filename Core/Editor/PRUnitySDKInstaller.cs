#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class PRUnitySDKInstaller
{
    private const string PREF_KEY = "PRUnitySDK.Installed";
    private const string SESSION_KEY = "PRUnitySDK.SessionInstalled";

    static PRUnitySDKInstaller()
    {
        if (SessionState.GetBool(SESSION_KEY, false))
            return;

        SessionState.SetBool(SESSION_KEY, true);

        if (!EditorPrefs.GetBool(PREF_KEY, false))
        {
            EditorApplication.delayCall += Install;
        }
    }

    private static void Install()
    {
        // твоя логика инициализации
       // SetupPrefabs();
        //SetupSettings();
        //SetupLayers();
        //SetupTags();

        EditorPrefs.SetBool(PREF_KEY, true);

        PRLog.WriteDebug("PRUnitySDKInstaller", "PRUnitySDK installed successfully");
    }

    // 👉 КНОПКА В МЕНЮ
    [MenuItem("PRUnitySDK/Reinstall SDK")]
    public static void Reinstall()
    {
        if (!EditorUtility.DisplayDialog(
                "Reinstall PRUnitySDK",
                "This will re-run SDK initialization.\nAre you sure?",
                "Reinstall",
                "Cancel"))
            return;

        EditorPrefs.DeleteKey(PREF_KEY);
        SessionState.EraseBool(SESSION_KEY);

        Install();

        EditorUtility.DisplayDialog(
            "PRUnitySDK",
            "SDK reinstalled successfully.",
            "Ok");
    }
}
#endif