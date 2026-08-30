using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Раздел «Описание» в инспекторе сущности.
/// </summary>
/// <remarks>
/// Описание сущности лежит не там, где сама сущность: у одних оно в определении, у других
/// в отдельном ассете, у третьих прямо на компоненте. Чтобы посмотреть или поправить его,
/// приходилось искать нужный ассет в проекте - раздел показывает его на месте.
/// <para>
/// Раздел один на все случаи, но у каждого источника своя область действия, и она
/// подписана: правка общего описания меняет его всем сущностям сразу, и без подписи
/// встроенный редактор читался бы как настройка одного объекта.
/// </para>
/// <para>
/// Оформлен отдельным классом, а не редактором: <c>[CustomEditor]</c> объявляет проект.
/// Два <c>[CustomEditor]</c> на один тип Unity разрешает молча и произвольно, поэтому
/// владелец инспектора должен быть один - а какой именно, зависит от проекта: там, где
/// уже есть инспектор на все объекты, раздел подмешивается в него.
/// </para>
/// <para>
/// Подключение: создать раздел, звать <see cref="Draw"/> из <c>OnInspectorGUI</c>
/// и освободить в <c>OnDisable</c>.
/// <code>
/// [CustomEditor(typeof(EntityBase), true)]
/// public class EntityInspector : Editor
/// {
///     private EntityDescriptionSection description;
///
///     public override void OnInspectorGUI()
///     {
///         base.OnInspectorGUI();
///         description ??= new EntityDescriptionSection();
///         description.Draw(serializedObject, target);
///     }
///
///     private void OnDisable() => description?.Dispose();
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class EntityDescriptionSection : IDisposable
{
    /// <summary>
    /// Поле <c>Definition</c> объявлено автосвойством, поэтому в сериализации у него
    /// имя backing-поля.
    /// </summary>
    private const string DefinitionPropertyPath = "<Definition>k__BackingField";

    private readonly Dictionary<UnityEngine.Object, Editor> embeddedEditors = new();

    private bool sourceExpanded = true;
    private bool overrideExpanded = true;

    /// <summary>
    /// Рисует раздел.
    /// </summary>
    /// <param name="serializedObject">Сериализованная сущность.</param>
    /// <param name="target">Сущность.</param>
    public void Draw(SerializedObject serializedObject, UnityEngine.Object target)
    {
        if (serializedObject == null || target == null)
            return;

        // Раздел разбирает один объект: у разных сущностей источники описания разные,
        // и показывать их одним блоком нечестно.
        if (serializedObject.isEditingMultipleObjects)
            return;

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Описание", EditorStyles.boldLabel);

        DrawSource(serializedObject, target);
        DrawInstanceOverride(target);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (Editor editor in embeddedEditors.Values)
        {
            if (editor != null)
                UnityEngine.Object.DestroyImmediate(editor);
        }

        embeddedEditors.Clear();
    }

    /// <summary>
    /// Рисует общее описание - то, что сущность разделяет с себе подобными.
    /// </summary>
    private void DrawSource(SerializedObject serializedObject, UnityEngine.Object target)
    {
        SerializedProperty definition = serializedObject.FindProperty(DefinitionPropertyPath);

        if (definition != null && definition.propertyType == SerializedPropertyType.ObjectReference)
        {
            DrawAssetSource(
                serializedObject,
                target,
                definition,
                "Описание берётся из определения.",
                "Общее для всех сущностей с этим определением.",
                allowCreate: false);
            return;
        }

        if (target is IEntityMetadata)
        {
            EditorGUILayout.HelpBox(
                "Сущность описывает себя сама - имя, иконка и переводы в полях выше.",
                MessageType.None);
            return;
        }

        SerializedProperty metadataAsset = FindMetadataAssetProperty(serializedObject, target);

        if (metadataAsset != null)
        {
            DrawAssetSource(
                serializedObject,
                target,
                metadataAsset,
                "Описание берётся из отдельного ассета.",
                "Общее для всех сущностей, ссылающихся на этот ассет.",
                allowCreate: true);
            return;
        }

        EditorGUILayout.HelpBox(
            "У сущности нет описания: ни определения, ни собственных полей, ни ассета. " +
            "В отладчике и в UI она останется безымянной.",
            MessageType.Warning);
    }

    /// <summary>
    /// Рисует ссылку на общее описание и его редактор.
    /// </summary>
    /// <param name="serializedObject">Сериализованная сущность.</param>
    /// <param name="target">Сущность.</param>
    /// <param name="property">Свойство со ссылкой.</param>
    /// <param name="sourceNote">Откуда берётся описание.</param>
    /// <param name="scopeNote">Кого затронет правка.</param>
    /// <param name="allowCreate">Можно ли создать ассет прямо отсюда.</param>
    private void DrawAssetSource(
        SerializedObject serializedObject,
        UnityEngine.Object target,
        SerializedProperty property,
        string sourceNote,
        string scopeNote,
        bool allowCreate)
    {
        UnityEngine.Object asset = property.objectReferenceValue;

        if (asset == null)
        {
            EditorGUILayout.HelpBox($"{sourceNote} Ассет не задан.", MessageType.Warning);

            if (allowCreate && GUILayout.Button("Создать описание"))
                CreateMetadataAsset(serializedObject, target, property);

            return;
        }

        EditorGUILayout.LabelField(sourceNote, EditorStyles.miniLabel);
        EditorGUILayout.LabelField(scopeNote, EditorStyles.miniLabel);

        sourceExpanded = DrawEmbedded(asset, sourceExpanded);
    }

    /// <summary>
    /// Рисует переопределение описания для этого экземпляра, если оно есть.
    /// </summary>
    /// <remarks>
    /// Блок выглядит так же, как общее описание, поэтому область действия у него
    /// подписана отдельно: перепутать их - значит поправить всем то, что хотел
    /// поправить одному.
    /// </remarks>
    private void DrawInstanceOverride(UnityEngine.Object target)
    {
        if (target is not Component component)
            return;

        var provider = component.GetComponent<EntityMetadataProvider>();

        if (provider == null)
            return;

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Переопределение экземпляра", EditorStyles.boldLabel);

        if (provider.EntityMetadataData == null)
        {
            EditorGUILayout.HelpBox(
                "На объекте висит EntityMetadataProvider, но ассет в нём не задан - " +
                "переопределения не будет.",
                MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Только этот экземпляр.", EditorStyles.miniLabel);

        overrideExpanded = DrawEmbedded(provider.EntityMetadataData, overrideExpanded);
    }

    /// <summary>
    /// Рисует ассет вместе с его инспектором.
    /// </summary>
    /// <returns>Развёрнут ли блок.</returns>
    private bool DrawEmbedded(UnityEngine.Object asset, bool expanded)
    {
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField(asset, typeof(UnityEngine.Object), false);

        expanded = EditorGUILayout.Foldout(expanded, "Поля описания", true);

        if (!expanded)
            return false;

        Editor editor = GetOrCreateEditor(asset);

        if (editor == null)
            return true;

        using (new EditorGUI.IndentLevelScope())
            editor.OnInspectorGUI();

        return true;
    }

    private Editor GetOrCreateEditor(UnityEngine.Object asset)
    {
        if (embeddedEditors.TryGetValue(asset, out Editor editor) && editor != null)
            return editor;

        editor = Editor.CreateEditor(asset);
        embeddedEditors[asset] = editor;

        return editor;
    }

    /// <summary>
    /// Ищет у сущности сериализованную ссылку на описание.
    /// </summary>
    /// <remarks>
    /// По типу поля, а не по его имени: у оружия оно зовётся <c>kindInfo</c>,
    /// у эффектов - <c>kindInfo</c>, и список имён пришлось бы
    /// пополнять при каждом новом виде сущности.
    /// </remarks>
    private static SerializedProperty FindMetadataAssetProperty(
        SerializedObject serializedObject,
        UnityEngine.Object target)
    {
        foreach (SerializedProperty property in PRSDKInspectorUtility.GetRootProperties(serializedObject))
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
                continue;

            Type fieldType = PRSDKInspectorUtility.GetFieldType(target.GetType(), property);

            if (fieldType != null && typeof(EntityMetadataBase).IsAssignableFrom(fieldType))
                return property;
        }

        return null;
    }

    /// <summary>
    /// Создаёт ассет описания и связывает его с сущностью.
    /// </summary>
    /// <remarks>
    /// Регистрировать описание больше нигде не нужно: окно описаний ищет ассеты по проекту,
    /// а в сборку описание попадает по ссылке с сущности.
    /// </remarks>
    private static void CreateMetadataAsset(
        SerializedObject serializedObject,
        UnityEngine.Object target,
        SerializedProperty property)
    {
        Type fieldType = PRSDKInspectorUtility.GetFieldType(target.GetType(), property);
        List<Type> types = FindMetadataTypes(fieldType);

        if (types.Count == 0)
        {
            Debug.LogError(
                $"[EntityDescriptionSection] Не найдено ни одного типа описания, подходящего " +
                $"полю {property.propertyPath} ({fieldType?.Name ?? "?"}).");
            return;
        }

        if (types.Count == 1)
        {
            CreateMetadataAsset(serializedObject, target, property.propertyPath, types[0]);
            return;
        }

        // Путь свойства, а не само свойство: меню показывается в следующем кадре,
        // а SerializedProperty до него не доживает.
        string propertyPath = property.propertyPath;
        var menu = new GenericMenu();

        foreach (Type type in types)
        {
            Type captured = type;
            menu.AddItem(
                new GUIContent(captured.Name),
                false,
                () => CreateMetadataAsset(serializedObject, target, propertyPath, captured));
        }

        menu.ShowAsContext();
    }

    /// <summary>
    /// Создаёт ассет описания выбранного типа.
    /// </summary>
    private static void CreateMetadataAsset(
        SerializedObject serializedObject,
        UnityEngine.Object target,
        string propertyPath,
        Type type)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);

        if (property == null)
            return;

        string path = EditorUtility.SaveFilePanelInProject(
            "Новое описание сущности",
            $"{target.GetType().Name}Metadata",
            "asset",
            "Где сохранить описание");

        if (string.IsNullOrEmpty(path))
            return;

        var asset = (EntityMetadataBase)ScriptableObject.CreateInstance(type);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        property.objectReferenceValue = asset;
        serializedObject.ApplyModifiedProperties();

        EditorGUIUtility.PingObject(asset);
    }

    /// <summary>
    /// Типы описаний, которые можно положить в поле.
    /// </summary>
    /// <remarks>
    /// Наследники <see cref="EntityMetadataBase"/> заводятся ради собственных полей,
    /// поэтому выбор оставлен настройщику. Когда подходит один тип, спрашивать не о чем.
    /// <para>
    /// Список ограничен типом поля: у сущности вида <c>EntityBase&lt;PlayerMetadata&gt;</c>
    /// описание другого типа в поле просто не поместится, и предлагать его значит
    /// предлагать ошибку.
    /// </para>
    /// </remarks>
    private static List<Type> FindMetadataTypes(Type fieldType)
    {
        Type required = fieldType != null && typeof(EntityMetadataBase).IsAssignableFrom(fieldType)
            ? fieldType
            : typeof(EntityMetadataBase);

        return TypeCache.GetTypesDerivedFrom<EntityMetadataBase>()
            .Append(typeof(EntityMetadataBase))
            .Where(candidate =>
                !candidate.IsAbstract
                && !candidate.ContainsGenericParameters
                && required.IsAssignableFrom(candidate))
            .Distinct()
            .OrderBy(candidate => candidate.Name)
            .ToList();
    }
}
