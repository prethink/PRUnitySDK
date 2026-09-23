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
            if (!context.IsPlaying && !tab.AvailableInEditMode)
                continue;

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
    /// Вкладки модулей для общего списка в Play Mode.
    /// </summary>
    private IEnumerable<(string name, Action draw)> GetModuleTabEntries()
    {
        return moduleTabs.Select(tab => (SafeTitle(tab), (Action)(() => DrawModuleTab(tab))));
    }

    /// <summary>
    /// Вкладки, которым есть что показать без игры.
    /// </summary>
    /// <remarks>
    /// Рисуются подряд, а не списком выбора: встроенных вкладок вне игры нет, а общий
    /// номер выбранной вкладки остался от Play Mode и указывал бы в другой список.
    /// </remarks>
    private void DrawEditModeModuleTabs()
    {
        foreach (string error in moduleTabLoadErrors)
            EditorGUILayout.HelpBox(error, MessageType.Error);

        IPRDebugTab[] available = moduleTabs.Where(tab => tab.AvailableInEditMode).ToArray();

        if (available.Length == 0)
            return;

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (IPRDebugTab tab in available)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(SafeTitle(tab), EditorStyles.boldLabel);
            DrawModuleTab(tab);
        }

        EditorGUILayout.EndScrollView();
    }

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
