using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Проверяет поля <see cref="EnumerationReference{T}"/> в ассетах, префабах и на сценах.
/// </summary>
/// <remarks>
/// Значение хранится строкой, поэтому после удаления или переименования ассеты остаются
/// со старой строкой, и компилятор об этом не сообщает.
/// </remarks>
public sealed class EnumerationValidator : IProjectValidator
{
    private const BindingFlags FieldFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private const string ProgressTitle = "Проверка Enumeration";

    /// <summary>
    /// Набор поля по типу владельца и пути свойства.
    /// </summary>
    private readonly Dictionary<(Type Owner, string Path), Type> providerByField = new();

    private readonly Dictionary<Type, HashSet<string>> optionsByProvider = new();

    private readonly List<ProjectValidationIssue> issues = new();

    /// <inheritdoc />
    public string Title => "Enumeration";

    /// <inheritdoc />
    public IEnumerable<ProjectValidationIssue> Validate()
    {
        issues.Clear();

        try
        {
            ValidateAssets();
            ValidateScenes();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        return issues;
    }

    private void ValidateAssets()
    {
        string[] paths = FindPaths("t:Prefab t:ScriptableObject");

        for (int index = 0; index < paths.Length; index++)
        {
            if (EditorUtility.DisplayCancelableProgressBar(ProgressTitle, paths[index], (float)index / paths.Length))
                return;

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(paths[index]);

            if (asset is GameObject prefab)
                ValidateHierarchy(prefab, paths[index]);
            else if (asset is ScriptableObject)
                ValidateTarget(asset, paths[index]);
        }
    }

    /// <summary>
    /// Сцены: открытые смотрим как есть, остальные открываем по очереди и закрываем.
    /// </summary>
    private void ValidateScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var openPaths = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene scene = SceneManager.GetSceneAt(index);
            openPaths.Add(scene.path);
            ValidateScene(scene);
        }

        string[] paths = FindPaths("t:Scene").Where(path => !openPaths.Contains(path)).ToArray();

        if (paths.Length == 0)
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            issues.Add(new ProjectValidationIssue(MessageType.Info,
                "Закрытые сцены не проверены: есть несохранённые изменения."));
            return;
        }

        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            for (int index = 0; index < paths.Length; index++)
            {
                if (EditorUtility.DisplayCancelableProgressBar(ProgressTitle, paths[index], (float)index / paths.Length))
                    return;

                Scene scene = EditorSceneManager.OpenScene(paths[index], OpenSceneMode.Additive);
                ValidateScene(scene);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        finally
        {
            if (setup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    private void ValidateScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
            ValidateHierarchy(root, scene.path);
    }

    private void ValidateHierarchy(GameObject root, string source)
    {
        foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component != null)
                ValidateTarget(component, $"{source} · {GetPath(component.transform)} ({component.GetType().Name})");
        }
    }

    private void ValidateTarget(UnityEngine.Object target, string source)
    {
        using var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.GetIterator();

        while (property.NextVisible(true))
        {
            if (property.propertyType != SerializedPropertyType.Generic)
                continue;

            if (!property.type.Contains(nameof(EnumerationReference)))
                continue;

            SerializedProperty valueProperty = property.FindPropertyRelative(EnumerationReference.ProtectedStringValueName);
            if (valueProperty == null)
                continue;

            Type provider = GetProviderType(target.GetType(), property.propertyPath);
            if (provider == null)
                continue;

            ProjectValidationIssue issue = Validate(target, source, valueProperty, provider);

            if (issue != null)
                issues.Add(issue);
        }
    }

    private ProjectValidationIssue Validate(
        UnityEngine.Object target,
        string source,
        SerializedProperty valueProperty,
        Type provider)
    {
        string location = $"{source} · {valueProperty.propertyPath}";
        string value = valueProperty.stringValue;

        if (string.IsNullOrEmpty(value))
            return CreateEmptyIssue(target, location, valueProperty.propertyPath, provider);

        if (GetOptions(provider).Contains(value))
            return null;

        return new ProjectValidationIssue(MessageType.Error,
            $"{location}: значение '{value}' отсутствует в {provider.Name}.", target);
    }

    /// <summary>
    /// Незаполненное поле: код получит значение по умолчанию, а без него — пустую строку.
    /// </summary>
    private static ProjectValidationIssue CreateEmptyIssue(
        UnityEngine.Object target,
        string location,
        string propertyPath,
        Type provider)
    {
        Enumeration defaultValue = provider.GetEnumerationDefault();

        if (defaultValue == null)
        {
            return new ProjectValidationIssue(MessageType.Error,
                $"{location}: значение не выбрано, а у {provider.Name} нет значения по умолчанию.", target);
        }

        return new ProjectValidationIssue(MessageType.Info,
            $"{location}: значение не выбрано, используется '{defaultValue}'.",
            target,
            $"Записать '{defaultValue}'",
            () => Write(target, propertyPath, defaultValue.Value));
    }

    /// <summary>
    /// Записывает значение в поле. Объект сцены помечается изменённым, сцену сохраняет
    /// пользователь.
    /// </summary>
    private static void Write(UnityEngine.Object target, string propertyPath, string value)
    {
        using var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyPath);

        if (property == null)
            return;

        property.stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);

        if (target is Component component && component.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
    }

    private static string[] FindPaths(string filter)
    {
        return AssetDatabase.FindAssets(filter)
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string GetPath(Transform transform)
    {
        string path = transform.name;

        for (Transform parent = transform.parent; parent != null; parent = parent.parent)
            path = $"{parent.name}/{path}";

        return path;
    }

    private HashSet<string> GetOptions(Type provider)
    {
        if (optionsByProvider.TryGetValue(provider, out HashSet<string> cached))
            return cached;

        cached = new HashSet<string>(
            provider.GetEnumerationsSmart(true).Where(option => option != null).Select(option => option.Value),
            StringComparer.Ordinal);

        optionsByProvider.Add(provider, cached);
        return cached;
    }

    /// <summary>
    /// Набор, к которому привязано поле; <c>null</c>, если это не <c>EnumerationReference&lt;T&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Тип набора знает только поле, в сериализованных данных его нет, поэтому путь свойства
    /// проходится рефлексией. Результат кешируется: один тип компонента встречается
    /// в сотнях префабов.
    /// </remarks>
    private Type GetProviderType(Type ownerType, string propertyPath)
    {
        var key = (ownerType, propertyPath);

        if (providerByField.TryGetValue(key, out Type cached))
            return cached;

        cached = ResolveProviderType(ownerType, propertyPath);
        providerByField.Add(key, cached);

        return cached;
    }

    private static Type ResolveProviderType(Type ownerType, string propertyPath)
    {
        Type current = ownerType;

        // Путь элемента списка выглядит как "items.Array.data[0]".
        foreach (string segment in propertyPath.Split('.'))
        {
            if (current == null)
                return null;

            if (segment == "Array")
                continue;

            current = segment.StartsWith("data[", StringComparison.Ordinal)
                ? GetElementType(current)
                : GetField(current, segment)?.FieldType;
        }

        for (Type type = current; type != null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EnumerationReference<>))
                return type.GetGenericArguments()[0];
        }

        return null;
    }

    private static FieldInfo GetField(Type type, string name)
    {
        for (Type current = type; current != null; current = current.BaseType)
        {
            FieldInfo field = current.GetField(name, FieldFlags);

            if (field != null)
                return field;
        }

        return null;
    }

    private static Type GetElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        return type.IsGenericType ? type.GetGenericArguments()[0] : null;
    }
}
