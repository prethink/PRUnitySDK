using System.Collections.Generic;
using UnityEngine;

public abstract class RewardDataBase : ScriptableObject, IIconProvider, ILocalizationProvider
{
    public abstract Sprite Icon { get; }

    public abstract string LocalizationKey { get; }

    public abstract IReadOnlyDictionary<LangType, string> LocalizationValues { get; }
}
