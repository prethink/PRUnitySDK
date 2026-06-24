public abstract class RewardItemBase : RewardDataBase
{
    public abstract ItemDefinitionBase Item { get; }

    public override QualityType GetQuality()
    {
        if (Item == null)
            return QualityReward;

        if (QualityReward.IsHigher(Item.Quality))
            return QualityReward;

        return Item.Quality;
    }
}
