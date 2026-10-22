#if !PRSDK_DISABLE_STEAM
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
using Steamworks;

/// <summary>
/// Имя игрока — его ник в Steam.
/// </summary>
public class SteamPlayerNameService : PlayerNameServiceBase
{
    protected override bool TryGetPlatformName(out string name)
    {
        name = SteamPlatform.IsReady ? SteamFriends.GetPersonaName() : null;
        return !string.IsNullOrWhiteSpace(name);
    }
}
#endif
#endif
