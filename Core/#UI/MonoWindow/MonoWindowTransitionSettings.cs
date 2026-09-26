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
                     "Custom — свои значения. Окно может взять свой переход или отключить его (Transition Mode у окна).")]
public class MonoWindowTransitionSettings
{
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
