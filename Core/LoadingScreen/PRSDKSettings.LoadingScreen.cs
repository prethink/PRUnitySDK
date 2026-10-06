using UnityEngine;

public partial class PRSDKSettings
{
    /// <summary>
    /// Вид экрана загрузки при запуске игры.
    /// </summary>
    [field: SerializeField]
    public LoadingScreenSettings LoadingScreen { get; private set; } = new();
}
