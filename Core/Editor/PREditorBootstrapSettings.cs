using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PRUnitySDK/Editor/Editor Bootstrap Settings")]
public class PREditorBootstrapSettings : ScriptableObject
{
    public List<GameObject> SingletonPrefabs = new();
}