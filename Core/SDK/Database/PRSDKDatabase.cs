using UnityEngine;

public partial class PRSDKDatabase : ScriptableObjectSingleton<PRSDKDatabase>
{
    [field: SerializeField] public ActionDatabase Actions { get; protected set; } = new();
    [field: SerializeField] public SpriteDatabase Sprites { get; protected set; } = new();
    [field: SerializeField] public SoundDatabase Sounds { get; protected set; } = new();
    [field: SerializeField] public EntityMetadataDatabase EntityMetadata { get; protected set; } = new();
}
