using UnityEngine;

[CreateAssetMenu(fileName = "Reward resource", menuName = "PRUnitySDK/Reward/Reward resource")]
public class RewardResource : RewardDataBase
{
    [field: SerializeField] public int Count { get; protected set; }
    [field: SerializeField] public ResourceItemDefinitionBase ResourceData { get; protected set; }
}
