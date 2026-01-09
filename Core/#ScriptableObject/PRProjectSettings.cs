using System;
using UnityEditor;
using UnityEngine;

public partial class PRProjectSettings : ResourceScriptableObject
{
    [field:SerializeField] public ReleaseType ReleaseType { get; protected set; }   

    [MenuItem("Assets/Create/PRUnitySDK/Settings/Project settings", false, 40)]
    public static void Create()
    {
        Create<PRProjectSettings>();
    }

    protected override void SetDefaultSettings()
    {
        //TODO:
        this.RunMethodHooks(MethodHookStage.DefaultSettings);
    }
}

[Serializable]
public enum ReleaseType
{
    Debug,
    Release
}