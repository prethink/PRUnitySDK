using System;
using UnityEngine;

public partial class PRSDKSettings
{
    [field: SerializeField] public EntityLodSettings EntityLod { get; protected set; } = new();
}

/// <summary>
/// Детализация NPC, ботов и питомцев вдали от камеры.
/// </summary>
[Serializable]
[SettingsDescription("Детализация NPC, ботов и питомцев вдали от камеры: тени только вблизи, дальние в кадре " +
    "анимируются реже, невидимые могут жить упрощённо. Сущность и спавнер могут задать своё поведение невидимых.")]
public class EntityLodSettings
{
    [field: SerializeField, Tooltip("Упрощать сущности вдали от камеры. Выключено — все живут полностью, как рядом.")]
    public bool Enabled { get; protected set; } = true;

    [field: SerializeField, Min(1f),
     Tooltip("Ближе этого расстояния до камеры сущность живёт полностью, даже за спиной: с ней можно столкнуться.")]
    public float NearDistance { get; protected set; } = 15f;

    [field: SerializeField,
     Tooltip("Как живут сущности, которых не видно и которые дальше ближней зоны. Full — как рядом; Simplified — " +
             "движение без проверок ступенек и стен, реже поиск воды, лестниц и действий. " +
             "Своё значение можно задать у сущности (EntityLod).")]
    public HiddenSimulation HiddenSimulation { get; protected set; } = HiddenSimulation.Simplified;

    [field: SerializeField, Range(1, 4),
     Tooltip("Дальние сущности в кадре продвигают анимацию раз в столько кадров. 1 — каждый кадр.")]
    public int ReducedAnimationStep { get; protected set; } = 2;

    /// <summary>
    /// Поведение невидимых по умолчанию; <see cref="global::HiddenSimulation.ProjectDefault"/> здесь значит полное.
    /// </summary>
    public HiddenSimulation ResolveHiddenSimulation()
    {
        return HiddenSimulation == global::HiddenSimulation.Simplified
            ? global::HiddenSimulation.Simplified
            : global::HiddenSimulation.Full;
    }
}
