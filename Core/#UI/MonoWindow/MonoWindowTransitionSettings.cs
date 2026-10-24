using System;
using UnityEngine;

/// <summary>
/// Переход окна: готовый пресет или свои значения.
/// </summary>
/// <remarks>
/// Им описан и переход проекта по умолчанию (<c>PRSDKSettings.WindowTransition</c>), и свой
/// переход окна (<c>Transition Override</c>).
/// </remarks>
[Serializable]
[SettingsDescription("Переход окон: как MonoWindow появляется и закрывается. Пресет — готовый набор, " +
                     "Custom — свои значения. Окно может взять свой переход или отключить его (Transition Mode у окна). " +
                     "Переход по умолчанию с превью — PRUnitySDK → Game → Window Transition.")]
[DatabaseExternalEditor(MenuPath, WindowName = "Переход окон")]
public class MonoWindowTransitionSettings
{
    /// <summary>
    /// Пункт меню окна перехода по умолчанию с превью.
    /// </summary>
    /// <remarks>
    /// Лежит в рантайме, а не рядом с окном: на путь ссылается атрибут настроек, а сборка
    /// редактора из рантайма не видна. Атрибут уводит из общего окна настроек только раздел
    /// проекта: свой переход окна по-прежнему правится на его префабе.
    /// </remarks>
    public const string MenuPath = "PRUnitySDK/Game/Window Transition";

    [field: SerializeField]
    [field: Tooltip("Окна появляются и закрываются плавно. Выключено — сразу.")]
    public bool Enabled { get; private set; } = true;

    [field: SerializeField]
    [field: Tooltip("Готовый переход. Custom — свои значения из поля ниже.")]
    public MonoWindowTransitionPreset Preset { get; private set; } = MonoWindowTransitionPreset.Pop;

    [field: SerializeField]
    [field: Tooltip("Свои значения перехода. Работают при Preset = Custom.")]
    public MonoWindowTransition Custom { get; private set; } = new();

    /// <summary>
    /// Переход, который надо играть, или <see langword="null"/>, если он выключен.
    /// </summary>
    public MonoWindowTransition Resolve()
    {
        if (!Enabled)
            return null;

        return Preset == MonoWindowTransitionPreset.Custom
            ? Custom ?? new MonoWindowTransition()
            : MonoWindowTransition.FromPreset(Preset) ?? MonoWindowTransition.FromPreset(MonoWindowTransitionPreset.Pop);
    }
}
