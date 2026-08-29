using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[DatabaseExternalEditor(
    "PRUnitySDK/Windows/Localization",
    WindowName = "Localization",
    Description = "Переводы правятся там: списки по языкам, проверки и обмен таблицей.")]
public class LocalizationDatabase 
{
    public static LocalizationDatabase Instance => PRUnitySDK.Database.LocalizationDatabase;
    [field: SerializeField] public LangType DefaultLanguage { get; protected set; }
    [field: SerializeField] public List<LocalizationControl> Common { get; protected set; } = new List<LocalizationControl>();
    [field: SerializeField] public List<LocalizationControl> Project { get; protected set; } = new List<LocalizationControl>();
}