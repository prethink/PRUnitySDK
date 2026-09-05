using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Список сцен, в которых используется описание.
/// </summary>
/// <remarks>
/// Сущность живёт не только в префабах: половина объектов стоит прямо в сценах, и без
/// этого блока ответ «где это описание используется» был бы неполным.
/// <para>
/// Сцены показываются отдельно от префабов, потому что и знаем мы о них разное. Сам факт
/// ссылки виден всегда - его даёт <see cref="EntityMetadataUsageIndex"/> по зависимостям.
/// А вот какие именно объекты внутри, видно только у открытой сцены: заглянуть в закрытую
/// можно лишь открыв её, а это меняет то, с чем человек сейчас работает.
/// </para>
/// </remarks>
public sealed class EntitySceneUsageList
{
    private Object shownAsset;
    private readonly Dictionary<string, bool> expanded = new();

    /// <summary>
    /// Рисует список сцен, ссылающихся на ассет.
    /// </summary>
    /// <param name="asset">Описание или определение.</param>
    public void Draw(Object asset)
    {
        if (shownAsset != asset)
        {
            shownAsset = asset;
            expanded.Clear();
        }

        IReadOnlyList<string> scenes = CollectScenes(asset);

        EditorGUILayout.LabelField($"Сцены ({scenes.Count})", EditorStyles.boldLabel);

        if (scenes.Count == 0)
        {
            EditorGUILayout.HelpBox("Ни одна сцена не ссылается на это описание.", MessageType.Info);
            return;
        }

        foreach (string path in scenes)
            DrawScene(path, asset);
    }

    /// <summary>
    /// Собирает сцены, где используется описание.
    /// </summary>
    /// <remarks>
    /// Открытые сцены просматриваются вживую, а не по индексу: индекс читает зависимости
    /// с диска, и объект, только что добавленный в сцену, туда не попадёт до сохранения.
    /// Сцена при этом открыта, объект видно в иерархии - и пустой список в окне выглядит
    /// ошибкой окна, а не следствием несохранённой сцены.
    /// <para>
    /// Закрытые сцены остаются за индексом: заглянуть в них можно только открыв.
    /// </para>
    /// </remarks>
    private static IReadOnlyList<string> CollectScenes(Object asset)
    {
        var paths = new List<string>();

        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene scene = SceneManager.GetSceneAt(index);

            if (scene.isLoaded && FindEntities(scene, asset).Count > 0)
                paths.Add(scene.path);
        }

        foreach (string path in EntityMetadataUsageIndex.GetScenes(asset))
        {
            if (!paths.Contains(path))
                paths.Add(path);
        }

        paths.Sort(System.StringComparer.OrdinalIgnoreCase);

        return paths;
    }

    /// <summary>
    /// Сущности сцены, использующие это описание.
    /// </summary>
    private static List<EntityBase> FindEntities(Scene scene, Object asset)
    {
        var found = new List<EntityBase>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (EntityBase entity in root.GetComponentsInChildren<EntityBase>(true))
            {
                if (entity != null && EntityDescriptionSource.Resolve(entity) == asset)
                    found.Add(entity);
            }
        }

        return found;
    }

    private void DrawScene(string path, Object asset)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        bool isOpen = scene.IsValid() && scene.isLoaded;

        using (new EditorGUILayout.HorizontalScope())
        {
            string title = Path.GetFileNameWithoutExtension(path);

            if (isOpen)
            {
                // Открытая сцена разворачивается сразу: ради объектов внутри блок и нужен,
                // а лишний клик на каждую сцену только мешает.
                if (!expanded.TryGetValue(path, out bool open))
                    open = true;

                expanded[path] = EditorGUILayout.Foldout(open, title, true);
            }
            else
            {
                EditorGUILayout.LabelField(title, EditorStyles.label);
            }

            if (GUILayout.Button("Показать", EditorStyles.miniButton, GUILayout.Width(70f)))
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(path));

            if (!isOpen && GUILayout.Button("Открыть", EditorStyles.miniButton, GUILayout.Width(70f)))
            {
                OpenScene(path);
                GUIUtility.ExitGUI();
            }
        }

        if (!isOpen)
        {
            EditorGUILayout.LabelField(
                "    Сцена закрыта - объекты видны после открытия.",
                EditorStyles.miniLabel);
            return;
        }

        if (expanded.TryGetValue(path, out bool isExpanded) && isExpanded)
            DrawSceneEntities(scene, asset);
    }

    /// <summary>
    /// Рисует сущности открытой сцены, использующие это описание.
    /// </summary>
    private void DrawSceneEntities(Scene scene, Object asset)
    {
        using (new EditorGUI.IndentLevelScope())
        {
            List<EntityBase> found = FindEntities(scene, asset);

            if (found.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "Объектов с этим описанием не найдено - возможно, ссылка идёт из префаба внутри сцены.",
                    EditorStyles.miniLabel);
                return;
            }

            foreach (EntityBase entity in found)
                DrawEntity(entity);

            // Несохранённая сцена в индекс не попадает: её ссылки на диске ещё нет.
            // Без пометки было бы непонятно, почему та же сцена не видна в отчётах.
            if (scene.isDirty)
            {
                EditorGUILayout.LabelField(
                    "Сцена не сохранена - в поиске по проекту эти объекты пока не видны.",
                    EditorStyles.miniLabel);
            }
        }
    }

    private static void DrawEntity(EntityBase entity)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"{entity.name} ({entity.GetType().Name})");

            if (GUILayout.Button("Выделить", EditorStyles.miniButton, GUILayout.Width(70f)))
            {
                Selection.activeGameObject = entity.gameObject;
                EditorGUIUtility.PingObject(entity.gameObject);
            }
        }
    }

    /// <summary>
    /// Открывает сцену, дав сохранить текущую.
    /// </summary>
    /// <remarks>
    /// Открытие заменяет то, с чем человек работает, поэтому сначала спрашивается
    /// сохранение: иначе несохранённые правки пропадут без предупреждения.
    /// </remarks>
    private static void OpenScene(string path)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }
}
