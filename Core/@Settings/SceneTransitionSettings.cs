using System;
using UnityEngine;

[Serializable]
[SettingsDescription("Переход между сценами: затемнение на смене, его вид (режим, цвет, картинка, узор, звуки) и предел ожидания загрузчика.")]
public class SceneTransitionSettings 
{
    [field: SerializeField] public bool UseFadeOnChange { get; protected set; } = true;
    [field: SerializeField] public float LoaderTimeout { get; protected set; }

    [field: SerializeField, Tooltip("Вид затемнения ScreenFade: им закрываются и смена сцены, и переходы внутри сцены.")]
    public ScreenFadeSettings Fade { get; protected set; } = new();
}
