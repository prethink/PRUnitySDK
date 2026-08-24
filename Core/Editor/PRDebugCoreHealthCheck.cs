using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Базовые проверки состояния SDK, managers, entities, pools и MonoWindows.
/// </summary>
public sealed class PRDebugCoreHealthCheck : IPRDebugHealthCheck
{
    /// <inheritdoc />
    public IEnumerable<PRDebugProblem> Check()
    {
        var problems = new List<PRDebugProblem>();
        CheckSdk(problems);

        if (!PRUnitySDK.IsInitialized)
            return problems;

        CheckManagers(problems);
        CheckEntities(problems);
        CheckPools(problems);
        CheckWindows(problems);
        return problems;
    }

    private static void CheckSdk(List<PRDebugProblem> problems)
    {
        if (!PRUnitySDK.IsStartInitialize)
        {
            problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Warning, "SDK", "InitializationNotStarted",
                "PRUnitySDK initialization has not started.", sourceType: typeof(PRUnitySDK)));
            return;
        }

        if (!PRUnitySDK.IsInitialized)
        {
            problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Error, "SDK", "InitializationIncomplete",
                "PRUnitySDK initialization started but did not complete.", sourceType: typeof(PRUnitySDK)));
        }

        foreach (PRInitializationInfo entry in PRUnitySDK.InitializationHistory)
        {
            if (entry.ContractType == null || entry.ImplementationType == null)
            {
                problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Error, "Initialization", "MissingType",
                    $"Initialization entry '{entry.Name}' does not contain contract or implementation type.",
                    sourceType: entry.ImplementationType ?? entry.ContractType));
                continue;
            }

            if (!entry.ContractType.IsAssignableFrom(entry.ImplementationType))
            {
                problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Error, "Initialization", "ContractMismatch",
                    $"{entry.ImplementationType.FullName} does not implement {entry.ContractType.FullName}.",
                    sourceType: entry.ImplementationType));
            }
        }
    }

    private static void CheckManagers(List<PRDebugProblem> problems)
    {
        PRManagerContainer managers = PRUnitySDK.Managers;
        if (managers.ManagerContainer == null)
        {
            problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Error, "Managers", "MissingContainer",
                "PRManagerContainer runtime GameObject was not created.", sourceType: typeof(PRManagerContainer)));
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
        foreach (FieldInfo field in typeof(PRManagerContainer).GetFields(flags))
        {
            if (field.IsStatic || field.Name == nameof(PRManagerContainer.ManagerContainer))
                continue;

            object value = field.GetValue(managers);
            if (value == null || value is UnityEngine.Object unityObject && unityObject == null)
            {
                problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Warning, "Managers", "MissingInstance",
                    $"Manager member '{field.Name}' ({field.FieldType.FullName}) is null or destroyed.",
                    sourceType: field.FieldType));
            }
        }

        foreach (PropertyInfo property in typeof(PRManagerContainer).GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0)
                continue;

            try
            {
                object value = property.GetValue(managers);
                if (value == null || value is UnityEngine.Object unityObject && unityObject == null)
                {
                    problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Warning, "Managers", "MissingInstance",
                        $"Manager member '{property.Name}' ({property.PropertyType.FullName}) is null or destroyed.",
                        sourceType: property.PropertyType));
                }
            }
            catch (Exception exception)
            {
                problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Warning, "Managers", "MemberReadFailed",
                    $"Cannot read manager member '{property.Name}': {exception.GetBaseException().Message}",
                    sourceType: property.PropertyType));
            }
        }
    }

    private static void CheckEntities(List<PRDebugProblem> problems)
    {
        var entities = PRUnitySDK.Trackers.Entities.Entities
            .Where(entity => entity != null && !entity.IsNull())
            .ToArray();

        foreach (var group in entities.GroupBy(entity => entity.Id).Where(group => group.Count() > 1))
        {
            problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Error, "Entities", "DuplicateId",
                $"{group.Count()} registered entities use ID {group.Key}.",
                group.FirstOrDefault()?.gameObject));
        }

        foreach (IEntity entity in entities)
        {
            if (entity.OnScene && entity.InPool)
            {
                problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Error, "Entities", "ScenePoolConflict",
                    $"Entity {entity.Id} is marked both OnScene and InPool.", entity.gameObject,
                    entity.GetType()));
            }

            if (entity.InPool && entity is IPoolable poolable && poolable.PoolBehaviour == null)
            {
                problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Warning, "Entities", "MissingPoolBehaviour",
                    $"Entity {entity.Id} is marked InPool but has no PoolBehaviour.", entity.gameObject,
                    entity.GetType()));
            }
        }
    }

    private static void CheckPools(List<PRDebugProblem> problems)
    {
        ObjectPoolManager manager = PRUnitySDK.Managers.ObjectPool;
        if (manager == null)
            return;

        foreach (PoolSystemTableData pool in manager.GenerateReport())
        {
            long expectedTotal = (long)pool.ShowCount + pool.HideCount;
            if (pool.TotalCount != expectedTotal)
            {
                problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Error, "Pools", "CountMismatch",
                    $"Pool '{pool.Type}/{pool.Category}' reports Total={pool.TotalCount}, " +
                    $"but Active + Free = {expectedTotal}.", sourceType: typeof(ObjectPoolManager)));
            }
        }
    }

    private static void CheckWindows(List<PRDebugProblem> problems)
    {
        if (PRUnitySDK.Windows.Container == null || PRUnitySDK.Windows.SharedCanvas == null)
        {
            problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Error, "MonoWindows", "MissingContainer",
                "Windows container or shared canvas is missing.", sourceType: typeof(PRWindowsContainer)));
        }

        MonoWindowsTracker tracker = PRUnitySDK.Trackers.MonoWindows;
        MonoWindowBase[] windows = tracker.Elements.Where(window => window != null).ToArray();
        var duplicateKeys = windows.Where(window => window.Key != null)
            .GroupBy(window => window.Key)
            .Where(group => group.Count() > 1);

        foreach (var group in duplicateKeys)
        {
            problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Error, "MonoWindows", "DuplicateKey",
                $"{group.Count()} MonoWindows use key '{group.Key}'.", group.First().gameObject,
                group.First().GetType()));
        }

        MonoWindowBase[] visible = windows.Where(window => SafeVisible(window)).ToArray();
        if (visible.Length > 1)
        {
            problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Warning, "MonoWindows", "MultipleVisible",
                $"{visible.Length} MonoWindows are visible simultaneously.", visible[0].gameObject,
                visible[0].GetType()));
        }

        if (tracker.CurrentWindow != null && !SafeVisible(tracker.CurrentWindow))
        {
            problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Warning, "MonoWindows", "InvalidCurrentWindow",
                $"CurrentWindow '{tracker.CurrentWindow.name}' is not visible.", tracker.CurrentWindow.gameObject,
                tracker.CurrentWindow.GetType()));
        }
    }

    private static bool SafeVisible(MonoWindowBase window)
    {
        try
        {
            return window != null && window.IsVisible;
        }
        catch
        {
            return false;
        }
    }
}
