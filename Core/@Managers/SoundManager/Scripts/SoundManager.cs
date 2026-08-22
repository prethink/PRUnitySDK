using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    #region Поля и свойства

    [SerializeField] private AudioSource effectsSource; // шаблон настроек для пула + fallback
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource uiSource;

    [Header("Пул источников для одноразовых эффектов")]
    [Tooltip("Каждый одновременно играющий эффект получает свой AudioSource из пула - " +
             "иначе pitch одного эффекта 'протекает' в уже играющий другой (pitch - " +
             "свойство всего AudioSource, а не отдельного voice внутри PlayOneShot), " +
             "и второй эффект не обрывает первый (раньше это делал source.Stop()).")]
    [SerializeField] private int effectsPoolInitialSize = 4;

    [Header("Пул позиционных (3D) эффектов")]
    [Tooltip("Отдельный пул от обычных эффектов - эти источники физически расставляются " +
             "в точке звука (шаги, удары и т.п.), поэтому нужен spatialBlend = 1. " +
             "Важно для игры с несколькими игроками: каждый источник в пуле переиспользуется " +
             "и просто переставляется в новую позицию, когда освобождается - никаких " +
             "Instantiate/Destroy на каждый шаг, даже при частых шагах у многих игроков одновременно.")]
    [SerializeField] private int positionalEffectsPoolInitialSize = 8;
    [SerializeField] private float positionalMinDistance = 1f;
    [SerializeField] private float positionalMaxDistance = 25f;

    private readonly List<AudioSource> effectsPool = new();
    private readonly List<AudioSource> positionalEffectsPool = new();

    private readonly Dictionary<Guid, AudioSource> loopingEffectSources = new();
    private readonly Dictionary<string, Dictionary<string, AudioSet>> soundPool = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<AudioClip> backgroundMusic = new();
    private int currentIndexPlayBackgroundMusic;
    private bool isInit;
    private Coroutine musicWatcherCoroutine;

    #endregion

    #region MonoBehaviour

    protected void Start()
    {
        if (!isInit)
            StartWork();
    }

    public void OnReadyGame()
    {
        if (!isInit)
            StartWork();
    }

    private void StartWork()
    {
        StartCoroutine(UpdateSettings());

        backgroundMusic.Clear();
        backgroundMusic.AddRange(PRUnitySDK.Database.Sounds.BackgroundMusic.Select(x => x.Value));

        PrewarmEffectsPool();
        PlayBackgroundMusic();

        isInit = true;
    }

    /// <summary>Создаёт стартовый набор источников для пула эффектов заранее,
    /// чтобы первые же несколько одновременных звуков не создавали AudioSource
    /// прямо в момент проигрывания.</summary>
    private void PrewarmEffectsPool()
    {
        for (int i = 0; i < effectsPoolInitialSize; i++)
            effectsPool.Add(CreatePooledEffectSource());

        for (int i = 0; i < positionalEffectsPoolInitialSize; i++)
            positionalEffectsPool.Add(CreatePooledPositionalSource());
    }

    private AudioSource CreatePooledEffectSource()
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f; // 2D - обычные UI-подобные эффекты без позиции в мире
        source.volume = effectsSource != null ? effectsSource.volume : 1f;
        return source;
    }

    /// <summary>Источник для позиционных эффектов - отдельный дочерний GameObject
    /// (не на самом SoundManager), т.к. его Transform будет физически переставляться
    /// в точку звука при каждом вызове PlaySoundEffectAtPoint.</summary>
    private AudioSource CreatePooledPositionalSource()
    {
        var go = new GameObject("PositionalEffectSource");
        go.transform.SetParent(transform);

        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f; // полноценный 3D-звук
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = positionalMinDistance;
        source.maxDistance = positionalMaxDistance;
        source.volume = effectsSource != null ? effectsSource.volume : 1f;
        return source;
    }

    public IEnumerator UpdateSettings()
    {
        ApplyVolumeSettings(PRUnitySDK.Managers.Game.GetGameSettings());

        while (true)
        {
            yield return new WaitForSeconds(0.2f);
            ApplyVolumeSettings(PRUnitySDK.Managers.Game.GetGameSettings());
        }
    }

    private void ApplyVolumeSettings(GameSettings currentSettings)
    {
        var masterVolume = currentSettings.MasterVolume;

        if (currentSettings.OffSound || AudioMixerManager.IsMute)
        {
            musicSource.volume = 0;
            uiSource.volume = 0;
            UpdateEffectVolume(0);
            return;
        }

        musicSource.volume = currentSettings.OffMusic ? 0 : Mathf.Clamp(currentSettings.MusicVolume, 0, masterVolume);
        uiSource.volume = Mathf.Clamp(currentSettings.UIVolume, 0, masterVolume);
        UpdateEffectVolume(Mathf.Clamp(currentSettings.EffectVolume, 0, masterVolume));
    }

    private void UpdateEffectVolume(float volume)
    {
        if (effectsSource != null)
            effectsSource.volume = volume;

        foreach (var source in effectsPool)
        {
            if (source != null)
                source.volume = volume;
        }

        foreach (var source in positionalEffectsPool)
        {
            if (source != null)
                source.volume = volume;
        }

        foreach (var effect in loopingEffectSources.Values)
        {
            if (effect != null)
                effect.volume = volume;
        }
    }

    #endregion

    #region Регистрация звуков

    public void RegisterSoundList(AudioSet audio)
    {
        RegisterSoundList(typeof(MonoBehaviour).ToString(), audio);
    }

    public void RegisterSoundList(Type type, AudioSet audio)
    {
        RegisterSoundList(type.ToString(), audio);
    }

    public void RegisterSoundList(Component component, AudioSet audio)
    {
        RegisterSoundList(component.GetType().ToString(), audio);
    }

    public void RegisterSoundList(string type, AudioSet audio)
    {
        var category = audio.Key;

        if (soundPool.TryGetValue(type, out var categories))
        {
            if (!categories.ContainsKey(category))
                categories.Add(category, audio);
        }
        else
        {
            soundPool[type] = new Dictionary<string, AudioSet>(StringComparer.OrdinalIgnoreCase) { { category, audio } };
        }
    }

    #endregion

    #region Одноразовые эффекты (пул)

    /// <summary>Возвращает свободный (не играющий сейчас) источник из пула эффектов,
    /// либо создаёт новый, если все заняты - пул растёт по требованию под пиковую
    /// нагрузку и дальше переиспользуется, не создавая источники заново каждый раз.</summary>
    private AudioSource GetFreeEffectSource()
    {
        foreach (var source in effectsPool)
        {
            if (source != null && !source.isPlaying)
                return source;
        }

        var newSource = CreatePooledEffectSource();
        effectsPool.Add(newSource);
        return newSource;
    }

    /// <summary>Аналог GetFreeEffectSource, но для позиционного пула - источник не
    /// перемещается, пока реально играет (проверка isPlaying), поэтому переиспользование
    /// никогда не "переставит" звук, который уже кто-то слушает в процессе.</summary>
    private AudioSource GetFreePositionalSource()
    {
        foreach (var source in positionalEffectsPool)
        {
            if (source != null && !source.isPlaying)
                return source;
        }

        var newSource = CreatePooledPositionalSource();
        positionalEffectsPool.Add(newSource);
        return newSource;
    }

    /// <summary>Долгоживущий (петлевой) эффект с собственным идентификатором -
    /// например, гул двигателя, который нужно явно остановить через RemoveEffect.
    /// Отдельный механизм от пула одноразовых - этому источнику нельзя внезапно
    /// подменяться другим звуком, пока RemoveEffect не вызван явно.</summary>
    public void PlayEffectWithLifetime(Guid guid, AudioClip sound)
    {
        if (sound == null || loopingEffectSources.ContainsKey(guid))
            return;

        var newAudioSource = gameObject.AddComponent<AudioSource>();
        newAudioSource.clip = sound;
        newAudioSource.loop = true;
        newAudioSource.volume = effectsSource != null ? effectsSource.volume : 1f;
        newAudioSource.Play();
        loopingEffectSources.Add(guid, newAudioSource);
    }

    public void RemoveEffect(Guid guid)
    {
        if (!loopingEffectSources.TryGetValue(guid, out var audioSource))
            return;

        loopingEffectSources.Remove(guid);

        if (audioSource != null)
            Destroy(audioSource);
    }

    public void PlaySoundEffectOneShot(AudioClip sound, Vector2? randomPitch = null)
    {
        PlaySoundEffectOneShot(sound, effectsSource != null ? effectsSource.volume : 1f, randomPitch);
    }

    public void PlaySoundEffectOneShot(AudioClip sound, float volume, Vector2? randomPitch = null)
    {
        if (IsMute() || sound == null)
            return;

        var source = GetFreeEffectSource();
        source.pitch = randomPitch.HasValue ? randomPitch.Value.GetRandom() : 1f;
        source.PlayOneShot(sound, volume);
    }

    /// <summary>
    /// Позиционный (3D) одноразовый эффект - например, шаги, удары, любые звуки,
    /// у которых важно направление/расстояние до слушателя. В отличие от
    /// PlaySoundEffectOneShot, источник физически ставится в position - при
    /// нескольких игроках/источниках звука одновременно будет слышно, откуда
    /// именно идёт каждый звук. Использует отдельный позиционный пул (см.
    /// positionalEffectsPool) - переиспользуемые источники, без Instantiate/Destroy
    /// на каждый вызов, поэтому безопасно дёргать часто и от многих игроков сразу.
    /// </summary>
    public void PlaySoundEffectAtPoint(AudioClip sound, Vector3 position, Vector2? randomPitch = null, float volume = 1f)
    {
        if (IsMute() || sound == null)
            return;

        var source = GetFreePositionalSource();
        source.transform.position = position;
        source.pitch = randomPitch.HasValue ? randomPitch.Value.GetRandom() : 1f;
        source.PlayOneShot(sound, volume);
    }

    #endregion

    #region UI и позиционные звуки

    public void PlaySoundUIOneShot(AudioClip sound, float volume)
    {
        if (IsMute() || sound == null)
            return;

        uiSource.PlayOneShot(sound, volume);
    }

    public void PlaySoundUIOneShot(AudioClip sound)
    {
        PlaySoundUIOneShot(sound, uiSource.volume);
    }

    public void PlayClipAtPoint(AudioClip sound, Vector3 soundPosition, float volume)
    {
        if (IsMute() || sound == null)
            return;

        var source = GetFreePositionalSource();
        source.transform.position = soundPosition;
        source.pitch = 1f;
        source.PlayOneShot(sound, volume);
    }

    public void PlayClipAtPoint(AudioClip sound, Vector3 soundPosition)
    {
        PlayClipAtPoint(sound, soundPosition, effectsSource != null ? effectsSource.volume : 1f);
    }

    #endregion

    #region Категоризированное воспроизведение

    public bool IsMute()
    {
        return AudioMixerManager.IsMute || PRUnitySDK.Managers.Game.GetGameSettings().OffSound;
    }

    public void PlaySound(string category, Vector3? position = null)
    {
        PlaySound(typeof(MonoBehaviour).ToString(), category, position);
    }

    public void PlaySound(Type type, string category, Vector3? position = null)
    {
        PlaySound(type.ToString(), category, position);
    }

    public void PlaySound(Component component, string category, Vector3? position = null)
    {
        PlaySound(component.GetType().ToString(), category, position);
    }

    public void PlaySound(string type, string category, Vector3? position = null)
    {
        if (IsMute())
            return;

        category = category.ToLower();

        if (!soundPool.TryGetValue(type, out var categories) || !categories.TryGetValue(category, out var audioCollection))
        {
            PRLog.WriteWarning(typeof(SoundManager), $"Sound not found: type='{type}', category='{category}'.");
            return;
        }

        if (position != null)
        {
            var positionalSource = GetFreePositionalSource();
            positionalSource.transform.position = position.Value;
            audioCollection.ApplySettings(positionalSource);
            positionalSource.PlayOneShot(audioCollection.AudioClips[UnityEngine.Random.Range(0, audioCollection.AudioClips.Count)]);
            return;
        }

        switch (audioCollection.SoundType)
        {
            case SoundType.Music:
                // Музыка - не одноразовый эффект, а полноценная смена текущего трека,
                // поэтому здесь осознанно Stop+Play (а не PlayOneShot) - два трека
                // одновременно на musicSource звучать не должны.
                audioCollection.ApplySettings(musicSource);
                musicSource.Stop();
                musicSource.loop = false;
                musicSource.clip = audioCollection.AudioClips[UnityEngine.Random.Range(0, audioCollection.AudioClips.Count)];
                musicSource.Play();
                break;

            case SoundType.UI:
                audioCollection.ApplySettings(uiSource);
                uiSource.PlayOneShot(audioCollection.AudioClips[UnityEngine.Random.Range(0, audioCollection.AudioClips.Count)]);
                break;

            default:
                var source = GetFreeEffectSource();
                audioCollection.ApplySettings(source);
                source.PlayOneShot(audioCollection.AudioClips[UnityEngine.Random.Range(0, audioCollection.AudioClips.Count)]);
                break;
        }
    }

    #endregion

    #region Фоновая музыка

    public void PlayBackgroundMusic()
    {
        if (backgroundMusic.Count == 0)
            return;

        PlayCurrentTrack();

        // Persistent-наблюдатель запускается один раз, а не при каждом PlayBackgroundMusic -
        // сам крутится вечно и переключает треки по мере естественного завершения.
        if (musicWatcherCoroutine == null)
            musicWatcherCoroutine = StartCoroutine(MusicPlaylistWatcher());
    }

    private void PlayCurrentTrack()
    {
        musicSource.clip = backgroundMusic[currentIndexPlayBackgroundMusic];

        var settings = PRUnitySDK.Managers.Game.GetGameSettings();
        musicSource.volume = (settings.OffSound || settings.OffMusic) ? 0 : Mathf.Clamp(settings.MusicVolume, 0, settings.MasterVolume);
        musicSource.loop = false; // зацикливаем ПЛЕЙЛИСТ целиком через watcher, а не один трек
        musicSource.Play();
    }

    /// <summary>
    /// Следит за естественным завершением текущего трека и переключает на следующий.
    /// КРИТИЧНО: во время логической паузы (PRUnitySDK.PauseManager.IsLogicPaused)
    /// проверка пропускается - раньше (в исходной версии) пауза, останавливающая
    /// AudioSource, приводила к ложному "трек закончился" и он перезапускался с
    /// начала. Явный Pause()/UnPause() синхронизирован с тем же флагом в Update().
    /// </summary>
    private IEnumerator MusicPlaylistWatcher()
    {
        while (true)
        {
            yield return null;

            if (backgroundMusic.Count == 0)
                continue;

            if (PRUnitySDK.PauseManager.IsLogicPaused)
                continue;

            if (musicSource.isPlaying)
                continue;

            currentIndexPlayBackgroundMusic = (currentIndexPlayBackgroundMusic + 1) % backgroundMusic.Count;
            PlayCurrentTrack();
        }
    }

    private bool wasLogicPausedLastFrame;

    private void Update()
    {
        bool isPaused = PRUnitySDK.PauseManager.IsLogicPaused;

        if (isPaused == wasLogicPausedLastFrame)
            return;

        wasLogicPausedLastFrame = isPaused;

        if (isPaused)
            musicSource.Pause();
        else
            musicSource.UnPause();
    }

    #endregion

    #region Mute

    public void Mute()
    {
        if (effectsSource != null)
            effectsSource.volume = 0;

        foreach (var source in effectsPool)
        {
            if (source != null)
                source.volume = 0;
        }

        foreach (var source in positionalEffectsPool)
        {
            if (source != null)
                source.volume = 0;
        }

        musicSource.volume = 0;
        uiSource.volume = 0;
    }

    public void UnMute()
    {
        var settings = PRUnitySDK.Managers.Game.GetGameSettings();
        UpdateEffectVolume(settings.EffectVolume);
        musicSource.volume = settings.MusicVolume;
        uiSource.volume = settings.UIVolume;
    }

    #endregion
}

public class SoundManagerFactory : SingletonMonoBehaviourFactoryBase<SoundManager>
{
    public override string ResourcePath => $"{PRUnitySDK.ResourcePaths.PrefabsPath}/SoundManager";
}