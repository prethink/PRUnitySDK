using System;
using UnityEngine;

public partial class PRSDKSettings
{
    [field: SerializeField] public UIClickSoundSettings UIClickSound { get; protected set; } = new();
}

/// <summary>
/// Общий звук нажатия на элементы интерфейса.
/// </summary>
[Serializable]
[SettingsDescription("Общий звук нажатия: кнопки HUD, кнопки и переключатели внутри окон. Своё звучание кнопки его заменяет.")]
public class UIClickSoundSettings
{
    [field: SerializeField, Tooltip("Щёлкать при нажатии на элементы интерфейса.")]
    public bool Enabled { get; protected set; } = true;

    [field: SerializeField, Tooltip("Звук нажатия. Пусто — тишина.")]
    public AudioClip Clip { get; protected set; }

    [field: SerializeField, Range(0f, 1f), Tooltip("Громкость относительно громкости интерфейса в настройках игрока.")]
    public float Volume { get; protected set; } = 1f;
}
