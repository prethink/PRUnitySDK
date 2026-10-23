using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Правила характеристик проекта: ограничения, которые применяются последними.
/// </summary>
/// <remarks>
/// Лежат в <see cref="PRSDKSettings"/>, а значит у каждой игры (проекта SDK) свои. Удобнее править
/// в окне <c>PRUnitySDK/Windows/Game Rules</c>. Читает их <see cref="GameRules"/> при старте SDK.
/// </remarks>
[Serializable]
public class GameRulesSettings
{
    [SerializeReference, ReferenceSelector]
    [Tooltip("Ограничения характеристик. Применяются после базовых значений и модификаторов, по возрастанию приоритета.")]
    private List<StatRuleBase> rules = new();

    /// <summary>
    /// Правила в порядке списка; пустые элементы возможны и пропускаются при загрузке.
    /// </summary>
    public IReadOnlyList<StatRuleBase> Rules => rules;

#if UNITY_EDITOR
    /// <summary>
    /// Имя поля списка — для окна правил.
    /// </summary>
    public const string RulesFieldName = nameof(rules);
#endif
}
