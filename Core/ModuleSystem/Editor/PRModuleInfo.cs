using System.Collections.Generic;

/// <summary>
/// Модуль, найденный в проекте: манифест и то, что вычислено по папкам.
/// </summary>
public sealed class PRModuleInfo
{
    /// <summary>
    /// Манифест модуля.
    /// </summary>
    public PRModuleManifest Manifest { get; }

    /// <summary>
    /// Папка модуля относительно корня проекта.
    /// </summary>
    public string Folder { get; }

    /// <summary>
    /// Часть SDK, в которой лежит модуль.
    /// </summary>
    public PRModuleLayer Layer { get; }

    /// <summary>
    /// Модуль, в папке которого лежит этот; <c>null</c>, если такого нет.
    /// </summary>
    public PRModuleInfo Parent { get; internal set; }

    /// <summary>
    /// Скрипты модуля без тех, что принадлежат вложенным модулям.
    /// </summary>
    public List<string> Scripts { get; } = new();

    /// <summary>
    /// Скрипты без обёртки модуля или с чужой обёрткой.
    /// </summary>
    public List<string> UnguardedScripts { get; } = new();

    /// <summary>
    /// Модули, без которых этот не компилируется: из манифеста и родительский.
    /// </summary>
    public List<PRModuleInfo> Dependencies { get; } = new();

    /// <summary>
    /// Модули, которые зависят от этого напрямую.
    /// </summary>
    public List<PRModuleInfo> Dependents { get; } = new();

    /// <summary>
    /// Ссылок на зависимости в манифесте, которые никуда не ведут.
    /// </summary>
    public int MissingDependencyCount { get; internal set; }

    /// <summary>
    /// Идентификатор модуля.
    /// </summary>
    public string Id => Manifest.Id;

    /// <summary>
    /// Имя модуля для окна.
    /// </summary>
    public string DisplayName => Manifest.DisplayName;

    /// <summary>
    /// Модуль нельзя отключить.
    /// </summary>
    public bool IsRequired => Manifest.IsRequired;

    public PRModuleInfo(PRModuleManifest manifest, string folder, PRModuleLayer layer)
    {
        Manifest = manifest;
        Folder = folder;
        Layer = layer;
    }
}
