using System;
using UnityEngine;

[Serializable]
[SettingsDescription("Качества предметов: вес в случайной выдаче, цвет подписи и поправка к уровню. Вес — не проценты: шанс считается долей от суммы весов всех качеств, участвующих в розыгрыше.")]
public class PRQualitySettings
{
    [field: SerializeField] public bool UseDefaultColor { get; protected set; } = true;
    [field: SerializeField] public bool UseQualityLevelModifier { get; protected set; } = true;

    #region Common

    [field: SerializeField, Header("Common")] public ulong CommonWeight { get; protected set; } = 100;

    [field: SerializeField] public Color CommonColor { get; protected set; }

    [field: SerializeField] public long CommonLevel { get; protected set; } = 0;

    #endregion

    #region Uncommon

    [field: SerializeField, Header("UnCommon")] public ulong UncommonWeight { get; protected set; } = 20;

    [field: SerializeField] public Color UncommonColor { get; protected set; }

    [field: SerializeField] public long UncommonLevel { get; protected set; } = 10;

    #endregion

    #region Rare

    [field: SerializeField, Header("Rare")] public ulong RareWeight { get; protected set; } = 15;

    [field: SerializeField] public Color RareColor { get; protected set; }
    [field: SerializeField] public long RareLevel { get; protected set; } = 20;
    #endregion

    #region Mythic

    [field: SerializeField, Header("Mythic")] public ulong MythicWeight { get; protected set; } = 2;

    [field: SerializeField] public Color MythicColor { get; protected set; }
    [field: SerializeField] public long MythicLevel { get; protected set; } = 30;

    #endregion

    #region Epic

    [field: SerializeField, Header("Epic")] public ulong EpicWeight { get; protected set; } = 5;

    [field: SerializeField] public Color EpicColor { get; protected set; }

    [field: SerializeField] public long EpicLevel { get; protected set; } = 40;

    #endregion

    #region Legendary

    [field: SerializeField, Header("Legendary")] public ulong LegendaryWeight { get; protected set; } = 3;

    [field: SerializeField] public Color LegendaryColor { get; protected set; }

    [field: SerializeField] public long LegendaryLevel { get; protected set; } = 50;

    #endregion

    #region Ancient

    [field: SerializeField, Header("Ancient")] public ulong AncientWeight { get; protected set; } = 1;

    [field: SerializeField] public Color AncientColor { get; protected set; }

    [field: SerializeField] public long AncientLevel { get; protected set; } = 60;

    #endregion

    #region Godlike

    [field: SerializeField, Header("Godlike")] public ulong GodlikeWeight { get; protected set; } = 1;

    [field: SerializeField] public Color GodlikeColor { get; protected set; }

    [field: SerializeField] public long GodlikeLevel { get; protected set; } = 70;

    #endregion

    // Вес новых качеств по умолчанию ноль: общий розыгрыш качества в уже выпущенных играх
    // не должен начать выдавать их сам. Игра с расширенной шкалой задаёт вес явно.

    #region Cosmic

    [field: SerializeField, Header("Cosmic")] public ulong CosmicWeight { get; protected set; }

    [field: SerializeField] public Color CosmicColor { get; protected set; }

    [field: SerializeField] public long CosmicLevel { get; protected set; } = 80;

    #endregion

    #region Secret

    [field: SerializeField, Header("Secret")] public ulong SecretWeight { get; protected set; }

    [field: SerializeField] public Color SecretColor { get; protected set; }

    [field: SerializeField] public long SecretLevel { get; protected set; } = 90;

    #endregion

    #region Rainbow

    [field: SerializeField, Header("Rainbow")] public ulong RainbowWeight { get; protected set; }

    [field: SerializeField] public Color RainbowColor { get; protected set; }

    [field: SerializeField] public long RainbowLevel { get; protected set; } = 100;

    #endregion
}

