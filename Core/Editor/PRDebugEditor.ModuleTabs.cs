using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class PRDebugEditor
{
    /// <summary>
    /// Вкладки, объявленные модулями через <see cref="IPRDebugTab"/>.
    /// </summary>
    private readonly List<IPRDebugTab> moduleTabs = new();

    /// <summary>
    /// Ошибки вкладок модулей: создания, обновления, отрисовки.
    /// </summary>
    /// <remarks>
    /// По вкладке, а не общим полем: сломанная вкладка одного модуля не должна прятать
    /// окно целиком, ошибка показывается прямо в ней.
    /// </remarks>
    private readonly Dictionary<IPRDebugTab, string> moduleTabErrors = new();

    private readonly List<string> moduleTabLoadErrors = new();

    private void DiscoverModuleTabs()
    {
        moduleTabs.Clear();
        moduleTabErrors.Clear();
        moduleTabLoadErrors.Clear();

        foreach (Type tabType in TypeCache.GetTypesDerivedFrom<IPRDebugTab>())
        {
            if (tabType.IsAbstract || tabType.ContainsGenericParameters)
                continue;

            try
            {
                if (Activator.CreateInstance(tabType) is IPRDebugTab tab)
                    moduleTabs.Add(tab);
            }
            catch (Exception exception)
            {
                moduleTabLoadErrors.Add($"Cannot create debug tab '{tabType.FullName}': {exception.GetBaseException().Message}");
            }
        }

        moduleTabs.Sort((left, right) =>
        {
            int order = left.Order.CompareTo(right.Order);
            return order != 0 ? order : string.CompareOrdinal(left.GetType().FullName, right.GetType().FullName);
        });
    }

    private PRDebugTabContext CreateModuleTabContext()
    {
        return new PRDebugTabContext(search, executor, EditorApplication.isPlaying);
    }

    private void RefreshModuleTabs()
    {
        PRDebugTabContext context = CreateModuleTabContext();

        foreach (IPRDebugTab tab in moduleTabs)
        {
            try
            {
                tab.Refresh(context);
                moduleTabErrors.Remove(tab);
            }
            catch (Exception exception)
            {
                moduleTabErrors[tab] = $"Refresh failed: {exception.GetType().Name}: {exception.Message}";
            }
        }
    }

    /// <summary>
    /// Вкладки модулей для общего списка в Play Mode; вкладки игр (<see cref="IPRDebugGameTab"/>) — в конце,
    /// и только активного проекта.
    /// </summary>
    private IEnumerable<(string name, Action draw)> GetModuleTabEntries()
    {
        IEnumerable<(string name, Action draw)> modules = moduleTabs.Where(tab => tab is not IPRDebugGameTab).Select(ToEntry);
        PRSDKProject active = PRSDKActiveProject.Instance != null ? PRSDKActiveProject.Instance.Project : null;
        IPRDebugTab[] games = moduleTabs.Where(tab => tab is IPRDebugGameTab game && IsForProject(game, active)).ToArray();

        if (games.Length == 0)
            return modules;

        // Пустая строка в списке Unity рисуется разделителем и не выбирается: игры отделены от разделов SDK.
        return modules.Append((string.Empty, () => { })).Concat(games.Select(ToEntry));
    }

    /// <summary>
    /// Вкладка игры относится к активному проекту. Без активного проекта видны все.
    /// </summary>
    private static bool IsForProject(IPRDebugGameTab tab, PRSDKProject active)
    {
        if (active == null)
            return true;

        try
        {
            return string.IsNullOrEmpty(tab.Project) || string.Equals(tab.Project, active.Title, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            // Сломанную вкладку лучше показать с ошибкой внутри, чем молча спрятать.
            return true;
        }
    }

    private (string name, Action draw) ToEntry(IPRDebugTab tab) => (SafeTitle(tab), () => DrawModuleTab(tab));

    private void DrawModuleTab(IPRDebugTab tab)
    {
        if (moduleTabErrors.TryGetValue(tab, out string error))
            EditorGUILayout.HelpBox(error, MessageType.Error);

        try
        {
            tab.Draw(CreateModuleTabContext());
        }
        catch (ExitGUIException)
        {
            throw;
        }
        catch (Exception exception)
        {
            moduleTabErrors[tab] = $"Draw failed: {exception.GetType().Name}: {exception.Message}";
        }
    }

    private static string SafeTitle(IPRDebugTab tab)
    {
        try
        {
            return string.IsNullOrEmpty(tab.Title) ? tab.GetType().Name : tab.Title;
        }
        catch (Exception)
        {
            return tab.GetType().Name;
        }
    }
}
