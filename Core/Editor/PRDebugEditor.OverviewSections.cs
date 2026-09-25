using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class PRDebugEditor
{
    /// <summary>
    /// Разделы вкладки Overview, объявленные модулями через <see cref="IPRDebugOverviewSection"/>.
    /// </summary>
    private readonly List<IPRDebugOverviewSection> overviewSections = new();

    /// <summary>
    /// Ошибки разделов: создания и отрисовки.
    /// </summary>
    /// <remarks>
    /// По разделу, как и у вкладок модулей: сломанный раздел не должен прятать вкладку,
    /// ошибка показывается на его месте.
    /// </remarks>
    private readonly Dictionary<IPRDebugOverviewSection, string> overviewSectionErrors = new();

    private readonly List<string> overviewSectionLoadErrors = new();

    private void DiscoverOverviewSections()
    {
        overviewSections.Clear();
        overviewSectionErrors.Clear();
        overviewSectionLoadErrors.Clear();

        foreach (Type sectionType in TypeCache.GetTypesDerivedFrom<IPRDebugOverviewSection>())
        {
            if (sectionType.IsAbstract || sectionType.ContainsGenericParameters)
                continue;

            try
            {
                if (Activator.CreateInstance(sectionType) is IPRDebugOverviewSection section)
                    overviewSections.Add(section);
            }
            catch (Exception exception)
            {
                overviewSectionLoadErrors.Add($"Cannot create overview section '{sectionType.FullName}': {exception.GetBaseException().Message}");
            }
        }

        overviewSections.Sort((left, right) =>
        {
            int order = left.Order.CompareTo(right.Order);
            return order != 0 ? order : string.CompareOrdinal(left.GetType().FullName, right.GetType().FullName);
        });
    }

    private void DrawOverviewSections()
    {
        foreach (string error in overviewSectionLoadErrors)
            EditorGUILayout.HelpBox(error, MessageType.Error);

        foreach (IPRDebugOverviewSection section in overviewSections)
        {
            DrawSectionHeader(SafeTitle(section));

            if (overviewSectionErrors.TryGetValue(section, out string error))
                EditorGUILayout.HelpBox(error, MessageType.Error);

            try
            {
                // Ошибка не снимается удачной отрисовкой: подсказка, пропавшая между
                // проходами Layout и Repaint, сломала бы разметку всей вкладки.
                section.Draw(CreateModuleTabContext());
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception exception)
            {
                overviewSectionErrors[section] = $"Draw failed: {exception.GetType().Name}: {exception.Message}";
            }
        }
    }

    private static string SafeTitle(IPRDebugOverviewSection section)
    {
        try
        {
            return string.IsNullOrEmpty(section.Title) ? section.GetType().Name : section.Title;
        }
        catch (Exception)
        {
            return section.GetType().Name;
        }
    }
}
