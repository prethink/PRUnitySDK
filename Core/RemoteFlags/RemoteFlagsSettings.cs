using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Флаги проекта по умолчанию.
/// </summary>
[Serializable]
[SettingsDescription("Флаги проекта: значения по имени, которые площадка может поменять без пересборки " +
                     "(у Яндекса — флаги в консоли). Здесь — значения по умолчанию: они работают без площадки " +
                     "и для флагов, которых площадка не прислала.")]
public class RemoteFlagsSettings
{
    /// <summary>
    /// Флаг: имя и значение строкой.
    /// </summary>
    [Serializable]
    public class Flag
    {
        [Tooltip("Имя флага, как в консоли площадки.")]
        public string Name;

        [Tooltip("Значение строкой: число, true/false, текст.")]
        public string Value;
    }

    [SerializeField]
    [Tooltip("Значения флагов по умолчанию.")]
    private List<Flag> flags = new();

    /// <summary>
    /// Значение флага по умолчанию.
    /// </summary>
    /// <returns><c>false</c>, если такого флага в настройках нет.</returns>
    public bool TryGetValue(string name, out string value)
    {
        value = null;

        if (string.IsNullOrEmpty(name))
            return false;

        foreach (Flag flag in flags)
        {
            if (flag == null || flag.Name != name)
                continue;

            value = flag.Value;
            return true;
        }

        return false;
    }
}
