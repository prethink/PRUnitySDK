using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Общие элементы интерфейса инспекторов assets PRUnitySDK.
/// </summary>
internal static class PRSDKInspectorUtility
{
    private const BindingFlags FieldFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// Возвращает сериализованные секции верхнего уровня без поля MonoScript.
    /// </summary>
    public static IReadOnlyList<SerializedProperty> GetRootProperties(SerializedObject serializedObject)
    {
        var properties = new List<SerializedProperty>();
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (iterator.propertyPath == "m_Script")
                continue;

            properties.Add(iterator.Copy());
        }

        return properties;
    }

    /// <summary>
    /// Возвращает видимые сериализованные свойства первого уровня внутри указанного свойства.
    /// </summary>
    public static IReadOnlyList<SerializedProperty> GetDirectChildren(SerializedProperty parent)
    {
        var properties = new List<SerializedProperty>();
        if (parent == null || !parent.hasVisibleChildren)
            return properties;

        SerializedProperty iterator = parent.Copy();
        SerializedProperty end = iterator.GetEndProperty();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren) &&
               !SerializedProperty.EqualContents(iterator, end))
        {
            enterChildren = false;
            if (iterator.depth == parent.depth + 1)
                properties.Add(iterator.Copy());
        }

        return properties;
    }

    /// <summary>
    /// Преобразует имя backing field в читаемое название секции.
    /// </summary>
    public static string GetSectionName(SerializedProperty property)
    {
        string name = property.name;
        const string backingFieldSuffix = ">k__BackingField";

        if (name.StartsWith("<", StringComparison.Ordinal) &&
            name.EndsWith(backingFieldSuffix, StringComparison.Ordinal))
        {
            name = name.Substring(1, name.Length - backingFieldSuffix.Length - 1);
        }

        return ObjectNames.NicifyVariableName(name);
    }

    /// <summary>
    /// Возвращает тип поля, соответствующего сериализованной секции.
    /// </summary>
    public static Type GetFieldType(Type targetType, SerializedProperty property)
    {
        return FindField(targetType, property)?.FieldType;
    }

    /// <summary>
    /// Возвращает отражённое поле, соответствующее сериализованному свойству.
    /// </summary>
    public static FieldInfo GetFieldInfo(Type targetType, SerializedProperty property)
    {
        return FindField(targetType, property);
    }

    /// <summary>
    /// Возвращает текущее значение поля, соответствующего сериализованной секции.
    /// </summary>
    public static object GetFieldValue(object target, SerializedProperty property)
    {
        if (target == null)
            return null;

        return FindField(target.GetType(), property)?.GetValue(target);
    }

    /// <summary>
    /// Возвращает тип definition для наследника <see cref="Database{T}"/>.
    /// </summary>
    public static Type GetDatabaseElementType(Type databaseType)
    {
        for (Type current = databaseType; current != null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Database<>))
                return current.GetGenericArguments()[0];
        }

        return null;
    }

    /// <summary>
    /// Проверяет, соответствует ли секция текущему поисковому запросу.
    /// </summary>
    public static bool MatchesSearch(string sectionName, string search)
    {
        return string.IsNullOrWhiteSpace(search) ||
               sectionName.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Рисует описание раздела настроек, если оно задано атрибутом.
    /// </summary>
    /// <remarks>
    /// Описание живёт рядом с самим разделом, а не в окне: раздел заводят вместе
    /// с модулем, и список описаний в окне разошёлся бы с ними на первой же правке.
    /// </remarks>
    /// <param name="sectionType">Тип раздела настроек.</param>
    /// <returns><c>true</c>, если описание нашлось и было нарисовано.</returns>
    public static bool DrawSectionDescription(Type sectionType)
    {
        if (sectionType == null)
            return false;

        var attribute = (SettingsDescriptionAttribute)Attribute.GetCustomAttribute(
            sectionType, typeof(SettingsDescriptionAttribute));

        if (attribute == null)
            return false;

        if (!string.IsNullOrWhiteSpace(attribute.Description))
            EditorGUILayout.HelpBox(attribute.Description, MessageType.None);

        if (!string.IsNullOrWhiteSpace(attribute.Warning))
            EditorGUILayout.HelpBox(attribute.Warning, MessageType.Warning);

        return true;
    }

    /// <summary>
    /// Рисует кнопку сброса раздела к значениям по умолчанию.
    /// </summary>
    /// <remarks>
    /// Сброс спрашивает подтверждение: отменить его можно только через Undo, а раздел
    /// вроде экономики или сохранения настраивают неделями.
    /// <para>
    /// Раздел, у которого есть собственное «по умолчанию»
    /// (<see cref="IDefaultSettings"/>), сбрасывается своим методом; остальные
    /// собираются заново — тогда поля возвращаются к значениям, записанным
    /// в объявлении, а ссылки на ассеты очищаются. Об этом и предупреждает диалог.
    /// </para>
    /// </remarks>
    /// <param name="asset">Ассет, в который попадёт правка: он пишется в Undo и помечается грязным.</param>
    /// <param name="owner">
    /// Объект, которому принадлежит поле раздела. У раздела верхнего уровня это сам ассет,
    /// у вложенного — раздел, внутри которого он лежит.
    /// </param>
    /// <param name="sectionName">Название раздела для диалога.</param>
    /// <param name="section">Текущее значение раздела.</param>
    /// <param name="field">Поле раздела в его владельце.</param>
    /// <returns><c>true</c>, если раздел был сброшен.</returns>
    public static bool DrawResetSectionButton(UnityEngine.Object asset, object owner, string sectionName,
        object section, FieldInfo field)
    {
        if (asset == null || owner == null || field == null)
            return false;

        Type sectionType = section?.GetType() ?? field.FieldType;

        if (!CanReset(sectionType))
            return false;

        if (!GUILayout.Button(ResetButtonContent, EditorStyles.miniButton, GUILayout.Width(88f)))
            return false;

        if (!ConfirmReset(sectionName, sectionType))
            return false;

        Undo.RecordObject(asset, $"Сброс настроек: {sectionName}");

        if (section is IDefaultSettings defaultable)
        {
            defaultable.SetDefaultSettings();
        }
        else
        {
            field.SetValue(owner, Activator.CreateInstance(sectionType));
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();

        return true;
    }

    /// <summary>
    /// Имена полей, которыми раздел настроек включают и выключают целиком.
    /// </summary>
    /// <remarks>
    /// Список закрытый: галка с именем вроде <c>ShowNames</c> управляет одной подписью,
    /// а не разделом, и подсветка по ней врала бы о состоянии всего блока.
    /// </remarks>
    private static readonly string[] ToggleFieldNames = { "Enabled", "IsEnabled", "Active", "IsActive" };

    /// <summary>
    /// Ищет у раздела флаг, которым он включается целиком.
    /// </summary>
    /// <param name="section">Свойство раздела.</param>
    /// <param name="isEnabled">Значение флага, если он нашёлся.</param>
    /// <returns><c>true</c>, если у раздела есть такой флаг.</returns>
    public static bool TryGetSectionToggle(SerializedProperty section, out bool isEnabled)
    {
        isEnabled = false;

        if (section == null || !section.hasVisibleChildren)
            return false;

        foreach (SerializedProperty child in GetDirectChildren(section))
        {
            if (child.propertyType != SerializedPropertyType.Boolean)
                continue;

            string name = GetSectionName(child).Replace(" ", string.Empty);

            foreach (string toggleName in ToggleFieldNames)
            {
                if (!string.Equals(name, toggleName, StringComparison.OrdinalIgnoreCase))
                    continue;

                isEnabled = child.boolValue;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Стиль заголовка раздела: включённый подсвечивается зелёным.
    /// </summary>
    /// <remarks>
    /// Цвет отвечает на вопрос, который иначе требует раскрыть раздел и найти в нём галку:
    /// работает ли это вообще. У свёрнутого списка разделов это единственный способ увидеть,
    /// что включено.
    /// <para>
    /// Подсвечивается только включённое. Красить выключенное вторым цветом значило бы
    /// сказать «здесь ошибка», хотя выключенный раздел — обычное состояние.
    /// </para>
    /// </remarks>
    /// <param name="highlight">Подсветить ли заголовок.</param>
    public static GUIStyle GetSectionFoldoutStyle(bool highlight)
    {
        if (!highlight)
            return EditorStyles.foldout;

        if (enabledFoldoutStyle == null)
        {
            // Тёмная и светлая темы требуют разной яркости: один и тот же зелёный
            // на светлом фоне сливается, а на тёмном выжигает глаза.
            Color color = EditorGUIUtility.isProSkin
                ? new Color(0.45f, 0.85f, 0.48f)
                : new Color(0.13f, 0.52f, 0.18f);

            enabledFoldoutStyle = new GUIStyle(EditorStyles.foldout);
            enabledFoldoutStyle.normal.textColor = color;
            enabledFoldoutStyle.onNormal.textColor = color;
            enabledFoldoutStyle.focused.textColor = color;
            enabledFoldoutStyle.onFocused.textColor = color;
            enabledFoldoutStyle.active.textColor = color;
            enabledFoldoutStyle.onActive.textColor = color;
            enabledFoldoutStyle.hover.textColor = color;
            enabledFoldoutStyle.onHover.textColor = color;
        }

        return enabledFoldoutStyle;
    }

    /// <summary>
    /// Стиль заголовка включённого раздела. Создаётся один раз: стили нельзя строить
    /// каждый кадр, иначе редактор мусорит на каждой отрисовке окна.
    /// </summary>
    private static GUIStyle enabledFoldoutStyle;

    /// <summary>
    /// Есть ли у типа описание раздела.
    /// </summary>
    /// <remarks>
    /// По нему окно решает, рисовать ли вложенное поле обычным <c>PropertyField</c>
    /// или разворачивать его как отдельный раздел — со своей шапкой, описанием
    /// и кнопкой сброса.
    /// </remarks>
    public static bool HasSectionDescription(Type sectionType)
    {
        return sectionType != null &&
               Attribute.IsDefined(sectionType, typeof(SettingsDescriptionAttribute));
    }

    /// <summary>
    /// Можно ли собрать такой раздел заново.
    /// </summary>
    private static bool CanReset(Type sectionType)
    {
        if (sectionType == null || sectionType.IsAbstract)
            return false;

        if (typeof(IDefaultSettings).IsAssignableFrom(sectionType))
            return true;

        return sectionType.GetConstructor(Type.EmptyTypes) != null;
    }

    private static bool ConfirmReset(string sectionName, Type sectionType)
    {
        bool hasOwnDefaults = typeof(IDefaultSettings).IsAssignableFrom(sectionType);

        string message = hasOwnDefaults
            ? $"Раздел «{sectionName}» вернётся к значениям по умолчанию.\n\n" +
              "Текущие значения будут потеряны — отменить можно только через Undo (Ctrl+Z)."
            : $"Раздел «{sectionName}» будет собран заново.\n\n" +
              "Поля вернутся к значениям из объявления, а ссылки на ассеты внутри раздела " +
              "очистятся. Отменить можно только через Undo (Ctrl+Z).";

        return EditorUtility.DisplayDialog("Сбросить раздел настроек?", message, "Сбросить", "Отмена");
    }

    private static readonly GUIContent ResetButtonContent = new(
        "Сбросить",
        "Вернуть разделу значения по умолчанию. Спросит подтверждение.");

    /// <summary>
    /// Рисует общую шапку инспектора singleton-asset.
    /// </summary>
    public static void DrawHeader(string title, UnityEngine.Object asset)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField(title, EditorStyles.largeLabel);
        string path = AssetDatabase.GetAssetPath(asset);
        if (!string.IsNullOrWhiteSpace(path))
            EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
        EditorGUILayout.Space(4f);
    }

    private static FieldInfo FindField(Type targetType, SerializedProperty property)
    {
        for (Type current = targetType; current != null; current = current.BaseType)
        {
            FieldInfo field = current.GetField(property.name, FieldFlags);
            if (field != null)
                return field;
        }

        return null;
    }
}
