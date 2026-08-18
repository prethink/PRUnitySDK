using System;

/// <summary>
/// Запись награды и её относительного веса внутри контейнера.
/// </summary>
[Serializable]
public sealed record WeightedRewardEntry : WeightItem<RewardBase>;
