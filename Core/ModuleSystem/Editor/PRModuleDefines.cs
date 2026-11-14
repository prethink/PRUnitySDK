using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

/// <summary>
/// Какие модули отключены: define-символы <c>PRSDK_DISABLE_*</c> в Player Settings.
/// </summary>
/// <remarks>
/// Символы пишутся во все платформы сразу: иначе сборка под другую платформу
/// молча вернула бы отключённые модули.
/// </remarks>
public static class PRModuleDefines
{
    /// <summary>
    /// Идентификаторы модулей, отключённых на текущей платформе.
    /// </summary>
    public static HashSet<string> ReadDisabledIds()
    {
        return ReadDisabledIds(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
    }

    /// <summary>
    /// Отключённые модули платформы сборки — по её символам, а не по символам платформы, выбранной в редакторе.
    /// </summary>
    /// <remarks>
    /// Обычно наборы совпадают: окно модулей пишет символы во все платформы. Но сборку запускают и скриптом под
    /// другую платформу, а символы правят руками и сливают в git — проверке сборки нужен набор именно собираемой.
    /// </remarks>
    public static HashSet<string> ReadDisabledIds(BuildTarget target)
    {
        return ReadDisabledIds(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target)));
    }

    /// <summary>
    /// Идентификаторы отключённых модулей из строки символов Player Settings.
    /// </summary>
    public static HashSet<string> ParseDisabledIds(string symbols)
    {
        return new HashSet<string>(Split(symbols)
            .Where(IsModuleSymbol)
            .Select(symbol => symbol.Substring(PRModuleManifest.DefinePrefix.Length)));
    }

    private static HashSet<string> ReadDisabledIds(NamedBuildTarget target)
    {
        return ParseDisabledIds(PlayerSettings.GetScriptingDefineSymbols(target));
    }

    /// <summary>
    /// Платформы, у которых набор отключённых модулей отличается от текущей.
    /// </summary>
    public static List<string> FindMismatchedTargets()
    {
        var current = ReadDisabledIds();
        var result = new List<string>();

        foreach (NamedBuildTarget target in GetTargets())
        {
            if (!current.SetEquals(ReadDisabledIds(target)))
                result.Add(target.TargetName);
        }

        return result;
    }

    /// <summary>
    /// Записывает набор отключённых модулей во все платформы. Прочие символы не трогает.
    /// </summary>
    /// <remarks>
    /// Смена символов текущей платформы запускает перекомпиляцию всего проекта.
    /// </remarks>
    public static void WriteDisabledIds(IEnumerable<string> disabledIds)
    {
        string[] moduleSymbols = disabledIds
            .Select(id => PRModuleManifest.DefinePrefix + id)
            .OrderBy(symbol => symbol, StringComparer.Ordinal)
            .ToArray();

        foreach (NamedBuildTarget target in GetTargets())
        {
            string current = PlayerSettings.GetScriptingDefineSymbols(target);
            string updated = string.Join(";", Split(current).Where(symbol => !IsModuleSymbol(symbol)).Concat(moduleSymbols));

            if (updated != current)
                PlayerSettings.SetScriptingDefineSymbols(target, updated);
        }
    }

    private static IEnumerable<NamedBuildTarget> GetTargets()
    {
        var seen = new HashSet<string>();

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

            if (seen.Add(target.TargetName))
                yield return target;
        }

        if (seen.Add(NamedBuildTarget.Server.TargetName))
            yield return NamedBuildTarget.Server;
    }

    private static bool IsObsolete(BuildTargetGroup group)
    {
        var field = typeof(BuildTargetGroup).GetField(group.ToString());
        return field != null && field.IsDefined(typeof(ObsoleteAttribute), false);
    }

    private static bool IsModuleSymbol(string symbol)
    {
        return symbol.StartsWith(PRModuleManifest.DefinePrefix, StringComparison.Ordinal);
    }

    private static IEnumerable<string> Split(string symbols)
    {
        return (symbols ?? string.Empty)
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(symbol => symbol.Trim())
            .Where(symbol => symbol.Length > 0);
    }
}
