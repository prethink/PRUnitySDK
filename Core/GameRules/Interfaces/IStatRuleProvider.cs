using System.Collections.Generic;

/// <summary>
/// ѕредоставл€ет набор правил дл€ характеристик (stat rules).
/// </summary>
public interface IStatRuleProvider 
{
    /// <summary>
    /// »м€ набора правил, используетс€ дл€ идентификации источника правил.
    /// </summary>
    public string RuleName { get; }

    /// <summary>
    /// ¬озвращает набор правил, которые предоставл€ет данный провайдер.
    /// </summary>
    public IEnumerable<StatRuleBase> GetRules();
}
