using System;
using System.Collections.Generic;

[Serializable]
/// <summary>
/// Игровые настройки.
/// </summary>
public class GameSettings : ICloneable
{
    #region Константы

    public const string KEYBOARD_KEY = "<Keyboard>/";

    #endregion

    #region Поля и свойства

    /// <summary>
    /// Чувствительность мыши.
    /// </summary>
    public float Sensitivity { get; set; }

    /// <summary>
    /// Инвертировать горизонтальный ввод.
    /// </summary>
    public bool InvertHorizontalInput { get; set; } = false;

    /// <summary>
    /// Инвертировать вертикальный ввод.
    /// </summary>
    public bool InvertVerticalInput { get; set; } = false;

    /// <summary>
    /// Громкость общая.
    /// </summary>
    public float MasterVolume { get; set; }

    /// <summary>
    /// Громкость музыка.
    /// </summary>
    public float MusicVolume { get; set; }

    /// <summary>
    /// Громкость эффектов.
    /// </summary>
    public float EffectVolume { get; set; }

    /// <summary>
    /// Признак, что музыка отключена.
    /// </summary>
    public bool OffMusic { get; set; }

    /// <summary>
    /// Признак, что звук выключен.
    /// </summary>
    public bool OffSound { get; set; }

    /// <summary>
    /// Громкость UI.
    /// </summary>
    public float UIVolume { get; set; }

    /// <summary>
    /// Признак, что музыка отключена.
    /// </summary>
    public bool OffEffect { get; set; }

    /// <summary>
    /// Признак, что требуется показать курсор.
    /// </summary>
    public bool IsShowCursor { get; set; }

    /// <summary>
    /// Переопределенные кнопки.
    /// </summary>
    public Dictionary<string, string> OverrideButtons = new();

    #endregion

    #region ICloneable

    public object Clone()
    {
        return new GameSettings()
        {
            Sensitivity = Sensitivity,
            MusicVolume = MusicVolume,
            EffectVolume = EffectVolume,
            MasterVolume = MasterVolume,
            InvertHorizontalInput = InvertHorizontalInput,
            InvertVerticalInput = InvertVerticalInput,
            OffMusic = OffMusic,
            OffSound = OffSound,
            OffEffect = OffEffect,
            OverrideButtons = new Dictionary<string, string>(OverrideButtons),
            IsShowCursor = IsShowCursor
        };
    }

    #endregion

    #region Конструкторы

    public GameSettings()
    {
        Sensitivity = 0f;
        MusicVolume = 0.2f;
        EffectVolume = 1.0f;
        UIVolume = 1.0f;
        MasterVolume = 1.0f;
        OffSound = false;
        OffMusic = false;
        OverrideButtons = new Dictionary<string, string>();
        IsShowCursor = true;
    }

    #endregion
}