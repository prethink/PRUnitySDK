using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Отображает EnumerationReference как popup и сохраняет неизвестные старые значения.
/// </summary>
[CustomPropertyDrawer(typeof(EnumerationReference<>), true)]
public class EnumerationReferenceDrawer : PropertyDrawer
{
    private static readonly Dictionary<Type, string[]> optionsCache = new();
    private static readonly Dictionary<Type, string> defaultCache = new();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var valueProperty = property.FindPropertyRelative(EnumerationReference.ProtectedStringValueName);
        if (valueProperty == null)
        {
            EditorGUI.LabelField(position, label, new GUIContent("Enumeration value field not found"));
            return;
        }

        var options = GetOptions();
        if (options.Length == 0)
        {
            EditorGUI.PropertyField(position, valueProperty, label);
            return;
        }

        var currentValue = valueProperty.stringValue;
        var currentIndex = Array.IndexOf(options, currentValue);
        var hasMissingValue = !string.IsNullOrEmpty(currentValue) && currentIndex < 0;
        var displayOptions = options;
        var selectedIndex = currentIndex;

        if (hasMissingValue)
        {
            displayOptions = new[] { $"Missing: {currentValue}" }.Concat(options).ToArray();
            selectedIndex = 0;
        }
        else if (selectedIndex < 0)
        {
            // Значение не выбрано. Показываем то, что вернёт код, — значение
            // по умолчанию набора. Иначе список показывал бы первый пункт, а из кода
            // приходило бы другое, и расхождение было бы не видно.
            int defaultIndex = Array.IndexOf(options, GetDefaultValue());
            selectedIndex = defaultIndex >= 0 ? defaultIndex : 0;
        }

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = valueProperty.hasMultipleDifferentValues;
        var newIndex = EditorGUI.Popup(position, label.text, selectedIndex, displayOptions);
        EditorGUI.showMixedValue = false;

        if (EditorGUI.EndChangeCheck())
        {
            var optionIndex = hasMissingValue ? newIndex - 1 : newIndex;
            if (optionIndex >= 0 && optionIndex < options.Length)
                valueProperty.stringValue = options[optionIndex];
        }

        EditorGUI.EndProperty();
    }

    /// <summary>
    /// Значение по умолчанию у набора этой ссылки.
    /// </summary>
    private string GetDefaultValue()
    {
        Type referenceType = fieldInfo.FieldType;

        if (!referenceType.IsGenericType)
            return null;

        Type providerType = referenceType.GetGenericArguments()[0];

        if (defaultCache.TryGetValue(providerType, out string cached))
            return cached;

        string value = providerType.GetEnumerationProvider() is EnumerationProviderBase provider
            ? provider.Default?.Value
            : null;
        defaultCache[providerType] = value;

        return value;
    }

    private string[] GetOptions()
    {
        var referenceType = fieldInfo.FieldType;
        if (!referenceType.IsGenericType)
            return Array.Empty<string>();

        var providerType = referenceType.GetGenericArguments()[0];
        if (optionsCache.TryGetValue(providerType, out var cached))
            return cached;

        var provider = Activator.CreateInstance(providerType) as IEnumerationProvider;
        var options = provider?.GetOptions()
            ?.Where(option => option != null)
            .Select(option => option.Value)
            .Where(value => !string.IsNullOrEmpty(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray()
            ?? Array.Empty<string>();

        optionsCache.Add(providerType, options);
        return options;
    }
}
