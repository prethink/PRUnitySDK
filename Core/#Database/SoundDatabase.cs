using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[DatabaseExternalEditor(
    "PRUnitySDK/Windows/Common",
    WindowName = "Общее",
    Description = "Действия, спрайты и звуки правятся там.")]
public class SoundDatabase 
{
    public static SoundDatabase Instance => PRUnitySDK.Database.Sounds;

    [SerializeField] private List<KeyValueWrapper<string, AudioClip>> backgroundMusic;

    [SerializeField] private List<KeyValueWrapper<string, AudioClip>> ui;

    [SerializeField] private List<KeyValueWrapper<string, AudioClip>> common;

    #region PublicAPI

    public IReadOnlyCollection<KeyValueWrapper<string, AudioClip>> BackgroundMusic => backgroundMusic;
    public IReadOnlyCollection<KeyValueWrapper<string, AudioClip>> UI => ui;
    public IReadOnlyCollection<KeyValueWrapper<string, AudioClip>> Common => common;

    #endregion
}
