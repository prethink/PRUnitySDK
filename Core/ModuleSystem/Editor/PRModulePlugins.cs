using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

/// <summary>
/// Прячет и возвращает папки сторонних плагинов и ставит шаблон WebGL включённого модуля.
/// </summary>
/// <remarks>
/// Шаблон WebGL модуль может держать у себя (<see cref="PRModuleManifest.WebGLTemplateSource"/>): Unity берёт шаблоны
/// только из <c>Assets/WebGLTemplates</c>, поэтому «Применить» и сборка WebGL копируют исходник туда. Копия —
/// производная: правится исходник в модуле, копия при расхождении перезаписывается.
/// </remarks>
/// <remarks>
/// Плагин площадки попадает в сборку и без кода модуля: его <c>.jslib</c> и <c>.jspre</c> встраиваются
/// в страницу, автозапуск поднимает SDK площадки, обработчики сборки правят результат. Две площадки
/// в одном проекте поэтому мешают друг другу, даже когда код одной вырезан символом. Папку плагина
/// отключённого модуля переименовывают в «Имя~»: такие папки Unity не импортирует вовсе.
/// <para>
/// <c>.meta</c> самой папки на время прятанья лежит внутри неё (<see cref="StoredFolderMeta"/>):
/// рядом со спрятанной папкой Unity сочла бы его осиротевшим и удалила, и GUID папки сменился бы.
/// <c>.meta</c> файлов внутри едут вместе с папкой, их GUID сохраняются.
/// </para>
/// </remarks>
public static class PRModulePlugins
{
    /// <summary>
    /// Окончание имени, с которым Unity папку не видит.
    /// </summary>
    public const string HiddenSuffix = "~";

    /// <summary>
    /// Имя, под которым <c>.meta</c> папки хранится внутри спрятанной папки. Точка в начале — Unity его не видит
    /// и после возвращения, если перенести назад не удалось.
    /// </summary>
    private const string StoredFolderMeta = ".folder.meta";

    /// <summary>
    /// Где сейчас папка плагина модуля.
    /// </summary>
    public static PRPluginState GetState(PRModuleManifest manifest)
    {
        string folder = manifest.PluginFolder;

        if (folder.Length == 0)
            return PRPluginState.None;

        if (Directory.Exists(folder))
            return PRPluginState.Visible;

        return Directory.Exists(folder + HiddenSuffix) ? PRPluginState.Hidden : PRPluginState.Missing;
    }

    /// <summary>
    /// Папка плагина там, где должна быть при таком состоянии модуля.
    /// </summary>
    public static bool IsPluginInPlace(PRModuleManifest manifest, bool enabled)
    {
        if (!manifest.HidePluginWhenDisabled)
            return true;

        PRPluginState state = GetState(manifest);
        return enabled ? state != PRPluginState.Hidden : state != PRPluginState.Visible;
    }

    /// <summary>
    /// Шаблон WebGL, который нужен при таком наборе отключённых модулей; <c>null</c> — шаблон никому не важен.
    /// </summary>
    public static string FindTemplate(IEnumerable<PRModuleInfo> modules, ICollection<string> disabledIds)
    {
        return modules
            .Where(module => !disabledIds.Contains(module.Id) && module.Manifest.WebGLTemplate.Length > 0)
            .Select(module => module.Manifest.WebGLTemplate)
            .FirstOrDefault();
    }

    /// <summary>
    /// Что нужно поменять, чтобы плагины и шаблон соответствовали набору модулей. Пусто — всё на месте.
    /// </summary>
    public static List<string> DescribeChanges(IEnumerable<PRModuleInfo> modules, ICollection<string> disabledIds)
    {
        var list = modules.ToList();
        var changes = new List<string>();

        foreach (PRModuleInfo module in list)
        {
            bool enabled = !disabledIds.Contains(module.Id);

            if (IsPluginInPlace(module.Manifest, enabled))
                continue;

            changes.Add(enabled
                ? $"вернуть плагин {module.Manifest.PluginFolder}"
                : $"спрятать плагин {module.Manifest.PluginFolder} → {Path.GetFileName(module.Manifest.PluginFolder)}{HiddenSuffix}");
        }

        foreach (PRModuleInfo module in list.Where(module => !disabledIds.Contains(module.Id)))
            if (IsTemplateOutdated(module.Manifest))
                changes.Add($"обновить шаблон {module.Manifest.WebGLTemplateFolder} из {module.Manifest.WebGLTemplateSource}");

        string template = FindTemplate(list, disabledIds);
        if (template != null && PlayerSettings.WebGL.template != template)
            changes.Add($"шаблон WebGL {PlayerSettings.WebGL.template} → {template}");

        return changes;
    }

    /// <summary>
    /// Возвращает папки плагинов включённых модулей.
    /// </summary>
    /// <remarks>
    /// Вызывается до записи символов: модуль, чей плагин вернуть не удалось, включать нельзя — не соберётся.
    /// </remarks>
    /// <returns>Ошибки; пусто — всё вернулось.</returns>
    public static List<string> ShowEnabled(IEnumerable<PRModuleInfo> modules, ICollection<string> disabledIds)
    {
        var errors = new List<string>();

        foreach (PRModuleInfo module in modules.Where(module => !disabledIds.Contains(module.Id)))
        {
            if (!module.Manifest.HidePluginWhenDisabled || GetState(module.Manifest) != PRPluginState.Hidden)
                continue;

            string folder = module.Manifest.PluginFolder;
            if (TryMove(folder + HiddenSuffix, folder, out string error))
                RestoreFolderMeta(folder);
            else
                errors.Add($"{module.DisplayName}: {error}");
        }

        return errors;
    }

    /// <summary>
    /// Прячет папки плагинов отключённых модулей и ставит шаблон WebGL.
    /// </summary>
    /// <returns>Ошибки; пусто — всё спрятано.</returns>
    public static List<string> HideDisabledAndSetTemplate(IEnumerable<PRModuleInfo> modules, ICollection<string> disabledIds)
    {
        var list = modules.ToList();
        var errors = new List<string>();

        foreach (PRModuleInfo module in list.Where(module => disabledIds.Contains(module.Id)))
        {
            if (!module.Manifest.HidePluginWhenDisabled || GetState(module.Manifest) != PRPluginState.Visible)
                continue;

            string folder = module.Manifest.PluginFolder;
            if (TryMove(folder, folder + HiddenSuffix, out string error))
                StoreFolderMeta(folder);
            else
                errors.Add($"{module.DisplayName}: {error}");
        }

        errors.AddRange(SyncTemplates(list, disabledIds));

        string template = FindTemplate(list, disabledIds);
        if (template != null && PlayerSettings.WebGL.template != template)
            PlayerSettings.WebGL.template = template;

        return errors;
    }

    /// <summary>
    /// Копия шаблона в <c>Assets/WebGLTemplates</c> расходится с исходником модуля или её нет.
    /// </summary>
    public static bool IsTemplateOutdated(PRModuleManifest manifest)
    {
        string source = manifest.WebGLTemplateSource;
        string target = manifest.WebGLTemplateFolder;

        if (target.Length == 0 || !Directory.Exists(source))
            return false;

        if (!Directory.Exists(target))
            return true;

        List<string> sourceFiles = TemplateFiles(source);
        List<string> targetFiles = TemplateFiles(target);

        return !sourceFiles.SequenceEqual(targetFiles, StringComparer.OrdinalIgnoreCase)
               || sourceFiles.Any(file => !File.ReadAllBytes(Path.Combine(source, file)).SequenceEqual(File.ReadAllBytes(Path.Combine(target, file))));
    }

    /// <summary>
    /// Копирует исходники шаблонов включённых модулей в <c>Assets/WebGLTemplates</c>.
    /// </summary>
    /// <remarks>
    /// Лишние файлы копии удаляются вместе с их <c>.meta</c>; <c>.meta</c> остальных Unity сохраняет.
    /// </remarks>
    /// <returns>Ошибки; пусто — всё скопировано.</returns>
    public static List<string> SyncTemplates(IEnumerable<PRModuleInfo> modules, ICollection<string> disabledIds)
    {
        var errors = new List<string>();

        foreach (PRModuleInfo module in modules.Where(module => !disabledIds.Contains(module.Id)))
        {
            PRModuleManifest manifest = module.Manifest;
            if (!IsTemplateOutdated(manifest))
                continue;

            string source = manifest.WebGLTemplateSource;
            string target = manifest.WebGLTemplateFolder;

            try
            {
                List<string> sourceFiles = TemplateFiles(source);

                foreach (string file in sourceFiles)
                {
                    string to = Path.Combine(target, file);
                    Directory.CreateDirectory(Path.GetDirectoryName(to)!);
                    File.Copy(Path.Combine(source, file), to, true);
                }

                var keep = new HashSet<string>(sourceFiles, StringComparer.OrdinalIgnoreCase);
                foreach (string file in TemplateFiles(target).Where(file => !keep.Contains(file)))
                {
                    File.Delete(Path.Combine(target, file));
                    File.Delete(Path.Combine(target, file) + ".meta");
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                errors.Add($"{module.DisplayName}: шаблон {source} не скопирован в {target} ({exception.Message})");
            }
        }

        return errors;
    }

    /// <summary>
    /// Файлы шаблона без <c>.meta</c>, пути относительно папки, по порядку.
    /// </summary>
    private static List<string> TemplateFiles(string folder)
    {
        return Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(folder, path).Replace(Path.DirectorySeparatorChar, '/'))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool TryMove(string from, string to, out string error)
    {
        if (Directory.Exists(to))
        {
            error = $"папка {to} уже есть — уберите одну из двух копий плагина руками";
            return false;
        }

        try
        {
            Directory.Move(from, to);
            error = null;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Чаще всего файл плагина держит открытым сама Unity или IDE.
            error = $"не удалось переименовать {from} в {to} ({exception.Message}). Закройте программы, " +
                    "которые держат файлы плагина, и нажмите «Применить» ещё раз, или переименуйте папку руками";
            return false;
        }
    }

    private static void StoreFolderMeta(string folder)
    {
        string meta = folder + ".meta";
        string stored = Path.Combine(folder + HiddenSuffix, StoredFolderMeta);

        if (File.Exists(meta) && !File.Exists(stored))
            File.Move(meta, stored);
    }

    private static void RestoreFolderMeta(string folder)
    {
        string meta = folder + ".meta";
        string stored = Path.Combine(folder, StoredFolderMeta);

        if (File.Exists(stored) && !File.Exists(meta))
            File.Move(stored, meta);
    }
}
