using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Описание модуля. Лежит в корне папки модуля, и всё, что в этой папке, принадлежит модулю.
/// </summary>
/// <remarks>
/// Отключённый модуль вырезается из сборки define-символом <see cref="DefineSymbol"/>:
/// каждый скрипт модуля обёрнут в <c>#if !PRSDK_DISABLE_&lt;Id&gt;</c>.
/// </remarks>
[CreateAssetMenu(fileName = "Module", menuName = "PRUnitySDK/Module Manifest")]
public sealed class PRModuleManifest : ScriptableObject
{
    /// <summary>
    /// Начало define-символа, которым модуль отключают.
    /// </summary>
    public const string DefinePrefix = "PRSDK_DISABLE_";

    private static readonly Regex IdPattern = new("^[A-Z][A-Z0-9_]*$");

    [Tooltip("Часть define-символа. Записан в обёртку каждого файла модуля, поэтому после раскатки его не меняют.")]
    [SerializeField] private string id;

    [Tooltip("Имя в окне модулей. Пусто — имя папки.")]
    [SerializeField] private string displayName;

    [SerializeField, TextArea(2, 6)] private string description;

    [SerializeField] private PRModuleGroup group;

    [Tooltip("Модуль нельзя отключить. Обёртки его файлам не нужны, а его зависимости тоже остаются включёнными.")]
    [SerializeField] private bool isRequired;

    [Tooltip("Модули, без которых этот не компилируется. Модуль в папке другого модуля зависит от него и без этого списка.")]
    [SerializeField] private List<PRModuleManifest> dependencies = new();

    [Header("Интеграция")]
    [Tooltip("Платформы сборки интеграции — имена BuildTarget через запятую, например WebGL или " +
             "StandaloneWindows64, StandaloneLinux64, StandaloneOSX. Окно модулей группирует по ним интеграции и предупреждает, " +
             "если включённая не подходит к активной платформе. Пусто — любая платформа.")]
    [SerializeField] private string platform;

    [Tooltip("Слот интеграции, например «Площадка». Из модулей с одним слотом включён не больше чем один. Пусто — модуль ни с кем не конкурирует.")]
    [SerializeField] private string slot;

    [Tooltip("Папка стороннего плагина, без которого модуль не компилируется, например Assets/PluginYourGames. Пусто — плагин не нужен.")]
    [SerializeField] private string pluginFolder;

    [Tooltip("Текстовый файл плагина со списком его установленных частей, по одной в строке. Окно модулей показывает его в карточке модуля.")]
    [SerializeField] private string pluginModulesFile;

    [Tooltip("Файлы настроек плагина, свои у каждого проекта SDK (игры), — пути от папки плагина, например " +
             "Resources/SettingsYG2.asset. При смене проекта на их место встаёт копия нового, правки сразу уходят в копию " +
             "активного (PRSDKProjectPlatformSettings). Пусто — настройки плагина общие для всех игр.")]
    [SerializeField] private List<string> projectSettingsFiles = new();

    [Tooltip("Прятать папку плагина, пока модуль отключён: окно модулей переименует её в «Имя~», и Unity перестанет её видеть. " +
             "Нужно плагинам, которые попадают в сборку и без кода модуля: jslib, jspre, автозапуск, обработчики сборки.")]
    [SerializeField] private bool hidePluginWhenDisabled;

    [Tooltip("Шаблон страницы WebGL, который окно модулей ставит, когда модуль включён, например PROJECT:YandexGames. Пусто — шаблон не меняется.")]
    [SerializeField] private string webGLTemplate;

    [Tooltip("Папка с исходником шаблона WebGL внутри модуля, от папки модуля, например WebGLTemplate~; путь с Assets/ — от проекта. " +
             "«~» в имени прячет её от Unity. «Применить» и сборка WebGL копируют её в Assets/WebGLTemplates под именем из " +
             "Web GL Template (PROJECT:Имя): Unity берёт шаблоны только оттуда. Пусто — шаблон лежит в Assets/WebGLTemplates сам.")]
    [SerializeField] private string webGLTemplateSource;

    /// <summary>
    /// Идентификатор модуля: заглавные латинские буквы, цифры и подчёркивание.
    /// </summary>
    public string Id => id;

    /// <summary>
    /// Имя модуля для окна.
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Path.GetFileName(FolderPath) : displayName;

    /// <summary>
    /// Описание модуля для окна.
    /// </summary>
    public string Description => description;

    /// <summary>
    /// Подгруппа в окне модулей.
    /// </summary>
    public PRModuleGroup Group => group;

    /// <summary>
    /// Модуль нельзя отключить.
    /// </summary>
    public bool IsRequired => isRequired;

    /// <summary>
    /// Явно указанные зависимости. Элементы бывают <c>null</c>, если ссылка потеряна.
    /// </summary>
    public IReadOnlyList<PRModuleManifest> Dependencies => dependencies;

    /// <summary>
    /// Слот взаимоисключения: из модулей с одним слотом включён не больше чем один. Пустая строка — слота нет.
    /// </summary>
    public string Slot => string.IsNullOrWhiteSpace(slot) ? string.Empty : slot.Trim();

    /// <summary>
    /// Платформы сборки интеграции (имена <see cref="BuildTarget"/> через запятую); пустая строка — любая.
    /// </summary>
    public string Platform => string.IsNullOrWhiteSpace(platform) ? string.Empty : platform.Trim();

    /// <summary>
    /// Интеграция подходит к платформе сборки.
    /// </summary>
    public bool SupportsPlatform(BuildTarget target) =>
        Platform.Length == 0 || Platform.Split(',').Any(name => string.Equals(name.Trim(), target.ToString(), System.StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Папка стороннего плагина, нужного модулю; пустая строка — плагин не нужен.
    /// </summary>
    public string PluginFolder => string.IsNullOrWhiteSpace(pluginFolder) ? string.Empty : pluginFolder.Trim().TrimEnd('/');

    /// <summary>
    /// Файл плагина со списком его установленных частей; пустая строка — списка нет.
    /// </summary>
    public string PluginModulesFile => string.IsNullOrWhiteSpace(pluginModulesFile) ? string.Empty : pluginModulesFile.Trim();

    /// <summary>
    /// Файлы настроек плагина, свои у каждого проекта SDK, — пути от папки плагина.
    /// </summary>
    public IReadOnlyList<string> ProjectSettingsFiles =>
        projectSettingsFiles == null
            ? System.Array.Empty<string>()
            : projectSettingsFiles.Where(file => !string.IsNullOrWhiteSpace(file)).Select(file => file.Trim().Replace('\\', '/').Trim('/')).ToList();

    /// <summary>
    /// Папку плагина прячут, пока модуль отключён, см. <see cref="PRModulePlugins"/>.
    /// </summary>
    public bool HidePluginWhenDisabled => hidePluginWhenDisabled && PluginFolder.Length > 0;

    /// <summary>
    /// Шаблон WebGL включённого модуля; пустая строка — шаблон модулю не важен.
    /// </summary>
    public string WebGLTemplate => string.IsNullOrWhiteSpace(webGLTemplate) ? string.Empty : webGLTemplate.Trim();

    /// <summary>
    /// Папка исходника шаблона WebGL от корня проекта; пустая строка — исходника в модуле нет.
    /// </summary>
    /// <remarks>
    /// В манифесте путь пишется от папки модуля: так он переживает перенос модуля. Путь с <c>Assets/</c> берётся как есть.
    /// </remarks>
    public string WebGLTemplateSource
    {
        get
        {
            if (string.IsNullOrWhiteSpace(webGLTemplateSource))
                return string.Empty;

            string source = webGLTemplateSource.Trim().Replace('\\', '/').TrimEnd('/');
            string folder = FolderPath;
            return source.StartsWith("Assets/", System.StringComparison.Ordinal) || folder.Length == 0 ? source : folder + "/" + source;
        }
    }

    /// <summary>
    /// Куда копируется исходник шаблона: <c>Assets/WebGLTemplates/Имя</c> для <c>PROJECT:Имя</c>; пустая строка —
    /// копировать некуда (нет исходника или шаблон не из проекта).
    /// </summary>
    public string WebGLTemplateFolder
    {
        get
        {
            const string projectPrefix = "PROJECT:";
            string template = WebGLTemplate;

            if (WebGLTemplateSource.Length == 0 || !template.StartsWith(projectPrefix, System.StringComparison.Ordinal))
                return string.Empty;

            return "Assets/WebGLTemplates/" + template.Substring(projectPrefix.Length);
        }
    }

    /// <summary>
    /// Define-символ, который вырезает модуль из сборки.
    /// </summary>
    public string DefineSymbol => DefinePrefix + id;

    /// <summary>
    /// Папка модуля относительно корня проекта, через прямой слеш.
    /// </summary>
    public string FolderPath => Path.GetDirectoryName(AssetDatabase.GetAssetPath(this))?.Replace('\\', '/') ?? string.Empty;

    /// <summary>
    /// Годится ли строка в идентификатор модуля.
    /// </summary>
    public static bool IsValidId(string value)
    {
        return !string.IsNullOrEmpty(value) && IdPattern.IsMatch(value);
    }

    /// <summary>
    /// Идентификатор из имени папки: <c>Waypoint System</c> → <c>WAYPOINT_SYSTEM</c>.
    /// </summary>
    public static string CreateId(string folderName)
    {
        var builder = new StringBuilder();

        foreach (char symbol in folderName ?? string.Empty)
        {
            if (symbol is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9')
                builder.Append(char.ToUpperInvariant(symbol));
            else if (builder.Length > 0 && builder[^1] != '_')
                builder.Append('_');
        }

        string result = builder.ToString().Trim('_');
        return result.Length > 0 && char.IsDigit(result[0]) ? "M_" + result : result;
    }

    /// <summary>
    /// Заполняет пустой идентификатор по имени папки.
    /// </summary>
    /// <returns><c>true</c>, если идентификатор изменился.</returns>
    public bool TryFillIdFromFolder()
    {
        if (!string.IsNullOrEmpty(id))
            return false;

        string folder = FolderPath;
        if (string.IsNullOrEmpty(folder))
            return false;

        id = CreateId(Path.GetFileName(folder));
        EditorUtility.SetDirty(this);
        return true;
    }

    private void OnValidate()
    {
        // При создании через меню путь появляется только после ввода имени,
        // поэтому идентификатор может заполниться при следующей загрузке.
        TryFillIdFromFolder();
    }
}
