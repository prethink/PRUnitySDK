using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[SettingsDescription("Правила урона проекта: действуют на всех сценах, для любого удара. Правила отдельной сцены кладут компонентом DamageRuleComponent.")]
public class DamageRulesSettings
{
    [SerializeReference, ReferenceSelector]
    [Tooltip("Правила урона, которые действуют на всех сценах.")]
    private List<DamageRule> rules = new();

    /// <summary>
    /// Правила урона проекта.
    /// </summary>
    public IReadOnlyList<DamageRule> Rules => rules;
}
