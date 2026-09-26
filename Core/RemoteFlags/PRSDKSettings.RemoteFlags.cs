using UnityEngine;

public partial class PRSDKSettings
{
    /// <summary>
    /// Флаги проекта по умолчанию.
    /// </summary>
    [field: SerializeField]
    public RemoteFlagsSettings RemoteFlags { get; private set; } = new();
}
