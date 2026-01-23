using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SoundDatabase 
{
    [SerializeField] private List<KeyValueWrapper<string, AudioClip>> backgroundMusic;

    [SerializeField] private List<KeyValueWrapper<string, AudioClip>> ui;

    [SerializeField] private List<KeyValueWrapper<string, AudioClip>> common;

    #region PublicAPI

    public IReadOnlyCollection<KeyValueWrapper<string, AudioClip>> BackgroundMusic => backgroundMusic;
    public IReadOnlyCollection<KeyValueWrapper<string, AudioClip>> UI => ui;
    public IReadOnlyCollection<KeyValueWrapper<string, AudioClip>> Common => common;

    #endregion
}
