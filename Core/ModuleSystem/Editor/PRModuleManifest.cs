using System.Collections.Generic;
using System.IO;
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
