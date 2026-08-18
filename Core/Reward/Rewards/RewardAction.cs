using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Reward action", menuName = "PRUnitySDK/Reward/Reward action")]
public class RewardAction : RewardBase
{
    [SerializeField] private string id = Guid.NewGuid().ToString();
    [SerializeField, SpritePreview(140)] protected Sprite icon;
    [field: SerializeField] public ActionBase Action { get; protected set; }
    [field: SerializeField, SerializedDictionary("Lang", "Value")] public SerializedDictionary<LangType, string> localization { get; private set; }

    public override Sprite Icon => icon;

    public override string LocalizationKey => $"RewardAction_{id}";

    public override IReadOnlyDictionary<LangType, string> LocalizationValues => localization;

    public override bool IsConfigured => Action != null;

    public void InvokeAction()
    {
        Action?.Execute();
    }

    /// <summary>
    /// Настраивает action-награду и её данные отображения.
    /// </summary>
    /// <param name="action">Выполняемое действие.</param>
    /// <param name="rewardIcon">Иконка награды.</param>
    /// <param name="quality">Качество награды.</param>
    /// <param name="localizationValues">Переводы названия награды.</param>
    public void Initialize(
        ActionBase action,
        Sprite rewardIcon,
        QualityType quality,
        IReadOnlyDictionary<LangType, string> localizationValues)
    {
        Action = action;
        icon = rewardIcon;
        QualityReward = quality;

        localization ??= new SerializedDictionary<LangType, string>();
        localization.Clear();
        if (localizationValues == null)
            return;

        foreach (KeyValuePair<LangType, string> pair in localizationValues)
            localization[pair.Key] = pair.Value;
    }
}
