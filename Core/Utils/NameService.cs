using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Загружает набор имён, выдаёт случайные уникальные значения и хранит их
/// резервирование в течение текущего запуска приложения.
/// </summary>
public class NameService : SingletonProviderBase<NameService>
{
    #region Поля

    /// <summary>
    /// Имя текстового ресурса со списком доступных имён.
    /// </summary>
    private const string ResourcePath = "Names"; 

    /// <summary>
    /// Очищенный исходный список имён, загруженный из Resources.
    /// </summary>
    private List<string> names;

    /// <summary>
    /// Имена, выданные потребителям и ещё не освобождённые.
    /// </summary>
    private readonly HashSet<string> reservedNames = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Следующий суффикс для имени, создаваемого после исчерпания исходного списка.
    /// </summary>
    private int generatedNameIndex = 1;

    /// <summary>
    /// Указывает, была ли уже выполнена попытка загрузки списка имён.
    /// </summary>
    private bool isInitialized = false;

    #endregion

    #region Инициализация

    /// <summary>
    /// Гарантирует, что данные загружены.
    /// </summary>
    private void EnsureInitialized()
    {
        if (isInitialized)
            return;

        TextAsset textAsset = Resources.Load<TextAsset>(PRUnitySDK.ResourcePaths.CorePath + "/" + ResourcePath);
        if (textAsset == null)
        {
            Debug.LogError($"[NameService] Не найден файл: {ResourcePath}");
            names = new List<string>();
        }
        else
        {
            names = textAsset.text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(name => name.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        isInitialized = true;
    }

    #endregion

    #region Публичные методы

    /// <summary>
    /// Возвращает копию исходного списка имён без учёта текущих резервирований.
    /// </summary>
    public List<string> GetAllNames()
    {
        EnsureInitialized();
        return names.ToList();
    }

    /// <summary>
    /// Возвращает и резервирует случайное свободное имя.
    /// Если исходный список исчерпан, создаёт уникальное имя с числовым суффиксом.
    /// </summary>
    public string GetRandomName()
    {
        EnsureInitialized();

        var availableNames = names.Where(name => !reservedNames.Contains(name)).ToList();
        if (availableNames.Count > 0)
        {
            string name = availableNames[UnityEngine.Random.Range(0, availableNames.Count)];
            reservedNames.Add(name);
            return name;
        }

        // Все исходные имена заняты. Сохраняем уникальность, добавляя стабильный
        // числовой суффикс, вместо случайного повторения уже выданного имени.
        string baseName = names.Count > 0
            ? names[UnityEngine.Random.Range(0, names.Count)]
            : "NoName";

        string generatedName;
        do
        {
            generatedName = $"{baseName} {generatedNameIndex++}";
        }
        while (reservedNames.Contains(generatedName));

        reservedNames.Add(generatedName);
        return generatedName;
    }

    /// <summary>
    /// Освобождает ранее выданное имя, чтобы его снова можно было использовать.
    /// </summary>
    /// <returns><see langword="true"/>, если имя находилось в резерве.</returns>
    public bool ReleaseName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && reservedNames.Remove(name.Trim());
    }

    #endregion
}
