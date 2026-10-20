using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

/// <summary>
/// Включает и выключает тесты SDK символом <c>PRSDK_TESTS</c> сразу на всех платформах.
/// </summary>
/// <remarks>
/// Без пакета Test Framework тесты не собрались бы и остановили всю компиляцию, поэтому включение
/// сначала проверяет, что NUnit в проекте есть. Тесты выключенных модулей не собираются и с символом:
/// их файлы обёрнуты ещё и в <c>PRSDK_DISABLE_&lt;Id&gt;</c>.
/// </remarks>
public static class PRTestsDefineMenu
{
    public const string Symbol = "PRSDK_TESTS";
    private const string EnableMenu = "PRUnitySDK/Тесты/Включить тесты";
    private const string DisableMenu = "PRUnitySDK/Тесты/Выключить тесты";

    /// <summary>
    /// Символ стоит у платформы, выбранной для сборки.
    /// </summary>
    public static bool IsEnabled => Split(PlayerSettings.GetScriptingDefineSymbols(CurrentTarget)).Contains(Symbol);

    private static NamedBuildTarget CurrentTarget =>
        NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));

    [MenuItem(EnableMenu)]
    public static void Enable()
    {
        if (Type.GetType("NUnit.Framework.TestAttribute, nunit.framework") == null)
        {
            EditorUtility.DisplayDialog("Тесты", "В проекте нет NUnit. Установите пакет Test Framework " +
                                                 "(Window → Package Manager → Test Framework) и включите тесты снова.", "Понятно");
            return;
        }

        SetForAllTargets(true);
    }

    [MenuItem(DisableMenu)]
    public static void Disable() => SetForAllTargets(false);

    [MenuItem(EnableMenu, true)]
    private static bool CanEnable() => !IsEnabled;

    [MenuItem(DisableMenu, true)]
    private static bool CanDisable() => IsEnabled;

    private static void SetForAllTargets(bool enabled)
    {
        foreach (NamedBuildTarget target in Targets())
        {
            string current = PlayerSettings.GetScriptingDefineSymbols(target);
            List<string> symbols = Split(current).Where(symbol => symbol != Symbol).ToList();
            if (enabled)
                symbols.Add(Symbol);

            string updated = string.Join(";", symbols);
            if (updated != current)
                PlayerSettings.SetScriptingDefineSymbols(target, updated);
        }
    }

    /// <summary>
    /// Платформы, для которых в проекте уже заданы символы, и выбранная для сборки.
    /// </summary>
    private static IEnumerable<NamedBuildTarget> Targets()
    {
        var targets = new List<NamedBuildTarget> { CurrentTarget };
        foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
        {
            if (group == BuildTargetGroup.Unknown || IsObsolete(group))
                continue;

            NamedBuildTarget target;
            try
            {
                target = NamedBuildTarget.FromBuildTargetGroup(group);
            }
            catch (ArgumentException)
            {
                continue;
            }

            if (!targets.Contains(target) && !string.IsNullOrEmpty(PlayerSettings.GetScriptingDefineSymbols(target)))
                targets.Add(target);
        }

        return targets;
    }

    private static bool IsObsolete(BuildTargetGroup group)
    {
        return typeof(BuildTargetGroup).GetField(group.ToString())?.IsDefined(typeof(ObsoleteAttribute), false) == true;
    }

    private static IEnumerable<string> Split(string symbols)
    {
        return (symbols ?? string.Empty).Split(';').Select(symbol => symbol.Trim()).Where(symbol => symbol.Length > 0);
    }
}
