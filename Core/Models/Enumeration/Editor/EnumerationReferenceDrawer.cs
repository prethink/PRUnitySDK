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

    /// <summary>
    /// Тот же список, но с пометкой у значения по умолчанию.
    /// </summary>
    /// <remarks>
    /// Отдельный массив, собранный один раз на тип: подписывать пункт на лету значило бы
    /// копировать список на каждую перерисовку инспектора.
    /// </remarks>
    private static readonly Dictionary<Type, string[]> defaultMarkedCache = new();

    private const string DefaultSuffix = " (по умолчанию)";

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
        var isDefault = false;

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

            // И сразу говорим, что это подставилось само: иначе не отличить
            // от осознанного выбора того же значения.
            displayOptions = GetDefaultMarkedOptions(options, selectedIndex);
            isDefault = true;
        }

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = valueProperty.hasMultipleDifferentValues;

        Color previousColor = GUI.color;

        if (isDefault)
            GUI.color = DefaultTint;

        var newIndex = EditorGUI.Popup(position, label.text, selectedIndex, displayOptions);

        GUI.color = previousColor;
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
    /// Приглушённый тон для значения, которое подставилось само.
    /// </summary>
    /// <remarks>
    /// Не красный и не жёлтый: это не ошибка и не предупреждение, а сообщение о том,
    /// что поле не трогали. Кричать о нём в каждом инспекторе не нужно.
    /// </remarks>
    private static readonly Color DefaultTint = new Color(0.75f, 0.85f, 1f);

    /// <summary>
    /// Список с подписью у значения по умолчанию.
    /// </summary>
    private string[] GetDefaultMarkedOptions(string[] options, int defaultIndex)
    {
        Type providerType = GetProviderType();

        if (providerType != null && defaultMarkedCache.TryGetValue(providerType, out string[] cached)
            && cached.Length == options.Length)
        {
            return cached;
        }

        string[] marked = new string[options.Length];
        Array.Copy(options, marked, options.Length);

        if (defaultIndex >= 0 && defaultIndex < marked.Length)
            marked[defaultIndex] += DefaultSuffix;

        if (providerType != null)
            defaultMarkedCache[providerType] = marked;

        return marked;
    }

    /// <summary>
    /// Значение по умолчанию у набора этой ссылки.
    /// </summary>
    private string GetDefaultValue()
    {
        return GetProviderType()?.GetEnumerationDefault()?.Value;
    }

    /// <summary>
    /// Набор, к которому привязано это поле.
    /// </summary>
    private Type GetProviderType()
    {
        Type referenceType = fieldInfo.FieldType;

        return referenceType.IsGenericType ? referenceType.GetGenericArguments()[0] : null;
    }

    private string[] GetOptions()
    {
        var providerType = GetProviderType();
        if (providerType == null)
            return Array.Empty<string>();

        if (optionsCache.TryGetValue(providerType, out var cached))
            return cached;

        var provider = providerType.GetEnumerationProvider();
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
