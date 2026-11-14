using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// Находит включённые интеграции, которые попадут в сборку не своей платформы.
/// </summary>
/// <remarks>
/// Символы модулей одинаковы на всех платформах сборки, поэтому включённая веб-площадка остаётся включённой и в сборке
/// под PC или Android. Нативная интеграция перекрывает только те сервисы, которые реализует сама: остальные —
/// реклама, покупки, время, метрики — достались бы веб-площадке. В Steam-сборке это кнопки «за рекламу» без роликов
/// и магазин без каталога.
/// <para>
/// Интеграция, чей код собирается только под свою платформу (<see cref="PRModuleManifest.CodeOnlyOnPlatform"/>),
/// в чужой сборке безвредна: её там просто нет.
/// </para>
/// </remarks>
public static class PRModulePlatformCheck
{
    /// <summary>
    /// Включённые интеграции, которые не рассчитаны на эту платформу сборки, а их код в неё попадёт.
    /// </summary>
    /// <param name="manifests">Манифесты всех модулей.</param>
    /// <param name="disabledIds">Идентификаторы отключённых модулей.</param>
    /// <param name="target">Платформа сборки.</param>
    public static List<PRModuleManifest> FindForeign(IEnumerable<PRModuleManifest> manifests, ICollection<string> disabledIds,
        BuildTarget target)
    {
        return manifests
            .Where(manifest => manifest != null
                               && manifest.Platform.Length > 0
                               && !disabledIds.Contains(manifest.Id)
                               && !manifest.SupportsPlatform(target)
                               && !manifest.CodeOnlyOnPlatform)
            .ToList();
    }

    /// <summary>
    /// Что не так с интеграцией и что с этим делать — для окна проверки и для отказа сборки.
    /// </summary>
    public static string Describe(PRModuleManifest manifest, BuildTarget target)
    {
        return $"Интеграция {manifest.DisplayName} рассчитана на {manifest.Platform}, а платформа сборки — {target}. " +
               "Её сервисы (реклама, покупки, сохранение) попадут в сборку и займут место там, где интеграция этой " +
               "платформы своих не даёт. Отключите её в окне модулей (PRUnitySDK → Модули → Интеграции).";
    }
}

/// <summary>
/// Останавливает сборку, в которую попала бы интеграция другой платформы.
/// </summary>
/// <remarks>
/// Раньше, чем <see cref="PRModuleTemplateBuildSync"/>: при неверном наборе интеграций готовить шаблон незачем.
/// </remarks>
public sealed class PRModulePlatformBuildCheck : IPreprocessBuildWithReport
{
    public int callbackOrder => -110;

    public void OnPreprocessBuild(BuildReport report)
    {
        BuildTarget target = report.summary.platform;
        List<PRModuleManifest> foreign = PRModulePlatformCheck.FindForeign(
            PRModuleCatalog.Load().Select(module => module.Manifest), PRModuleDefines.ReadDisabledIds(), target);

        if (foreign.Count > 0)
            throw new BuildFailedException(string.Join("\n", foreign.Select(manifest => PRModulePlatformCheck.Describe(manifest, target))));
    }
}
