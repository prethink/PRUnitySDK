using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Окно правил характеристик активного проекта SDK.
/// </summary>
/// <remarks>
/// Правит раздел <see cref="PRSDKSettings.StatRules"/> настроек того проекта, что сейчас активен: у каждой игры
/// свой набор. Показывает противоречия (минимум выше максимума), правила без характеристики и даёт проверить,
/// во что превратится значение. Во время игры правка сразу перезагружает <see cref="GameRules"/>.
/// </remarks>
public sealed class GameRulesWindow : EditorWindow
{
    private const float TypeWidth = 110f;
    private const float StatWidth = 190f;
    private const float FieldWidth = 140f;
    private const float PriorityWidth = 90f;
    private const float ButtonWidth = 22f;

    private PRSDKSettings target;
    private SerializedObject serializedSettings;
    private Vector2 scroll;
    private string search = string.Empty;
    private int testStatIndex;
    private float testValue;

    [MenuItem("PRUnitySDK/Windows/Game Rules", false, 32)]
    private static void Open()
    {
        GameRulesWindow window = GetWindow<GameRulesWindow>();
        window.titleContent = new GUIContent("Game Rules");
        window.minSize = new Vector2(720f, 360f);
        window.Show();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("Game Rules");
        PRSDKActiveProject.EditedInEditor += Repaint;
        PRSDKProject.EditedInEditor += Repaint;
    }

    private void OnDisable()
    {
        PRSDKActiveProject.EditedInEditor -= Repaint;
        PRSDKProject.EditedInEditor -= Repaint;
    }

    private void OnGUI()
    {
        if (!Bind())
        {
            EditorGUILayout.HelpBox("Не найден ассет PRSDKSettings активного проекта.", MessageType.Error);
            return;
        }

        serializedSettings.UpdateIfRequiredOrScript();
        SerializedProperty list = serializedSettings
            .FindProperty($"<{nameof(PRSDKSettings.StatRules)}>k__BackingField")
            ?.FindPropertyRelative(GameRulesSettings.RulesFieldName);

        if (list == null)
        {
            EditorGUILayout.HelpBox("В PRSDKSettings нет раздела правил.", MessageType.Error);
            return;
        }

        DrawHeader();
        DrawToolbar(list);
        DrawProblems();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawRules(list);
        EditorGUILayout.EndScrollView();

        DrawTester();

        if (serializedSettings.ApplyModifiedProperties() && Application.isPlaying && GameRules.IsInitialized)
            GameRules.Initialize();
    }

    /// <summary>
    /// Берёт настройки активного проекта; после смены проекта — новые.
    /// </summary>
    private bool Bind()
    {
        PRSDKSettings settings = PRSDKSettings.Instance;
        if (settings == null || !EditorUtility.IsPersistent(settings))
            return false;

        if (settings != target || serializedSettings == null || serializedSettings.targetObject == null)
        {
            target = settings;
            serializedSettings = new SerializedObject(settings);
        }

        return true;
    }

    private void DrawHeader()
    {
        PRSDKProject project = PRSDKActiveProject.Instance != null ? PRSDKActiveProject.Instance.Project : null;
        string title = project != null ? $"Правила характеристик — {project.Title}" : "Правила характеристик";
        PRSDKInspectorUtility.DrawHeader(title, target);

        EditorGUILayout.HelpBox(
            "Ограничения, которые применяются к характеристикам последними — после базовых значений и модификаторов. " +
            "Правила свои у каждого проекта SDK: здесь показан активный. Порядок — по возрастанию приоритета, " +
            "при равном — как в списке.",
            MessageType.None);
    }

    private void DrawToolbar(SerializedProperty list)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Добавить правило", EditorStyles.toolbarDropDown, GUILayout.Width(130f)))
            ShowAddMenu(list);

        GUILayout.FlexibleSpace();
        GUILayout.Label("Поиск", EditorStyles.miniLabel, GUILayout.Width(40f));
        search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.Width(200f));

        if (GUILayout.Button("Настройки", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            EditorGUIUtility.PingObject(target);

        EditorGUILayout.EndHorizontal();
    }

    private void ShowAddMenu(SerializedProperty list)
    {
        var menu = new GenericMenu();
        IEnumerable<Type> types = TypeCache.GetTypesDerivedFrom<StatRuleBase>()
            .Where(type => !type.IsAbstract && !type.ContainsGenericParameters && type.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(type => type.Name, StringComparer.Ordinal);

        foreach (Type type in types)
        {
            Type captured = type;
            menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), false, () =>
            {
                serializedSettings.Update();
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).managedReferenceValue = Activator.CreateInstance(captured);
                serializedSettings.ApplyModifiedProperties();
                if (Application.isPlaying && GameRules.IsInitialized)
                    GameRules.Initialize();
            });
        }

        menu.ShowAsContext();
    }

    /// <summary>
    /// Противоречия: нижняя граница выше верхней, правила без характеристики.
    /// </summary>
    private void DrawProblems()
    {
        IReadOnlyList<StatRuleBase> rules = target.StatRules.Rules;

        int empty = rules.Count(rule => rule == null || rule.Stat == null);
        if (empty > 0)
            EditorGUILayout.HelpBox($"Правил без типа или характеристики: {empty}. Они не применяются.", MessageType.Warning);

        foreach (IGrouping<string, StatRuleBase> group in rules.Where(rule => rule?.Stat != null).GroupBy(rule => rule.Stat.Value))
        {
            float[] mins = group.OfType<MinValueRule>().Select(rule => rule.MinValue).ToArray();
            float[] maxes = group.OfType<MaxValueRule>().Select(rule => rule.MaxValue).ToArray();
            if (mins.Length > 0 && maxes.Length > 0 && mins.Max() > maxes.Min())
            {
                EditorGUILayout.HelpBox(
                    $"{group.Key}: минимум {mins.Max()} выше максимума {maxes.Min()} — итог зависит от порядка правил.",
                    MessageType.Warning);
            }
        }
    }

    private void DrawRules(SerializedProperty list)
    {
        if (list.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Правил нет: характеристики не ограничиваются.", MessageType.Info);
            return;
        }

        int removeAt = -1;
        int moveFrom = -1;
        int moveTo = -1;

        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            object value = element.managedReferenceValue;
            SerializedProperty stat = value != null ? element.FindPropertyRelative(StatRuleBase.StatFieldName) : null;

            if (!string.IsNullOrEmpty(search) &&
                (stat == null || stat.stringValue.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0))
                continue;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            if (value == null)
            {
                EditorGUILayout.LabelField("Пустое правило — удалите или добавьте заново", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(value.GetType().Name), EditorStyles.boldLabel,
                    GUILayout.Width(TypeWidth));

                Rect statRect = GUILayoutUtility.GetRect(StatWidth, EditorGUIUtility.singleLineHeight, GUILayout.Width(StatWidth));
                StatKeyDrawer.Draw(statRect, stat, GUIContent.none);

                DrawOwnFields(element);
                GUILayout.FlexibleSpace();

                SerializedProperty priority = element.FindPropertyRelative(StatRuleBase.PriorityFieldName);
                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 60f;
                EditorGUILayout.PropertyField(priority, new GUIContent("Приоритет", priority.tooltip), GUILayout.Width(PriorityWidth));
                EditorGUIUtility.labelWidth = labelWidth;
            }

            using (new EditorGUI.DisabledScope(i == 0))
            {
                if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(ButtonWidth)))
                    (moveFrom, moveTo) = (i, i - 1);
            }

            using (new EditorGUI.DisabledScope(i == list.arraySize - 1))
            {
                if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(ButtonWidth)))
                    (moveFrom, moveTo) = (i, i + 1);
            }

            if (GUILayout.Button("✕", EditorStyles.miniButtonRight, GUILayout.Width(ButtonWidth)))
                removeAt = i;

            EditorGUILayout.EndHorizontal();
        }

        if (moveFrom >= 0)
            list.MoveArrayElement(moveFrom, moveTo);

        if (removeAt >= 0)
        {
            // У managed reference удаление ссылки сразу убирает и элемент, второго вызова не нужно.
            list.DeleteArrayElementAtIndex(removeAt);
        }
    }

    /// <summary>
    /// Поля конкретного правила (граница, шаг…), кроме общих характеристики и приоритета.
    /// </summary>
    private static void DrawOwnFields(SerializedProperty element)
    {
        SerializedProperty child = element.Copy();
        SerializedProperty end = element.GetEndProperty();
        float labelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 70f;

        for (bool enter = true; child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end); enter = false)
        {
            if (child.name == StatRuleBase.StatFieldName || child.name == StatRuleBase.PriorityFieldName)
                continue;

            EditorGUILayout.PropertyField(child, new GUIContent(ShortLabel(child), child.tooltip), true, GUILayout.Width(FieldWidth));
        }

        EditorGUIUtility.labelWidth = labelWidth;
    }

    private static string ShortLabel(SerializedProperty property)
    {
        return property.name switch
        {
            "minValue" => "Минимум",
            "maxValue" => "Максимум",
            _ => property.displayName,
        };
    }

    /// <summary>
    /// Что вернут правила для введённого значения — по текущему списку, без запуска игры.
    /// </summary>
    private void DrawTester()
    {
        string[] stats = target.StatRules.Rules
            .Where(rule => rule?.Stat != null)
            .Select(rule => rule.Stat.Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(stat => stat, StringComparer.Ordinal)
            .ToArray();

        if (stats.Length == 0)
            return;

        testStatIndex = Mathf.Clamp(testStatIndex, 0, stats.Length - 1);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Проверка значения", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        testStatIndex = EditorGUILayout.Popup(testStatIndex, stats, GUILayout.Width(StatWidth));
        testValue = EditorGUILayout.FloatField(testValue, GUILayout.Width(90f));

        float result = Apply(stats[testStatIndex], testValue);
        bool changed = !Mathf.Approximately(result, testValue);
        EditorGUILayout.LabelField(changed ? $"→  {result}  (ограничено)" : $"→  {result}", GUILayout.Width(220f));

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private float Apply(string stat, float value)
    {
        IEnumerable<StatRuleBase> ordered = target.StatRules.Rules
            .Select((rule, index) => (rule, index))
            .Where(entry => entry.rule?.Stat != null && entry.rule.Stat.Value == stat)
            .OrderBy(entry => entry.rule.Priority)
            .ThenBy(entry => entry.index)
            .Select(entry => entry.rule);

        foreach (StatRuleBase rule in ordered)
            value = rule.Apply(value);

        return value;
    }
}
