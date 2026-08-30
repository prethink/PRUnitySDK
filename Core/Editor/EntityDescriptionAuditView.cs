using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Вкладка «Проверка»: всё, что не так с описаниями проекта.
/// </summary>
/// <remarks>
/// Проверки по одному ассету видны в его карточке, но так их не собрать: чтобы заметить
/// сущность без описания, нужно на неё наткнуться. Вкладка собирает то же самое разом.
/// <para>
/// Открытые сцены проверяются всегда - это бесплатно. Префабы приходится грузить, чтобы
/// отличить «ссылки нет» от «сущности нет», а их тысячи, поэтому это отдельный режим.
/// Закрытые сцены не проверяются вовсе: заглянуть в них можно лишь открыв, а это меняет
/// то, с чем человек сейчас работает.
/// </para>
/// </remarks>
public sealed class EntityDescriptionAuditView : ScriptableObject
{
    private readonly List<EntityDescriptionIssue> issues = new();

    private Vector2 scroll;
    private bool scanned;
    private bool includePrefabs;

    /// <summary>
    /// Рисует вкладку.
    /// </summary>
    public void Draw()
    {
        DrawToolbar();

        if (!scanned)
        {
            EditorGUILayout.HelpBox(
                "Нажмите «Проверить», чтобы собрать проблемы описаний.",
                MessageType.Info);
            return;
        }

        if (issues.Count == 0)
        {
            EditorGUILayout.HelpBox("Проблем не найдено.", MessageType.Info);
            return;
        }

        DrawSummary();
        DrawList();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Проверить", EditorStyles.toolbarButton, GUILayout.Width(90f)))
            {
                Scan();
                GUIUtility.ExitGUI();
            }

            includePrefabs = GUILayout.Toggle(
                includePrefabs,
                new GUIContent(
                    "Искать по всем префабам",
                    "Открытые сцены проверяются всегда. Этот режим дополнительно грузит "
                    + "все префабы проекта - заметно дольше."),
                EditorStyles.toolbarButton,
                GUILayout.Width(220f));

            GUILayout.FlexibleSpace();
        }
    }

    private void DrawSummary()
    {
        int errors = issues.Count(issue => issue.Severity == MessageType.Error);
        int warnings = issues.Count(issue => issue.Severity == MessageType.Warning);
        int infos = issues.Count - errors - warnings;

        EditorGUILayout.LabelField(
            $"Ошибок: {errors}   предупреждений: {warnings}   замечаний: {infos}",
            EditorStyles.boldLabel);
    }

    private void DrawList()
    {
        using var view = new EditorGUILayout.ScrollViewScope(scroll);

        foreach (EntityDescriptionIssue issue in issues)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.HelpBox(
                    issue.Target != null ? $"{issue.Target.name}: {issue.Message}" : issue.Message,
                    issue.Severity);

                using (new EditorGUI.DisabledScope(issue.Target == null))
                {
                    if (GUILayout.Button("Показать", GUILayout.Width(80f), GUILayout.Height(30f)))
                        EditorGUIUtility.PingObject(issue.Target);
                }
            }
        }

        scroll = view.scrollPosition;
    }

    /// <summary>
    /// Собирает проблемы заново.
    /// </summary>
    /// <remarks>
    /// Порядок - от серьёзного к мелкому: с ошибками разбираются в первую очередь,
    /// а замечаний вроде «описанием никто не пользуется» обычно много и они терпят.
    /// </remarks>
    private void Scan()
    {
        issues.Clear();

        foreach (ScriptableObject asset in FindDescriptions())
            issues.AddRange(EntityDescriptionValidator.Validate(asset));

        // Открытые сцены проверяются всегда: это бесплатно и покрывает то, с чем
        // человек работает прямо сейчас.
        issues.AddRange(EntityDescriptionValidator.FindSceneEntitiesWithoutDescription());

        if (includePrefabs)
            issues.AddRange(EntityDescriptionValidator.FindEntitiesWithoutDescription());

        issues.Sort((left, right) => Rank(left.Severity).CompareTo(Rank(right.Severity)));
        scanned = true;
    }

    private static IEnumerable<ScriptableObject> FindDescriptions()
    {
        return AssetDatabase.FindAssets($"t:{nameof(EntityMetadataBase)}")
            .Concat(AssetDatabase.FindAssets($"t:{nameof(ItemDefinitionBase)}"))
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct(System.StringComparer.Ordinal)
            .Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>)
            .Where(asset => asset is IEntityMetadata);
    }

    private static int Rank(MessageType severity)
    {
        return severity switch
        {
            MessageType.Error => 0,
            MessageType.Warning => 1,
            _ => 2
        };
    }
}
