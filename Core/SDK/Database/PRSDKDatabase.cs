using UnityEngine;

public partial class PRSDKDatabase : ScriptableObjectSingleton<PRSDKDatabase>
{
    [field: SerializeField] public ActionDatabase Actions { get; protected set; }
    [field: SerializeField] public SpriteDatabase Sprites { get; protected set; }
    [field: SerializeField] public SoundDatabase Sounds { get; protected set; }
    [field: SerializeField] public RewardDatabase Rewards { get; protected set; }
}
