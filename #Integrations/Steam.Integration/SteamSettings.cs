#if !PRSDK_DISABLE_STEAM
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Настройки Steam для PC-сборки.
/// </summary>
/// <remarks>
/// Настройки не зависят от платформы сборки: код Steam собирается только под PC, а поля настроек должны жить всегда,
/// иначе при сохранении ассета под WebGL они пропали бы.
/// </remarks>
[Serializable]
[SettingsDescription("Steam в PC-сборке: App ID, перезапуск через Steam, файл облачного сохранения, связь достижений игры с достижениями Steam.")]
public sealed class SteamSettings
{
    /// <summary>
    /// App ID тестовой игры Valve (Spacewar): работает у любого, у кого есть Steam.
    /// </summary>
    public const uint SpacewarAppId = 480;

    [Tooltip("App ID игры из Steamworks. 480 — тестовая игра Valve (Spacewar), для проверки до регистрации.")]
    [SerializeField] private uint appId = SpacewarAppId;

    [Tooltip("Запущенная не из Steam игра закрывается и перезапускается через Steam. Включать в релизе: так работают " +
             "DRM и оверлей. В редакторе не действует.")]
    [SerializeField] private bool restartIfNotLaunchedFromSteam;

    [Tooltip("Имя файла сохранения в Steam Cloud и рядом с игрой. В Steamworks для облака нужна квота (Steam Cloud → Byte quota).")]
    [SerializeField] private string saveFileName = "prsave.json";

    [Tooltip("Какие достижения игры дублируются в Steam: GUID достижения из базы и API-имя достижения в Steamworks.")]
    [SerializeField] private List<SteamAchievementLink> achievements = new();

    public uint AppId => appId;

    public bool RestartIfNotLaunchedFromSteam => restartIfNotLaunchedFromSteam;

    public string SaveFileName => string.IsNullOrWhiteSpace(saveFileName) ? "prsave.json" : saveFileName.Trim();

    public IReadOnlyList<SteamAchievementLink> Achievements => achievements;
}

/// <summary>
/// Достижение игры и его пара в Steamworks.
/// </summary>
[Serializable]
public sealed class SteamAchievementLink
{
    [Tooltip("GUID достижения в базе проекта (PRSDKDatabase → Achievements).")]
    [SerializeField] private string achievementGuid;

    [Tooltip("API-имя достижения в Steamworks (Stats & Achievements → Achievements → API Name).")]
    [SerializeField] private string steamApiName;

    public string AchievementGuid => achievementGuid?.Trim() ?? string.Empty;

    public string SteamApiName => steamApiName?.Trim() ?? string.Empty;
}

public partial class PRSDKSettings
{
    /// <summary>
    /// Steam в PC-сборке.
    /// </summary>
    [field: SerializeField] public SteamSettings Steam { get; private set; } = new();
}
#endif
