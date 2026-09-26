using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/// <summary>
/// Проверка модулей: манифесты, обёртки файлов, отключённые обязательные модули
/// и включённые модули с отключёнными зависимостями.
/// </summary>
public sealed class PRModuleValidator : IProjectValidator
{
    /// <inheritdoc />
    public string Title => "Модули SDK";

    /// <inheritdoc />
    public IEnumerable<ProjectValidationIssue> Validate()
    {
        List<PRModuleInfo> modules = PRModuleCatalog.Load();
        HashSet<string> disabled = PRModuleDefines.ReadDisabledIds();

        foreach (IGrouping<string, PRModuleInfo> group in modules.GroupBy(module => module.Id).Where(group => group.Count() > 1))
        {
            yield return new ProjectValidationIssue(MessageType.Error,
                $"Идентификатор {group.Key} у нескольких модулей: {string.Join(", ", group.Select(module => module.Folder))}.",
                group.First().Manifest);
        }

        foreach (PRModuleInfo module in modules)
        {
            if (!PRModuleManifest.IsValidId(module.Id))
            {
                yield return new ProjectValidationIssue(MessageType.Error,
                    $"У модуля {module.Folder} недопустимый идентификатор «{module.Id}».", module.Manifest);
            }

            if (module.MissingDependencyCount > 0)
            {
                yield return new ProjectValidationIssue(MessageType.Warning,
                    $"У модуля {module.DisplayName} пустые ссылки на зависимости.", module.Manifest);
            }

            if (module.IsRequired && disabled.Contains(module.Id))
            {
                yield return new ProjectValidationIssue(MessageType.Error,
                    $"Обязательный модуль {module.DisplayName} отключён. Включите его в окне модулей.", module.Manifest);
            }

            // Окно модулей такого не допускает, но символы правят и руками в Player Settings,
            // и через слияние в git. Модуль тогда не собирается, а причина не видна.
            List<PRModuleInfo> disabledDependencies = disabled.Contains(module.Id)
                ? new List<PRModuleInfo>()
                : module.Dependencies.Where(dependency => disabled.Contains(dependency.Id)).ToList();

            if (disabledDependencies.Count > 0)
            {
                yield return new ProjectValidationIssue(MessageType.Error,
                    $"Модуль {module.DisplayName} включён, а его зависимости отключены: " +
                    $"{string.Join(", ", disabledDependencies.Select(dependency => dependency.DisplayName))}. " +
                    "Включите их или отключите модуль в окне модулей.", module.Manifest);
            }

            if (module.UnguardedScripts.Count > 0 && PRModuleManifest.IsValidId(module.Id))
            {
                List<string> scripts = module.UnguardedScripts.ToList();
                string id = module.Id;

                yield return new ProjectValidationIssue(MessageType.Warning,
                    $"У модуля {module.DisplayName} файлов без обёртки: {scripts.Count}. Первый: {scripts[0]}.",
                    AssetDatabase.LoadAssetAtPath<MonoScript>(scripts[0]),
                    "Обернуть",
                    () => PRModuleGuard.WrapAll(scripts, id));
            }
        }

        foreach (string script in FindStrayGuardedScripts(modules))
        {
            yield return new ProjectValidationIssue(MessageType.Warning,
                $"Файл обёрнут как модульный, но лежит вне модулей: {script}.",
                AssetDatabase.LoadAssetAtPath<MonoScript>(script),
                "Снять обёртку",
                () =>
                {
                    PRModuleGuard.Unwrap(script);
                    AssetDatabase.ImportAsset(script);
                });
        }
    }

    private static IEnumerable<string> FindStrayGuardedScripts(List<PRModuleInfo> modules)
    {
        var roots = new[] { PRModuleCatalog.PublicRoot, PRModuleCatalog.PrivateRoot };

        return roots
            .SelectMany(root => PRModuleCatalog.EnumerateScripts(root.TrimEnd('/')))
            .Where(script => PRModuleCatalog.FindOwner(modules, script) == null && PRModuleGuard.ReadGuardId(script) != null);
    }
}
