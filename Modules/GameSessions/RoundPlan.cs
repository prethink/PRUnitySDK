using System;
using System.Collections.Generic;

namespace PRGameSessions
{
    /// <summary>
    /// Фабрики создают независимое состояние режима и правил для следующего раунда.
    /// </summary>
    public sealed class RoundPlan
    {
        public string ModeKey { get; }
        internal IReadOnlyList<Func<RoundBehaviour>> Factories { get; }

        public RoundPlan(string modeKey, Func<RoundBehaviour> mode,
            params Func<RoundBehaviour>[] rules)
        {
            if (string.IsNullOrWhiteSpace(modeKey)) throw new ArgumentException("Mode key is required.", nameof(modeKey));
            if (mode == null) throw new ArgumentNullException(nameof(mode));
            ModeKey = modeKey;
            var factories = new List<Func<RoundBehaviour>> { mode };
            if (rules != null)
                foreach (var rule in rules)
                    factories.Add(rule ?? throw new ArgumentException("Rule factory is null.", nameof(rules)));
            Factories = factories.AsReadOnly();
        }
    }
}
