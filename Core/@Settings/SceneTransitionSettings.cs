using System;
using UnityEngine;

[Serializable]
[SettingsDescription("Переход между сценами: затемнение на смене и предел ожидания загрузчика.")]
public class SceneTransitionSettings 
{
    [field: SerializeField] public bool UseFadeOnChange { get; protected set; } = true;
    [field: SerializeField] public float LoaderTimeout { get; protected set; }
}
