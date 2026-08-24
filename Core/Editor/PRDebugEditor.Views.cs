using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class PRDebugEditor
{
    private void DrawOverview()
    {
        DrawSectionHeader("Runtime");
        DrawKeyValue("SDK initialized", PRUnitySDK.IsInitialized);
        DrawKeyValue("Editor paused", EditorApplication.isPaused);
        DrawKeyValue("Last snapshot", lastRefreshUtc == default ? "-" : lastRefreshUtc.ToLocalTime().ToString("T"));

        DrawSectionHeader("Pause");
        using (new EditorGUI.DisabledScope(true))
            DrawToggleGrid(("Project", pause.Project), ("Logic", pause.Logic), ("Focus", pause.Focus),
                ("Music", pause.Music), ("Tutorial", pause.Tutorial), ("Cutscene", pause.Cutscene));

        DrawSectionHeader("Summary");
        DrawCards(("Players", players.Count), ("Humans", humanCount), ("AI", aiCount),
            ("Initialized", initializationEntries.Count), ("Entities", entityTotal), ("On scene", entityOnScene),
            ("In pool", entityInPool), ("Pools", pools.Count));
    }

    private void DrawInitialization()
    {
        DrawSectionHeader("PRUnitySDK initialization");
        DrawKeyValue("Tracked total", $"{initializationTotalMilliseconds:F2} ms");

        DrawInitializationTable(PRInitializationCategory.Module, "Modules");
        DrawInitializationTable(PRInitializationCategory.Manager, "Managers");
        DrawInitializationTable(PRInitializationCategory.Singleton, "Singletons");
        DrawInitializationTable(PRInitializationCategory.Factory, "Factories");
        DrawInitializationTable(PRInitializationCategory.MonoWindow, "MonoWindows");
        DrawInitializationTable(PRInitializationCategory.Notifier, "Notifiers");
        DrawInitializationTable(PRInitializationCategory.Type, "Other initialized types");
    }

    private void DrawInitializationTable(PRInitializationCategory category, string title)
    {
        int totalCount = 0;
        int visibleCount = 0;
        double totalMilliseconds = 0d;

        foreach (var row in initializationEntries)
        {
            if (row.Category != category)
                continue;

            totalCount++;
            totalMilliseconds += row.DurationMilliseconds;
        }

        if (totalCount == 0)
            return;

        DrawSectionHeader($"{title} ({totalCount}) — {totalMilliseconds:F2} ms");
        DrawFixedRow(true, ("Name", 140), ("Contract", 205), ("Implementation", 205),
            ("Time", 75), ("Source", 60));

        foreach (var row in initializationEntries)
        {
            if (row.Category != category)
                continue;

            if (!MatchesSearch(row.Category, row.Name, row.ContractType, row.ImplementationType))
                continue;

            visibleCount++;
            EditorGUILayout.BeginHorizontal();
            Label(row.Name, 140);
            Label(row.ContractType, 205);
            Label(row.ImplementationType, 205);
            Label($"{row.DurationMilliseconds:F2} ms", 75);
            DrawScriptButton(row.ImplementationTypeReference, row.ContractTypeReference);
            EditorGUILayout.EndHorizontal();
        }

        DrawEmpty(visibleCount, $"No {title.ToLowerInvariant()} match the current search.");
    }

    private void DrawPlayers()
    {
        DrawSectionHeader("Players");
        DrawFixedRow(true, ("Player ID", 70), ("Entity ID", 70), ("Type", 65), ("Name", 140),
            ("Team", 90), ("Ready", 55), ("Points", 65), ("K/D", 55), ("Object", 60));

        int count = 0;
        foreach (var row in players)
        {
            if (!MatchesSearch(row.PlayerId, row.EntityId, row.Type, row.Name, row.Team))
                continue;

            count++;
            EditorGUILayout.BeginHorizontal();
            Label(row.PlayerId, 70); Label(row.EntityId, 70); Label(row.Type, 65); Label(row.Name, 140);
            Label(row.Team, 90); Label(row.Ready?.ToString() ?? "-", 55); Label(row.Points, 65);
            Label($"{row.Kills}/{row.Deaths}", 55); DrawObjectButton(row.GameObject);
            EditorGUILayout.EndHorizontal();
        }
        DrawEmpty(count, "No players match the current search.");
    }

    private void DrawEntities()
    {
        DrawSectionHeader("Entities by type");
        DrawFixedRow(true, ("Icon", 56), ("Type", 174), ("Registered", 90), ("On scene", 80),
            ("Hidden", 70), ("In pool", 70));
        int count = 0;
        foreach (var row in entities)
        {
            if (!MatchesSearch(row.Type)) continue;
            count++;
            EditorGUILayout.BeginHorizontal(GUILayout.Height(40f));
            DrawIcon(row.Icon, 56f, 36f);
            Label(row.Type, 174); Label(row.Registered, 90); Label(row.OnScene, 80);
            Label(row.Hidden, 70); Label(row.InPool, 70);
            EditorGUILayout.EndHorizontal();
        }
        DrawEmpty(count, "No entity types match the current search.");
    }

    private void DrawPools()
    {
        DrawSectionHeader("Object pools");
        DrawFixedRow(true, ("Type", 170), ("Category", 210), ("Total", 65),
            ("Active", 65), ("Free", 65), ("Usage", 70));
        int count = 0;
        foreach (var row in pools)
        {
            if (!MatchesSearch(row.Type, row.Category)) continue;
            count++;
            float usage = row.TotalCount > 0 ? row.ShowCount / (float)row.TotalCount : 0f;
            DrawFixedRow(false, (row.Type ?? "-", 170), (row.Category ?? "-", 210),
                (row.TotalCount.ToString(), 65), (row.ShowCount.ToString(), 65),
                (row.HideCount.ToString(), 65), ($"{usage:P0}", 70));
        }
        DrawEmpty(count, "No pools match the current search.");
    }

    private void DrawFlags()
    {
        DrawSectionHeader("Flag resolvers");
        int count = 0;
        foreach (var resolver in flagResolvers)
        {
            bool resolverMatch = MatchesSearch(resolver.Name);
            if (!resolverMatch && !resolver.Flags.Any(flag => MatchesSearch(flag.Key?.Value)) && !string.IsNullOrWhiteSpace(search))
                continue;

            count++;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{resolver.Name} ({resolver.Flags.Count})", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            DrawObjectButton(resolver.Owner);
            EditorGUILayout.EndHorizontal();

            foreach (var flag in resolver.Flags)
            {
                if (!resolverMatch && !MatchesSearch(flag.Key?.Value)) continue;
                Color old = GUI.color;
                GUI.color = DecisionColor(flag.Decision);
                EditorGUILayout.LabelField($"{flag.Key}: {flag.Decision}", EditorStyles.miniBoldLabel);
                GUI.color = old;

                foreach (var influence in flag.Influences)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(16f);
                    Label(influence.IsFrame ? "Frame" : "Persistent", 70);
                    Label(influence.Decision, 55);
                    EditorGUILayout.LabelField(SourceName(influence.Source));
                    DrawObjectButton(influence.Source as Object);
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
        }
        DrawEmpty(count, "No flag resolvers match the current search.");
    }
}
