using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Reward action", menuName = "PRUnitySDK/Reward/Reward action")]
public class RewardAction : RewardDataBase
{
    [SerializeField] private string id = Guid.NewGuid().ToString();
    [SerializeField, SpritePreview(140)] protected Sprite icon;
    [field: SerializeField] public ActionBase Action { get; protected set; }
    [field: SerializeField, SerializedDictionary("Lang", "Value")] public SerializedDictionary<LangType, string> localization { get; private set; }

    public override Sprite Icon => icon;

    public override string LocalizationKey => throw new System.NotImplementedException();

    public override IReadOnlyDictionary<LangType, string> LocalizationValues => localization;
}
