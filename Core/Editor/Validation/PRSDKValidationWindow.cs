using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Окно проверки проекта: собирает проблемы ассетов и чинит те, что чинятся сами.
/// </summary>
/// <remarks>
/// Проверки добавляются реализацией <see cref="IProjectValidator"/> рядом со своим
/// модулем — окно находит их само и правки не требует.
/// </remarks>
public sealed class PRSDKValidationWindow : ExtendedEditorWindow
{
    private const string MenuPath = "PRUnitySDK/Windows/Проверка проекта";

    /// <summary>
    /// Результаты последней проверки; <c>null</c>, пока её не запускали.
    /// </summary>
    private List<ValidatorResult> results;

    private Vector2 scroll;

    private int issueCount;

    private int fixableCount;

    /// <summary>
    /// Результат одной проверки.
    /// </summary>
    private sealed class ValidatorResult
    {
        public string Title;

        public List<ProjectValidationIssue> Issues = new();
    }

    [MenuItem(MenuPath, false, 18)]
    private static void Open()
    {
        var window = GetWindow<PRSDKValidationWindow>();
        window.titleContent = new GUIContent("Проверка проекта");
        window.minSize = new Vector2(560f, 320f);
        window.Show();
    }

    private void OnGUI()
    {
        CreateHorizontalToolBar(DrawToolbarButtons);

        if (results == null)
        {
            EditorGUILayout.HelpBox("Нажмите «Проверить».", MessageType.Info);
            return;
        }

        if (issueCount == 0)
        {
            EditorGUILayout.HelpBox("Проблем не найдено.", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (ValidatorResult result in results.Where(result => result.Issues.Count > 0))
        {
            EditorGUILayout.LabelField($"{result.Title} — {result.Issues.Count}", EditorStyles.boldLabel);

            foreach (ProjectValidationIssue issue in result.Issues)
                DrawIssue(issue);

            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbarButtons()
    {
        if (GUILayout.Button("Проверить", EditorStyles.toolbarButton, GUILayout.Width(90f)))
            Check();

        using (new EditorGUI.DisabledScope(fixableCount == 0))
        {
            if (GUILayout.Button($"Исправить всё ({fixableCount})", EditorStyles.toolbarButton, GUILayout.Width(140f)))
                FixAll();
        }
    }

    private void DrawIssue(ProjectValidationIssue issue)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.HelpBox(issue.Message, issue.Severity);

        if (issue.Target != null && GUILayout.Button("Показать", GUILayout.Width(80f)))
        {
            Selection.activeObject = issue.Target;
            EditorGUIUtility.PingObject(issue.Target);
        }

        if (issue.Fix != null && GUILayout.Button(issue.FixTitle ?? "Исправить", GUILayout.Width(110f)))
        {
            RunFix(issue);
            AssetDatabase.SaveAssets();
            Check();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void Check()
    {
        results = new List<ValidatorResult>();

        foreach (Type type in TypeCache.GetTypesDerivedFrom<IProjectValidator>())
        {
            if (type.IsAbstract || type.ContainsGenericParameters)
                continue;

            var result = new ValidatorResult { Title = type.Name };

            try
            {
                var validator = (IProjectValidator)Activator.CreateInstance(type);
                result.Title = validator.Title;
                result.Issues.AddRange(validator.Validate()?.Where(issue => issue != null)
                    ?? Enumerable.Empty<ProjectValidationIssue>());
            }
            catch (Exception exception)
            {
                result.Issues.Add(new ProjectValidationIssue(MessageType.Error,
                    $"Проверка '{type.Name}' упала: {exception.GetBaseException().Message}"));
            }

            results.Add(result);
        }

        results.Sort((left, right) => string.CompareOrdinal(left.Title, right.Title));

        ProjectValidationIssue[] issues = results.SelectMany(result => result.Issues).ToArray();
        issueCount = issues.Length;
        fixableCount = issues.Count(issue => issue.Fix != null);
    }

    private void FixAll()
    {
        foreach (ProjectValidationIssue issue in results.SelectMany(result => result.Issues)
            .Where(issue => issue.Fix != null).ToArray())
        {
            RunFix(issue);
        }

        AssetDatabase.SaveAssets();
        Check();
    }

    private static void RunFix(ProjectValidationIssue issue)
    {
        try
        {
            issue.Fix();
        }
        catch (Exception exception)
        {
            Debug.LogError($"Не удалось исправить: {exception.GetBaseException().Message}");
        }
    }
}
