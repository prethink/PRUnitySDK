using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Хранилище площадки, когда самой площадки нет (запуск в редакторе): ключи и строки одним файлом.
/// </summary>
/// <remarks>
/// В редакторе файл лежит в <c>UserSettings/PRSDKEditorSaves/&lt;площадка&gt;/storage.json</c> — его переключает
/// вместе с проектом SDK перенос сохранений (<c>PRSDKProjectSaves</c>), и у каждой игры своё сохранение.
/// <c>PlayerPrefs</c> для этого не годятся: они общие на весь Unity-проект, то есть на все игры сразу.
/// Вне редактора (сборка без площадки) файл — в <see cref="Application.persistentDataPath"/>.
/// <para>
/// Файл читается заново на каждой игре: проект меняют вне игры, и прежнее содержимое держать нельзя.
/// </para>
/// </remarks>
public sealed class PRSDKEditorStorage
{
    /// <summary>
    /// Папка сохранений площадок в редакторе, от корня Unity-проекта.
    /// </summary>
    public const string EditorFolder = "UserSettings/PRSDKEditorSaves";

    /// <summary>
    /// Имя файла хранилища.
    /// </summary>
    public const string FileName = "storage.json";

    private static readonly List<PRSDKEditorStorage> opened = new();

    private readonly string path;
    private Dictionary<string, string> values;

    /// <param name="platform">Имя площадки: папка файла.</param>
    public PRSDKEditorStorage(string platform)
    {
        path = PathOf(platform, FileName);
        opened.Add(this);
    }

    /// <summary>
    /// Путь файла площадки: в редакторе — в <see cref="EditorFolder"/>, в сборке — в данных приложения.
    /// </summary>
    public static string PathOf(string platform, string file) =>
        Application.isEditor
            ? $"{EditorFolder}/{platform}/{file}"
            : Path.Combine(Application.persistentDataPath, "PRSDKEditorSaves", platform, file);

    /// <summary>
    /// Значение по ключу; нет — пустая строка.
    /// </summary>
    public string Get(string key)
    {
        Load();
        return values.TryGetValue(key, out string value) ? value : string.Empty;
    }

    /// <summary>
    /// Запоминает значение; на диск — <see cref="Flush"/>.
    /// </summary>
    public void Set(string key, string value)
    {
        Load();
        values[key] = value ?? string.Empty;
    }

    /// <summary>
    /// Пишет всё на диск.
    /// </summary>
    public void Flush()
    {
        Load();
        var data = new Data();
        foreach (KeyValuePair<string, string> pair in values)
            data.entries.Add(new Entry { key = pair.Key, value = pair.Value });

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Debug.LogWarning($"[PRUnitySDK] Хранилище {path} не записано: {exception.Message}");
        }
    }

    private void Load()
    {
        if (values != null)
            return;

        values = new Dictionary<string, string>();
        try
        {
            if (!File.Exists(path))
                return;

            Data data = JsonUtility.FromJson<Data>(File.ReadAllText(path));
            if (data?.entries == null)
                return;

            foreach (Entry entry in data.entries)
                if (!string.IsNullOrEmpty(entry.key))
                    values[entry.key] = entry.value ?? string.Empty;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Debug.LogWarning($"[PRUnitySDK] Хранилище {path} не прочитано: {exception.Message}");
        }
    }

    // С выключенной перезагрузкой домена статика переживает выход из игры: файл перечитывается на каждой игре.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        foreach (PRSDKEditorStorage storage in opened)
            storage.values = null;
    }

    [Serializable]
    private sealed class Data
    {
        public List<Entry> entries = new();
    }

    [Serializable]
    private sealed class Entry
    {
        public string key;
        public string value;
    }
}
