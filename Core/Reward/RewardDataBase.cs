using System.Collections.Generic;
using UnityEngine;

public abstract class RewardDataBase : ScriptableObject, IIconProvider, ILocalizationProvider
{
    public abstract Sprite Icon { get; }

    [field: SerializeField] public float ScaleSprite { get; protected set; } = 1f;
    [field: SerializeField] public Color BackgroundColor { get; protected set; }

    [field: SerializeField] public bool IsSecretGift { get; protected set; }

    [field: SerializeField] public QualityType QualityReward { get; protected set; }

    public abstract string LocalizationKey { get; }

    public abstract IReadOnlyDictionary<LangType, string> LocalizationValues { get; }

    public virtual QualityType GetQuality()
    {
        return QualityReward;
    }
}
