using UnityEngine;

public partial class PRSDKSettings : ScriptableObjectSingleton<PRSDKSettings>
{
    [field: SerializeField] public PRProjectSettings Project { get; protected set; }
}