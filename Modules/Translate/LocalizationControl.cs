using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocalizationControl : ILocalizationProvider
{
    [field: SerializeField] public string LocalizationKey { get; private set; }

    /// <summary>
    /// Группа, к которой относится подпись: качество, магазин, окно наград.
    /// </summary>
    /// <remarks>
    /// Только для порядка в редакторе: в игре подпись ищется по ключу, и группа
    /// на поиск не влияет. Пустая означает «без группы» — список от этого не ломается,
    /// и старые записи продолжают работать как раньше.
    /// </remarks>
    [field: SerializeField] public string Group { get; private set; } = string.Empty;
    [field: SerializeField, SerializedDictionary("Lang", "Value")] public SerializedDictionary<LangType, string> localizationValues { get; private set; } = new();

    public IReadOnlyDictionary<LangType, string> LocalizationValues => localizationValues;

    public const string InternalLocalizationValuesPropertyName = nameof(localizationValues);

    /// <summary>
    /// Разделители, по которым имя ключа делится на группу и остальное.
    /// </summary>
    /// <remarks>
    /// Ключи давно пишут с префиксом — <c>quality_rare</c>, <c>shop.buy</c>, — так что
    /// группу почти всегда можно предложить, ничего не переименовывая.
    /// </remarks>
    public static readonly char[] GroupSeparators = { '.', '_', '/', ':' };

    /// <summary>
    /// Группа, выведенная из ключа.
    /// </summary>
    /// <returns>Пустая строка, если в ключе нет разделителя.</returns>
    public static string GetGroupFromKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        int index = key.IndexOfAny(GroupSeparators);
        return index > 0 ? key.Substring(0, index) : string.Empty;
    }

    public LocalizationControl()
    {
        
    }

    public LocalizationControl(string key, Dictionary<LangType, string> localization)
    {
        LocalizationKey = key;
        localizationValues = new SerializedDictionary<LangType, string>(localization);
    }
}
