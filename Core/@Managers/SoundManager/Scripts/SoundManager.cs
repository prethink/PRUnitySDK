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
    [Tooltip("До этого расстояния от слушателя позиционный звук идёт на полной громкости. Слушатель стоит " +
             "на камере, а камера от третьего лица — в нескольких метрах от персонажа: при меньшем значении " +
             "тихим оказывается всё, что звучит рядом с самим игроком.")]
    [SerializeField] private float positionalMinDistance = 6f;

    [Tooltip("На этом расстоянии от слушателя позиционный звук затихает полностью.")]
    [SerializeField] private float positionalMaxDistance = 30f;

    /// <summary>
    /// Пределы на случай, когда настроек проекта нет.
    /// </summary>
    private static readonly SoundLimitSettings DefaultLimits = new();

    /// <summary>
    /// Пределы позиционных звуков из настроек проекта.
    /// </summary>
    /// <remarks>
    /// Не в полях менеджера: подбирать их приходится на устройстве, и лезть ради этого в префаб
    /// или в код не нужно.
    /// </remarks>
    private static SoundLimitSettings Limits
    {
        get
        {
            PRSDKSettings settings = PRUnitySDK.Settings;
            return settings != null && settings.SoundLimits != null ? settings.SoundLimits : DefaultLimits;
        }
    }

    /// <summary>
    /// Когда какой клип последний раз запускался позиционно, по реальному времени.
    /// </summary>
    private readonly Dictionary<AudioClip, float> positionalClipTimes = new();

    /// <summary>
    /// Когда звук с каким ключом последний раз запускался, по реальному времени.
    /// </summary>
    private readonly Dictionary<int, float> positionalKeyTimes = new();

    /// <summary>
    /// С какого числа записей чистится <see cref="positionalKeyTimes"/>.
    /// </summary>
    /// <remarks>
    /// Ключом часто служит <c>GetInstanceID()</c> объекта, а объекты приходят и уходят: без чистки
    /// словарь рос бы всю сессию. Запись старше промежутка уже ни на что не влияет.
    /// </remarks>
    private const int KeyTimesPruneThreshold = 256;

    private readonly List<int> staleKeys = new();

    /// <summary>
    /// С каким ключом источник позиционного пула запускался последний раз; без записи или ноль - без ключа.
    /// </summary>
    private readonly Dictionary<AudioSource, int> positionalSourceKeys = new();

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
        // Линейное затухание: логарифмическое у дальней границы не доходит до нуля, и удары
        // ботов с другого конца карты так и звучали бы тихим фоном.
        source.rolloffMode = AudioRolloffMode.Linear;
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
    /// <remarks>
    /// В отличие от пула обычных эффектов, этот растёт не бесконечно: позиционные звуки -
    /// удары и шаги, их бывают десятки в секунду. Сверх предела новый звук пропускается,
    /// а не обрывает играющий: оборванный удар слышен как щелчок, пропущенный не слышен вовсе.
    /// </remarks>
    /// <param name="clip">Клип, который собираются играть: одинаковые клипы прореживаются.</param>
    /// <param name="limitKey">Ключ источника шума; ноль - звук без ключа, для него действует только общий предел.</param>
    /// <param name="important">
    /// Важный звук: ему доступен запас голосов сверх общего предела. Одинаковые клипы прореживаются
    /// и у важных - второй такой же звук в тот же миг ничего не добавляет.
    /// </param>
    /// <param name="source">Свободный источник.</param>
    /// <returns><c>false</c>, если звук нужно пропустить.</returns>
    private bool TryGetPositionalSource(AudioClip clip, int limitKey, bool important, out AudioSource source)
    {
        source = null;

        bool touch = IsTouchDevice();
        SoundLimitSettings limits = Limits;
        float clipInterval = limits.GetSameClipInterval(touch);
        float now = Time.unscaledTime;

        if (clip != null && clipInterval > 0f &&
            positionalClipTimes.TryGetValue(clip, out float lastClipTime) && now - lastClipTime < clipInterval)
            return false;

        bool keyed = limitKey != 0;

        if (keyed)
        {
            float interval = limits.GetKeyInterval(touch);

            if (interval > 0f && positionalKeyTimes.TryGetValue(limitKey, out float lastKeyTime) &&
                now - lastKeyTime < interval)
                return false;
        }

        int limit = limits.GetVoiceLimit(touch);

        // Ноль - «без предела», и запас его не меняет.
        if (important && limit > 0)
            limit += limits.GetImportantVoiceReserve(touch);

        int keyLimit = keyed ? limits.GetKeyVoiceLimit(touch) : 0;
        int playing = 0;
        int playingWithKey = 0;

        foreach (var pooled in positionalEffectsPool)
        {
            if (pooled == null)
                continue;

            if (!pooled.isPlaying)
            {
                source ??= pooled;
                continue;
            }

            playing++;

            if (keyed && positionalSourceKeys.TryGetValue(pooled, out int pooledKey) && pooledKey == limitKey)
                playingWithKey++;
        }

        if ((limit > 0 && playing >= limit) || (keyLimit > 0 && playingWithKey >= keyLimit))
        {
            source = null;
            return false;
        }

        if (source == null)
        {
            source = CreatePooledPositionalSource();
            positionalEffectsPool.Add(source);
        }

        positionalSourceKeys[source] = limitKey;

        if (clip != null)
            positionalClipTimes[clip] = now;

        if (keyed)
        {
            PruneKeyTimes(now, limits.GetKeyInterval(touch));
            positionalKeyTimes[limitKey] = now;
        }

        return true;
    }

    /// <summary>
    /// Убирает записи о ключах, которые давно не звучали.
    /// </summary>
    private void PruneKeyTimes(float now, float interval)
    {
        if (positionalKeyTimes.Count < KeyTimesPruneThreshold)
            return;

        staleKeys.Clear();

        foreach (KeyValuePair<int, float> pair in positionalKeyTimes)
        {
            if (now - pair.Value >= interval)
                staleKeys.Add(pair.Key);
        }

        foreach (int key in staleKeys)
            positionalKeyTimes.Remove(key);
    }

    /// <summary>
    /// Ключ предела из строки: одинаковые строки дают один ключ, пустая - ноль, то есть «без ключа».
    /// </summary>
    /// <remarks>
    /// Ключ - число, а не строка, чтобы источником шума мог быть и объект: тогда передают его
    /// <c>GetInstanceID()</c>. Строкой удобно задать общий ключ группе объектов в инспекторе.
    /// </remarks>
    public static int GetLimitKey(string name)
    {
        if (string.IsNullOrEmpty(name))
            return 0;

        int key = name.GetHashCode();

        // Ноль занят под «без ключа».
        return key != 0 ? key : 1;
    }

    /// <summary>
    /// Телефон или планшет. До готовности SDK сведений об устройстве нет - считаем, что нет.
    /// </summary>
    private static bool IsTouchDevice()
    {
        return PRUnitySDK.DeviceInfo != null && PRUnitySDK.DeviceInfo.IsTouchDevice();
    }

    /// <summary>
    /// Долгоживущий петлевой эффект с идентификатором, например гул двигателя.
    /// Останавливается только через <c>RemoveEffect</c>: подменить его другим звуком,
    /// как в пуле одноразовых, нельзя.
    /// </summary>
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
    /// <param name="limitKey">
    /// Ключ источника шума: звуков с одним ключом звучит не больше предела, остальные пропускаются.
    /// Свой у объекта - его <c>GetInstanceID()</c>, общий у группы - <see cref="GetLimitKey"/>.
    /// Ноль - без ключа: действует только общий предел позиционных голосов.
    /// </param>
    /// <param name="important">
    /// Звук, который нельзя терять: смерть цели, награда. Ему доступен запас голосов сверх общего предела
    /// (<see cref="SoundLimitSettings"/>), поэтому занятые ударами голоса его не глушат. Гарантии нет и тут:
    /// кончился запас либо такой же клип только что прозвучал - звук пропускается.
    /// </param>
    public void PlaySoundEffectAtPoint(AudioClip sound, Vector3 position, Vector2? randomPitch = null, float volume = 1f,
        int limitKey = 0, bool important = false)
    {
        if (IsMute() || sound == null || !TryGetPositionalSource(sound, limitKey, important, out AudioSource source))
            return;

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
        if (IsMute() || sound == null || !TryGetPositionalSource(sound, 0, false, out AudioSource source))
            return;

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
            AudioClip positionalClip = audioCollection.AudioClips[UnityEngine.Random.Range(0, audioCollection.AudioClips.Count)];

            if (!TryGetPositionalSource(positionalClip, 0, false, out AudioSource positionalSource))
                return;

            positionalSource.transform.position = position.Value;
            audioCollection.ApplySettings(positionalSource);
            positionalSource.PlayOneShot(positionalClip);
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