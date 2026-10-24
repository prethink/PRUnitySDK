using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Правка раздела настроек проекта из своего окна: поля, сброс к значению по умолчанию, запись.
/// </summary>
/// <remarks>
/// Окна с превью (эффекты урона, подписи над сущностями, переход окон) правят настройки
/// не напрямую, а через <see cref="SerializedObject"/>: так работают отмена, кривые, цвета
/// и чужие PropertyDrawer-ы, а закрытые сеттеры раздела не мешают.
/// <para>
/// Значение по умолчанию берётся из временной копии <see cref="PRSDKSettings"/>, собранной
/// конструктором, — то есть из инициализаторов полей. Поле сравнивается с ней целиком
/// (<see cref="SerializedProperty.DataEquals"/>), поэтому кнопка сброса работает для любого
/// поля, от числа до кривой, без списка значений в окне.
/// </para>
/// <para>
/// Пути пишутся именами свойств через точку: <c>"Damage.Effects.Scale"</c>. Для автосвойства
/// имя переводится в поле <c>&lt;Name&gt;k__BackingField</c>, обычное поле ищется как есть.
/// </para>
/// </remarks>
public sealed class PRSDKSettingsSection : IDisposable
{
    private const string BackingFieldFormat = "<{0}>k__BackingField";
    private const float ResetButtonWidth = 18f;

    private readonly string rootPath;
    private readonly PRSDKSettings fixedSettings;

    private PRSDKSettings settings;
    private SerializedObject serialized;
    private PRSDKSettings defaultsAsset;
    private SerializedObject defaults;
    private GUIContent resetContent;

    /// <param name="path">Путь раздела от корня настроек: <c>"Damage.Effects"</c>.</param>
    /// <param name="settings">
    /// Свои настройки вместо настроек активного проекта — для тестов, которым нельзя трогать
    /// настоящий ассет.
    /// </param>
    public PRSDKSettingsSection(string path, PRSDKSettings settings = null)
    {
        rootPath = ToSerializedPath(path);
        fixedSettings = settings;
    }

    /// <summary>
    /// Настройки активного проекта; пусто, пока <see cref="Begin"/> их не нашёл.
    /// </summary>
    public PRSDKSettings Settings => settings;

    /// <summary>
    /// Свойство раздела в настройках.
    /// </summary>
    public SerializedProperty Root => serialized?.FindProperty(rootPath);

    /// <summary>
    /// Начинает кадр окна: находит настройки активного проекта и читает их свежими.
    /// </summary>
    /// <remarks>
    /// Проект могли переключить, пока окно открыто, — тогда раздел берётся из новых настроек.
    /// </remarks>
    /// <returns><see langword="false"/>, если настроек или раздела нет.</returns>
    public bool Begin()
    {
        PRSDKSettings current = fixedSettings != null ? fixedSettings : PRSDKSettings.Instance;

        if (current == null)
            return false;

        if (current != settings || serialized == null || serialized.targetObject == null)
        {
            settings = current;
            serialized = new SerializedObject(current);
        }

        serialized.UpdateIfRequiredOrScript();
        resetContent ??= new GUIContent(EditorGUIUtility.IconContent("Refresh").image, "Вернуть значение по умолчанию");

        return Root != null;
    }

    /// <summary>
    /// Заканчивает кадр окна: записывает правки в настройки с отменой через Ctrl+Z.
    /// </summary>
    /// <returns><see langword="true"/>, если в этом кадре что-то поменялось.</returns>
    public bool End()
    {
        return serialized != null && serialized.ApplyModifiedProperties();
    }

    /// <summary>
    /// Свойство внутри раздела: <c>"Scale.ScaleMultiplier"</c>.
    /// </summary>
    public SerializedProperty Find(string relativePath)
    {
        if (serialized == null)
            return null;

        return string.IsNullOrEmpty(relativePath)
            ? Root
            : serialized.FindProperty(rootPath + "." + ToSerializedPath(relativePath));
    }

    /// <summary>
    /// Поле раздела с кнопкой сброса справа.
    /// </summary>
    /// <param name="relativePath">Путь от раздела.</param>
    /// <param name="label">Своя подпись; пусто — из имени и подсказки поля.</param>
    /// <returns><see langword="true"/>, если значение поменяли.</returns>
    public bool Field(string relativePath, GUIContent label = null)
    {
        SerializedProperty property = Find(relativePath);

        if (property == null)
        {
            EditorGUILayout.HelpBox($"Нет поля {relativePath}.", MessageType.Warning);
            return false;
        }

        return Field(property, label);
    }

    /// <inheritdoc cref="Field(string, GUIContent)"/>
    /// <remarks>
    /// Место под поле берётся по его полной высоте, а кнопка сброса ставится у нижней строки:
    /// над полем бывает заголовок из атрибута <c>[Header]</c>, а у вектора значения уходят
    /// на вторую строку.
    /// </remarks>
    public bool Field(SerializedProperty property, GUIContent label = null)
    {
        label ??= new GUIContent(property.displayName, property.tooltip);

        float height = EditorGUI.GetPropertyHeight(property, label, includeChildren: true);
        Rect rect = EditorGUILayout.GetControlRect(true, height);

        var field = new Rect(rect.x, rect.y, rect.width - ResetButtonWidth - 2f, rect.height);
        var button = new Rect(rect.xMax - ResetButtonWidth, rect.yMax - EditorGUIUtility.singleLineHeight,
            ResetButtonWidth, EditorGUIUtility.singleLineHeight);

        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(field, property, label, includeChildren: true);
        bool changed = EditorGUI.EndChangeCheck();

        return ResetButton(button, property) || changed;
    }

    /// <summary>
    /// Кнопка сброса поля; у значения по умолчанию неактивна, но место держит.
    /// </summary>
    /// <returns><see langword="true"/>, если поле сбросили.</returns>
    public bool ResetButton(SerializedProperty property)
    {
        Rect rect = GUILayoutUtility.GetRect(ResetButtonWidth, EditorGUIUtility.singleLineHeight,
            GUILayout.Width(ResetButtonWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));

        return ResetButton(rect, property);
    }

    private bool ResetButton(Rect rect, SerializedProperty property)
    {
        using (new EditorGUI.DisabledScope(IsDefault(property)))
        {
            if (!GUI.Button(rect, resetContent, EditorStyles.iconButton))
                return false;
        }

        ResetToDefault(property);
        GUI.FocusControl(null);
        return true;
    }

    /// <summary>
    /// Стоит ли в поле значение по умолчанию.
    /// </summary>
    public bool IsDefault(SerializedProperty property)
    {
        SerializedProperty standard = FindDefault(property);

        return standard == null || SerializedProperty.DataEquals(property, standard);
    }

    /// <summary>
    /// Возвращает полю значение по умолчанию.
    /// </summary>
    public void ResetToDefault(SerializedProperty property)
    {
        SerializedProperty standard = FindDefault(property);

        if (standard != null)
            serialized.CopyFromSerializedProperty(standard);
    }

    /// <summary>
    /// Возвращает весь раздел к значениям по умолчанию.
    /// </summary>
    public void ResetAll()
    {
        SerializedProperty root = Root;

        if (root != null)
            ResetToDefault(root);
    }

    /// <summary>
    /// Записывает настройки на диск.
    /// </summary>
    public void Save()
    {
        if (settings == null)
            return;

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssetIfDirty(settings);
    }

    /// <summary>
    /// Полоса над окном: чей проект правится, «Всё по умолчанию» и «Сохранить».
    /// </summary>
    /// <returns><see langword="true"/>, если раздел сбросили целиком.</returns>
    public bool DrawToolbar()
    {
        bool reset = false;

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            PRSDKProject project = PRSDKActiveProject.Instance != null ? PRSDKActiveProject.Instance.Project : null;
            string owner = project != null ? project.Title : settings != null ? settings.name : "—";

            GUILayout.Label("Проект: " + owner, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("Всё по умолчанию", "Вернуть весь раздел к стандартным значениям. Отменяется через Ctrl+Z."),
                    EditorStyles.toolbarButton, GUILayout.Width(120f)))
            {
                ResetAll();
                GUI.FocusControl(null);
                reset = true;
            }

            if (GUILayout.Button(new GUIContent("Сохранить", "Записать настройки проекта на диск."),
                    EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                End();
                Save();
            }
        }

        return reset;
    }

    /// <summary>
    /// Значение раздела как объект — для превью, которому нужны типизированные настройки.
    /// </summary>
    /// <remarks>
    /// Читается после <see cref="End"/>: правки этого кадра уже записаны в настройки.
    /// </remarks>
    public T GetValue<T>() where T : class
    {
        return Root?.boxedValue as T;
    }

    /// <summary>
    /// Значение раздела в настройках по умолчанию.
    /// </summary>
    public T GetDefaultValue<T>() where T : class
    {
        EnsureDefaults();
        return defaults.FindProperty(rootPath)?.boxedValue as T;
    }

    public void Dispose()
    {
        if (defaultsAsset != null)
            UnityEngine.Object.DestroyImmediate(defaultsAsset);

        defaultsAsset = null;
        defaults = null;
        serialized = null;
    }

    private SerializedProperty FindDefault(SerializedProperty property)
    {
        if (property == null)
            return null;

        EnsureDefaults();
        return defaults.FindProperty(property.propertyPath);
    }

    private void EnsureDefaults()
    {
        if (defaults != null && defaultsAsset != null)
            return;

        defaultsAsset = ScriptableObject.CreateInstance<PRSDKSettings>();
        defaultsAsset.hideFlags = HideFlags.HideAndDontSave;
        defaults = new SerializedObject(defaultsAsset);
    }

    private static string ToSerializedPath(string path)
    {
        string[] parts = path.Split('.');

        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];

            // Элементы списков и уже готовые имена полей не трогаем.
            if (part.StartsWith("<", StringComparison.Ordinal) || part == "Array" || part.StartsWith("data[", StringComparison.Ordinal))
                continue;

            if (part.Length > 0 && char.IsUpper(part[0]))
                parts[i] = string.Format(BackingFieldFormat, part);
        }

        return string.Join(".", parts);
    }
}
