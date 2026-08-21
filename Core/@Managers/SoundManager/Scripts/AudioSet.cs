using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class AudioSet
{
    #region Константы

    private const float DEFAULT_VOLUME = 1f;
    private const float DEFAULT_PITCH = 1f;
    private const float DEFAULT_PAN_STEREO = 0f;
    private const float DEFAULT_SPATIAL_BLEND = 0f;
    private const float DEFAULT_REVERB_ZONE_MIX = 1f;
    private const float DEFAULT_DOPPLER_LEVEL = 1f;
    private const float DEFAULT_MAX_DISTANCE = 500f;
    private const float DEFAULT_MIN_DISTANCE = 1f;
    private const bool DEFAULT_LOOP = false;
    private const bool DEFAULT_PLAY_ON_AWAKE = false;
    private const bool DEFAULT_MUTE = false;

    #endregion

    #region Поля и свойства

    [SerializeField] private string key;
    [SerializeField] private List<AudioClip> audioClips;
    [SerializeField] private SoundType soundType;

    public string Key => key;
    public List<AudioClip> AudioClips => audioClips.ToList();
    public SoundType SoundType => soundType;

    [Header("Настройки для AudioSource")]
    public float Volume = DEFAULT_VOLUME;
    public float Pitch = DEFAULT_PITCH;
    public float PanStereo = DEFAULT_PAN_STEREO;

    [Header("Гибкие настройки")]
    public bool RandomPitch;

    //[MinMaxSlider(-3f,3f)]
    //public Vector2 MinMaxPitch;

    #endregion

    #region Методы

    public float GetPitch()
    {
        if(RandomPitch)
            return UnityEngine.Random.Range(-3, 3f);

        return Pitch;
    }

    public void ApplySettings(AudioSource source)
    {
        source.volume = Volume;
        source.pitch = GetPitch();
        source.panStereo = PanStereo;
    }

    public static void ApplyDefaultSettings(AudioSource source)
    {
        source.volume = DEFAULT_VOLUME;
        source.pitch = DEFAULT_PITCH;
        source.panStereo = DEFAULT_PAN_STEREO;
        source.spatialBlend = DEFAULT_SPATIAL_BLEND;
        source.reverbZoneMix = DEFAULT_REVERB_ZONE_MIX;
        source.dopplerLevel = DEFAULT_DOPPLER_LEVEL;
        source.maxDistance = DEFAULT_MAX_DISTANCE;
        source.minDistance = DEFAULT_MIN_DISTANCE;
        source.loop = DEFAULT_LOOP;
        source.playOnAwake = DEFAULT_PLAY_ON_AWAKE;
        source.mute = DEFAULT_MUTE;
    }

    #endregion

    #region Конструкторы

    public AudioSet(List<AudioClip> audioClips, SoundType soundType) 
        : this(typeof(MonoBehaviour).ToString(), audioClips, soundType) { }

    public AudioSet(string key, List<AudioClip> audioClips, SoundType soundType)
    {
        this.key = key;
        this.audioClips = audioClips.ToList();
        this.soundType = soundType; 
    }

    #endregion
}