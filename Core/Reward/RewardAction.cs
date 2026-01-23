using UnityEngine;

[CreateAssetMenu(fileName = "Reward action", menuName = "PRUnitySDK/Reward/Reward action")]
public class RewardAction : RewardDataBase
{
    [field: SerializeField] public ActionBase Action { get; protected set; }
}
