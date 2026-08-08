using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManagerData : MonoBehaviour
{
    [SerializeField] private List<SoundData> soundData;

    public void RegisterSound()
    {
        foreach (var sound in soundData) 
        {
            //TODO:soundManager.RegisterSoundList(this, sound.audioSet);
        }
    }
}


[Serializable]
public class SoundData
{
    public string key;
    public AudioSet audioSet;
}