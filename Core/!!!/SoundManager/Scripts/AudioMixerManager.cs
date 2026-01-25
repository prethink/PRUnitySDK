using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Менеджер управления аудиомикшером.
/// </summary>
public class AudioMixerManager : MonoBehaviour, IReadyGameEvent, IPauseStateListener
{
    public const string MASTER_MIXER = "Master";
    public const string MUSIC_MIXER = "Music";
    public const string EFFECT_MIXER = "Effects";
    public const string UI_MIXER = "UI";

    public const int DEBUG_LEVEL_LOG = 10;

    [SerializeField] private AudioMixer mixer;

    private float oldMasterValue;

    public static bool IsMute => IsUserMute || IsSystemMute;
    public static bool IsUserMute { get; private set; }
    public static bool IsSystemMute { get; private set; }

    private float ChangeValue(float value)
    {
        //return value > 0 ? Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f : -80f; // -80 dB = mute
        return Mathf.Clamp(value, 0.0001f, 1f);
    }

    // Установка громкости для музыки
    public void SetMusicVolume(float value)
    {
        //mixer.SetFloat(MUSIC_MIXER, ChangeValue(value));
    }

    // Установка громкости для эффектов
    public void SetEffectVolume(float value)
    {
       // mixer.SetFloat(EFFECT_MIXER, ChangeValue(value));
    }

    // Установка громкости для UI-звуков
    public void SetUIVolume(float value)
    {
        //mixer.SetFloat(UI_MIXER, ChangeValue(value));
    }

    public void SetMasterVolume(float value)
    {
        //mixer.SetFloat(MASTER_MIXER, ChangeValue(value));
    }

    public float GetMasterVolume()
    {
        mixer.GetFloat(MASTER_MIXER, out var volumeValue);
        return volumeValue;
    }

    public void MuteByUser(object executer, bool notify = true)
    {
        PRLog.WriteDebug(this, $"{nameof(MuteByUser)} : {executer.GetType()}", new PRLogSettings() { LevelDebug = DEBUG_LEVEL_LOG });
        if (IsUserMute)
            return;

        PRLog.WriteDebug(this, $"{nameof(MuteByUser)} : execute", new PRLogSettings() { LevelDebug = DEBUG_LEVEL_LOG });
        IsUserMute = true;

        if (!IsSystemMute)
            Mute(executer, notify);
    }

    public void UnMuteByUser(object executer, bool notify = true)
    {
        PRLog.WriteDebug(this, $"{nameof(UnMuteByUser)} : {executer.GetType()}", new PRLogSettings() { LevelDebug = DEBUG_LEVEL_LOG });
        IsUserMute = false;

        if (IsSystemMute)
            return;

        PRLog.WriteDebug(this, $"{nameof(UnMuteByUser)} : execute", new PRLogSettings() { LevelDebug = DEBUG_LEVEL_LOG });
        UnMute(executer, notify);
    }

    public void MuteBySystem(object executer, bool notify = true)
    {
        PRLog.WriteDebug(this, $"{nameof(MuteBySystem)} : {executer.GetType()}", new PRLogSettings() { LevelDebug = DEBUG_LEVEL_LOG });
        if (IsUserMute || IsSystemMute)
            return;

        PRLog.WriteDebug(this, $"{nameof(MuteBySystem)} : execute", new PRLogSettings() { LevelDebug = DEBUG_LEVEL_LOG });
        IsSystemMute = true;
        Mute(executer, notify);
    }

    public void UnMuteBySystem(object executer, bool notify = true) 
    {
        PRLog.WriteDebug(this, $"{nameof(UnMuteBySystem)} : {executer.GetType()}", new PRLogSettings() { LevelDebug = DEBUG_LEVEL_LOG });
        if (IsUserMute || !IsSystemMute)
            return;

        PRLog.WriteDebug(this, $"{nameof(UnMuteBySystem)} : execute", new PRLogSettings() { LevelDebug = DEBUG_LEVEL_LOG });
        IsSystemMute = false;
        UnMute(executer, notify);
    }


    private void Mute(object executer, bool notify = true)
    {
        var oldValue = GetMasterVolume();
        if(oldValue != 0)
            oldMasterValue = oldValue;

        SetMasterVolume(0);
        soundManager?.Mute();

        if (notify)
            PRUnitySDK.PauseManager.SetMusicPaused(true, executer, true);
    }

    private void UnMute(object executer, bool notify = true)
    {
        SetMasterVolume(oldMasterValue);
        soundManager?.UnMute();
        if (notify)
            PRUnitySDK.PauseManager.SetMusicPaused(false, executer);
    }

    public void RestoreSetting()
    {
        var settings = GameManager.Instance.GetGameSettings();
        SetEffectVolume(settings.EffectVolume);
        SetMusicVolume(settings.MusicVolume);
        SetUIVolume(settings.UIVolume);
        SetMasterVolume(settings.MasterVolume);
    }

    #region IReadyGameEvent

    public void OnReadyGame()
    {
        RestoreSetting();
    }

    #endregion

    public void OnPauseStateChanged(PauseEventArgs args)
    {
        if (args.Executer?.GetType() == typeof(GameManager))
        {
            if (PRUnitySDK.PauseManager.IsMusicPaused)
                MuteBySystem(args.Executer, false);
            else
                UnMuteBySystem(args.Executer, false);
        }
    }

    private SoundManager soundManager;
    public void RegisterSoundManager(SoundManager soundManager)
    {
        this.soundManager = soundManager;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(this);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(this);
    }

    public static AudioMixerManager Factory()
    {
        return Instantiate(Resources.Load<AudioMixerManager>($"{PRUnitySDK.CorePrefabsPath}/AudioMixer"));
    }
}
