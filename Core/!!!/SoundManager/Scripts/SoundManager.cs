using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    #region Поля и свойства

    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource uiSource;

    [SerializeField] private Dictionary<Guid, AudioSource> effectsSources = new();

    [SerializeField] private Dictionary<string, Dictionary<string, AudioSet>> soundPool = new(StringComparer.OrdinalIgnoreCase);

    private HashSet<AudioClip> backgroundMusic = new HashSet<AudioClip>();
    private int currentIndexPlayBackgroundMusic = 0;
    private bool isInit;

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
        backgroundMusic.AddRange(PRUnitySDK.Database.Sounds.BackgroundMusic.Select(x => x.Value));
        PlayBackgroundMusic();

        //foreach (var container in resourceContainer.SoundManagerContainer.GetAllData)
        //{
        //    foreach (var item in container.Value)
        //        RegisterSoundList(container.GetKey(), item);
        //}
        isInit = true;
    }

    public IEnumerator UpdateSettings()
    {
        if (PRUnitySDK.Managers.GameManager.GetGameSettings().OffSound || AudioMixerManager.IsMute)
        {
            musicSource.volume = 0;
            uiSource.volume = 0;
            UpdateEffectVolume(0);
        }

        while (true)
        {
            yield return new WaitForSeconds(0.2f);
            var currentSettings = PRUnitySDK.Managers.GameManager.GetGameSettings();
            var masterVolume = currentSettings.MasterVolume;

            if (currentSettings.OffSound || AudioMixerManager.IsMute)
            {
                musicSource.volume = 0;
                UpdateEffectVolume(0);
                uiSource.volume = 0;
                continue;
            }

            if(currentSettings.OffMusic)
                musicSource.volume = 0;
            else
                musicSource.volume = Mathf.Clamp(currentSettings.MusicVolume, 0, masterVolume);

            UpdateEffectVolume(Mathf.Clamp(currentSettings.EffectVolume, 0, masterVolume));
            uiSource.volume = Mathf.Clamp(currentSettings.UIVolume, 0, masterVolume);
        }
    }

    private void UpdateEffectVolume(float volume)
    {
        effectsSource.volume = volume;
        foreach (var effect in effectsSources)
        {
            if (effect.Value != null)
                effect.Value.volume = volume;
        }
    }

    #endregion

    #region Методы

    public void RegisterSoundList(AudioSet audio)
    {
        var type = typeof(MonoBehaviour).ToString();
        RegisterSoundList(type, audio);
    }

    public void RegisterSoundList(Type type, AudioSet audio)
    {
        RegisterSoundList(type.ToString(), audio);
    }

    public void RegisterSoundList(Component component, AudioSet audio)
    {
        var type = component.GetType();
        RegisterSoundList(type.ToString(), audio);
    }

    public void RegisterSoundList(string type, AudioSet audio)
    {
        var category = audio.Key;
        if (soundPool.ContainsKey(type) && soundPool[type].ContainsKey(category))
            return;

        var queue = new Queue<PoolObject>();
        if (soundPool.ContainsKey(type))
        {
            if (soundPool[type] == null)
                soundPool[type] = CreateActionValue(category, audio);

            if (!soundPool[type].ContainsKey(category))
                soundPool[type].Add(category, audio);
        }
        else
        {
            soundPool[type] = CreateActionValue(category, audio);
        }
    }

    public void PlayEffectWithLifetime(Guid guid, AudioClip sound)
    {
        if(sound == null || effectsSources.ContainsKey(guid))
            return;

        var newAudioSource = gameObject.AddComponent<AudioSource>();
        newAudioSource.clip = sound;
        newAudioSource.loop = true;
        newAudioSource.Play();
        effectsSources.Add(guid, newAudioSource);
    }

    public void RemoveEffect(Guid guid)
    {
        if (effectsSources.ContainsKey(guid))
        {
            var audioSource = effectsSources[guid];
            effectsSources.Remove(guid);
            if (audioSource != null)
                Destroy(audioSource);
        }
    }


    public void PlaySoundEffectOneShot(AudioClip sound, Vector2? randomPitch = null)
    {
        PlaySoundEffectOneShot(sound, effectsSource.volume, randomPitch);
    }

    public void PlaySoundEffectOneShot(AudioClip sound, float volume, Vector2? randomPitch = null)
    {
        if (IsMute() || sound == null)
            return;

        effectsSource.pitch = randomPitch.HasValue
            ? randomPitch.Value.GetRandom()
            : 1f;

        effectsSource.PlayOneShot(sound, volume);
    }

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

        AudioSource.PlayClipAtPoint(sound, soundPosition, volume);
    }

    public void PlayClipAtPoint(AudioClip sound, Vector3 soundPosition)
    {
        PlayClipAtPoint(sound, soundPosition, effectsSource.volume);
    }

    public bool IsMute()
    {
        return AudioMixerManager.IsMute || GameManager.Instance.GetGameSettings().OffSound;
    }

    public void PlaySound(string category, Vector3? position = null)
    {
        var type = typeof(MonoBehaviour).ToString();
        PlaySound(type, category, position);
    }

    public void PlaySound(Type type, string category, Vector3? position = null)
    {
        PlaySound(type.ToString(), category, position);
    }

    public void PlaySound(Component component, string category, Vector3? position = null)
    {
        var type = component.GetType().ToString();
        PlaySound(type, category, position);
    }

    public void PlaySound(string type, string category, Vector3? position = null)
    {
        if (IsMute())
            return;

        category = category.ToLower();

        if (!soundPool.ContainsKey(type))
            throw new Exception($"Not found sound for type '{type}' in collection.");

        if (soundPool.ContainsKey(type) && !soundPool[type].ContainsKey(category))
            throw new Exception($"Not found audioset with category '{category}' for type '{type}'.");

        var audioCollection = soundPool[type][category];
        if (position != null)
            AudioSource.PlayClipAtPoint(audioCollection.AudioClips[UnityEngine.Random.Range(0, audioCollection.AudioClips.Count)], position.Value);
        else
        {
            if (audioCollection.SoundType == SoundType.Music)
                PlayOneShot(musicSource, audioCollection);
            else if (audioCollection.SoundType == SoundType.UI)
                PlayOneShot(uiSource, audioCollection);
            else
                PlayOneShot(effectsSource, audioCollection);
        }
    }

    private void PlayOneShot(AudioSource source, AudioSet set)
    {
        set.ApplySettings(source);
        source.Stop();
        source.PlayOneShot(set.AudioClips[UnityEngine.Random.Range(0, set.AudioClips.Count)]);
    }

    public void PlayBackgroundMusic()
    {
        if (backgroundMusic.Any())
        {
            musicSource.clip = backgroundMusic.ElementAt(currentIndexPlayBackgroundMusic);
            var settngs = GameManager.Instance.GetGameSettings();
            musicSource.volume = (settngs.OffSound || settngs.OffMusic) ? 0 : Mathf.Clamp(settngs.MusicVolume, 0 , settngs.MasterVolume);
            musicSource.loop = true;
            musicSource.Play();
            //StartCoroutine(WaitForMusicEnd());

            //currentIndexPlayBackgroundMusic++;
            //if (currentIndexPlayBackgroundMusic >= backgroundMusic.Count)
            //    currentIndexPlayBackgroundMusic = 0;
        }
    }

    private IEnumerator WaitForMusicEnd()
    {
        // Ждем до тех пор, пока музыка играет или стоит на паузе
        while (musicSource.clip != null && musicSource.time < musicSource.clip.length)
        {
            // Если стоит на паузе — ждем
            if (!musicSource.isPlaying)
                yield return null;
            else
                yield return null;
        }

        // Когда дошло до конца — запускаем следующую
        PlayBackgroundMusic();
    }

    private Dictionary<string, AudioSet> CreateActionValue(string category, AudioSet audios)
    {
        return new Dictionary<string, AudioSet>(StringComparer.OrdinalIgnoreCase)
        {
            { category, audios }
        };
    }

    public void Mute()
    {
        effectsSource.volume = 0;
        musicSource.volume = 0;
        uiSource.volume = 0;
    }

    public void UnMute()
    {
        effectsSource.volume = PRUnitySDK.Managers.GameManager.GetGameSettings().EffectVolume;
        musicSource.volume = PRUnitySDK.Managers.GameManager.GetGameSettings().MusicVolume;
        uiSource.volume = PRUnitySDK.Managers.GameManager.GetGameSettings().UIVolume;
    }

    #endregion

    public static SoundManager Factory()
    {
        return Instantiate(Resources.Load<SoundManager>($"{PRUnitySDK.CorePrefabsPath}/SoundManager"));
    }
}
