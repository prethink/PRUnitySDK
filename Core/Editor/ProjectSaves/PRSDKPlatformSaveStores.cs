using System.Collections.Generic;
using System.IO;

// Файлы сохранений площадок в редакторе. Лежат в ядре, без кода плагинов и без символов отключения модулей:
// иначе отключённая площадка выпадала бы из переноса сохранений, и при смене проекта её файл оставался бы
// от прежней игры — а после включения новая игра стартовала бы с чужим прогрессом.

/// <summary>
/// Яндекс (YG2): <c>SavesEditorYG2.json</c> в папке плагина, в том числе спрятанной («PluginYourGames~»).
/// </summary>
public sealed class YandexEditorSaveStore : IPRSDKEditorSaveStore
{
    private const string Plugin = "Assets/PluginYourGames";

    /// <inheritdoc />
    public string Platform => "Yandex";

    /// <inheritdoc />
    public IEnumerable<string> Files
    {
        get
        {
            // Папку не создаём, как делает сам плагин: спрятанный плагин ожил бы пустой папкой рядом с «~».
            string folder = !Directory.Exists(Plugin) && Directory.Exists(Plugin + PRModulePlugins.HiddenSuffix)
                ? Plugin + PRModulePlugins.HiddenSuffix
                : Plugin;
            yield return $"{folder}/Editor/SavesEditorYG2.json";
        }
    }
}

/// <summary>
/// VK: хранилище моста в редакторе (<see cref="PRSDKEditorStorage"/>).
/// </summary>
public sealed class VKEditorSaveStore : IPRSDKEditorSaveStore
{
    /// <inheritdoc />
    public string Platform => "VK";

    /// <inheritdoc />
    public IEnumerable<string> Files
    {
        get { yield return $"{PRSDKEditorStorage.EditorFolder}/{Platform}/{PRSDKEditorStorage.FileName}"; }
    }
}

/// <summary>
/// Пикабу: хранилище моста в редакторе (<see cref="PRSDKEditorStorage"/>).
/// </summary>
public sealed class PikabuEditorSaveStore : IPRSDKEditorSaveStore
{
    /// <inheritdoc />
    public string Platform => "Pikabu";

    /// <inheritdoc />
    public IEnumerable<string> Files
    {
        get { yield return $"{PRSDKEditorStorage.EditorFolder}/{Platform}/{PRSDKEditorStorage.FileName}"; }
    }
}

/// <summary>
/// Steam: локальная копия сохранения в редакторе (<c>SteamGameDataStorager</c>).
/// </summary>
public sealed class SteamEditorSaveStore : IPRSDKEditorSaveStore
{
    /// <inheritdoc />
    public string Platform => "Steam";

    /// <inheritdoc />
    public IEnumerable<string> Files
    {
        get { yield return $"{PRSDKEditorStorage.EditorFolder}/{Platform}/{PRSDKEditorStorage.FileName}"; }
    }
}
