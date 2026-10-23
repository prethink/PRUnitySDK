using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Выпадающий список характеристик для строкового поля с <see cref="StatKeyAttribute"/>.
/// </summary>
[CustomPropertyDrawer(typeof(StatKeyAttribute))]
public class StatKeyDrawer : PropertyDrawer
{
    private const string NoneLabel = "— не выбрано —";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label, new GUIContent("StatKey только для строк"));
            return;
        }

        Draw(position, property, label);
    }

    /// <summary>
    /// Рисует выбор характеристики; используется и окном правил.
    /// </summary>
    public static void Draw(Rect position, SerializedProperty property, GUIContent label)
    {
        IReadOnlyList<StatOption> options = StatOptions.Get();
        string current = property.stringValue;

        if (options.Count == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        int index = -1;
        for (int i = 0; i < options.Count; i++)
        {
            if (string.Equals(options[i].Value, current, StringComparison.Ordinal))
            {
                index = i;
                break;
            }
        }

        bool missing = !string.IsNullOrEmpty(current) && index < 0;
        var labels = new List<GUIContent> { new(missing ? $"Missing: {current}" : NoneLabel) };
        labels.AddRange(options.Select(option => new GUIContent(option.Label)));

        EditorGUI.BeginProperty(position, label, property);
        int selected = EditorGUI.Popup(position, label, index + 1, labels.ToArray());
        EditorGUI.EndProperty();

        if (selected != index + 1 && selected > 0)
            property.stringValue = options[selected - 1].Value;
    }

    /// <summary>
    /// Характеристика в списке: значение и подпись (с набором, если наборов несколько).
    /// </summary>
    public readonly struct StatOption
    {
        public StatOption(string value, string label)
        {
            Value = value;
            Label = label;
        }

        public string Value { get; }

        public string Label { get; }
    }

    /// <summary>
    /// Характеристики всех ассетов статов проекта: наборы <c>T</c> из наследников <c>EntityStatsBase&lt;T&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Собирается один раз до перезагрузки домена: новые характеристики появляются вместе с перекомпиляцией.
    /// </remarks>
    public static class StatOptions
    {
        private static StatOption[] cache;

        public static IReadOnlyList<StatOption> Get()
        {
            return cache ??= Collect();
        }

        private static StatOption[] Collect()
        {
            var providers = new HashSet<Type>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<EntityStatsBase>())
            {
                for (Type current = type; current != null; current = current.BaseType)
                {
                    if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(EntityStatsBase<>))
                    {
                        Type provider = current.GetGenericArguments()[0];
                        if (!provider.ContainsGenericParameters)
                            providers.Add(provider);
                        break;
                    }
                }
            }

            bool grouped = providers.Count > 1;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<StatOption>();

            foreach (Type provider in providers.OrderBy(type => type.Name, StringComparer.Ordinal))
            {
                foreach (Enumeration stat in provider.GetEnumerationsSmart(true))
                {
                    if (stat == null || !seen.Add(stat.Value))
                        continue;

                    result.Add(new StatOption(stat.Value, grouped ? $"{provider.Name}/{stat.Value}" : stat.Value));
                }
            }

            return result.ToArray();
        }
    }
}
