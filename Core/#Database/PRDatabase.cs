using UnityEditor;
using UnityEngine;

public partial class PRDatabase : ResourceScriptableObject
{
    [field: SerializeField] public SpriteDatabase SpriteDatabase { get; protected set; }
    [field: SerializeField] public ActionDatabase ActionDatabase { get; protected set; }

    [MenuItem("Assets/Create/PRUnitySDK/Database", false, 40)]
    public static void CreateGameSettings()
    {
        Create<PRDatabase>();
    }

    protected override void SetDefaultSettings()
    {
        this.RunMethodHooks(MethodHookStage.DefaultSettings);
    }
}
