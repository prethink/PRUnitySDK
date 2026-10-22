#if !PRSDK_DISABLE_STEAM
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
using System;
using Steamworks;
using UnityEngine;

/// <summary>
/// Запуск Steam и обслуживание его вызовов: <c>SteamAPI.Init</c>, <c>RunCallbacks</c> каждый кадр, пауза на оверлее, выход.
/// </summary>
/// <remarks>
/// Steam не поднялся (клиент не запущен, нет <c>steam_appid.txt</c> рядом с игрой в разработке) — игра работает без него:
/// сохранение остаётся на диске, имя и язык — по умолчанию. Сервисы Steam проверяют <see cref="IsReady"/>.
/// <para>
/// В релизе с <see cref="SteamSettings.RestartIfNotLaunchedFromSteam"/> игра, запущенная мимо Steam, закрывается
/// и перезапускается через него.
/// </para>
/// </remarks>
public sealed class SteamPlatform : MonoBehaviour
{
    private static SteamPlatform instance;
    private Callback<GameOverlayActivated_t> overlayCallback;

    /// <summary>
    /// Steam запущен, и к нему можно обращаться.
    /// </summary>
    public static bool IsReady { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        IsReady = false;
    }

    /// <summary>
    /// Запускает Steam. Повторный вызов ничего не делает.
    /// </summary>
    public static void Init()
    {
        if (instance != null)
            return;

        var host = new GameObject(nameof(SteamPlatform));
        DontDestroyOnLoad(host);
        instance = host.AddComponent<SteamPlatform>();
        instance.Startup();
    }

    private void Startup()
    {
        SteamSettings settings = PRUnitySDK.Settings.Steam;

#if !UNITY_EDITOR
        try
        {
            if (settings.RestartIfNotLaunchedFromSteam && SteamAPI.RestartAppIfNecessary(new AppId_t(settings.AppId)))
            {
                Application.Quit();
                return;
            }
        }
        catch (DllNotFoundException exception)
        {
            PRLog.WriteError(this, $"Нет steam_api: {exception.Message}");
            return;
        }
#endif

        try
        {
            ESteamAPIInitResult result = SteamAPI.InitEx(out string error);
            IsReady = result == ESteamAPIInitResult.k_ESteamAPIInitResult_OK;
            if (!IsReady)
            {
                PRLog.WriteWarning(this, $"Steam не запустился ({result}): {error}. Игра работает без Steam.");
                return;
            }
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            PRLog.WriteWarning(this, $"Steam не запустился: {exception.Message}. Игра работает без Steam.");
            return;
        }

        overlayCallback = Callback<GameOverlayActivated_t>.Create(OnOverlay);
    }

    /// <remarks>
    /// Оверлей (Shift+Tab) закрывает игру: время и звук стоят, пока он открыт.
    /// </remarks>
    private void OnOverlay(GameOverlayActivated_t data)
    {
        PRUnitySDK.PauseManager?.SetProjectPaused(data.m_bActive != 0, this);
    }

    private void Update()
    {
        if (IsReady)
            SteamAPI.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        if (!IsReady)
            return;

        IsReady = false;
        overlayCallback?.Dispose();
        SteamAPI.Shutdown();
    }
}
#endif
#endif
