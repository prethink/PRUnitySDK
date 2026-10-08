using UnityEngine;

public partial class PRSDKSettings
{
    /// <summary>
    /// Фоновая музыка: треки, порядок и повтор.
    /// </summary>
    [field: SerializeField]
    public BackgroundMusicSettings BackgroundMusic { get; private set; } = new();
}
