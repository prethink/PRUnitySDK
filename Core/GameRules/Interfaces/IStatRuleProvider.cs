using System.Collections.Generic;

public interface IStatRuleProvider 
{
    public string RuleName { get; }
    public IEnumerable<StatRuleBase> GetRules();
}
