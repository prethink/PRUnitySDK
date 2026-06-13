using System;
using UnityEngine;

public partial class PRSDKSettings
{
    [field: SerializeField] public XPSettings ExperiencePoints { get; protected set; } = new();
}

[Serializable]
public class XPSettings
{
    [field: Header("XP система")]
    [Tooltip("—тартовое количество очков дл€ расчета уровней")]
    [field: SerializeField] public int BasePoints { get; protected set; } = 100;


    [field: Tooltip("«начение стартового уровн€")]
    [field: SerializeField] public int StartLevel { get; protected set; } = 1;

    [field: Tooltip("ћножитель дл€ расчета следующего уровн€")]
    [field: Range(1f, 5f)]
    [field: SerializeField] public float GrowthFactor { get; protected set; } = 1.5f;

    [field: SerializeField] public long PointForLevelUP { get; protected set; } = 1;

    public void SetDefaultSettings()
    {
        BasePoints = 100;
        StartLevel = 1;
        GrowthFactor = 1.5f;
        PointForLevelUP = 1;
    }
}