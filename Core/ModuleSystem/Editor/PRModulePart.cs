using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Дополнительная папка модуля. Лежит в корне папки, и всё, что в ней, принадлежит модулю из <see cref="Module"/>.
/// </summary>
/// <remarks>
/// Скрипты папки оборачиваются символом этого модуля, своего символа у части нет.
/// Так один модуль занимает папки в разных частях SDK и отключается целиком.
/// </remarks>
[CreateAssetMenu(fileName = "ModulePart", menuName = "PRUnitySDK/Module Part")]
public sealed class PRModulePart : ScriptableObject
{
    [Tooltip("Модуль, которому принадлежит эта папка.")]
    [SerializeField] private PRModuleManifest module;

    /// <summary>
    /// Модуль, которому принадлежит папка; <c>null</c>, если ссылка не задана или потеряна.
    /// </summary>
    public PRModuleManifest Module => module;

    /// <summary>
    /// Папка части относительно корня проекта, через прямой слеш.
    /// </summary>
    public string FolderPath => Path.GetDirectoryName(AssetDatabase.GetAssetPath(this))?.Replace('\\', '/') ?? string.Empty;
}
