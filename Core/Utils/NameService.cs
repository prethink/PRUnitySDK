using System;
using System.Collections.Generic;
using UnityEngine;

public class NameService : SingletonProviderBase<NameService>
{
    #region Поля

    private const string ResourcePath = "Names"; 
    private List<string> names;
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

        TextAsset textAsset = Resources.Load<TextAsset>(ResourcePath);
        if (textAsset == null)
        {
            Debug.LogError($"[NameService] Не найден файл: {ResourcePath}");
            names = new List<string>();
        }
        else
        {
            names = new List<string>(
                textAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            );
        }

        isInitialized = true;
    }

    #endregion

    #region Публичные методы

    /// <summary>
    /// Получить все имена.
    /// </summary>
    public List<string> GetAllNames()
    {
        EnsureInitialized();
        return names;
    }

    /// <summary>
    /// Получить случайное имя.
    /// </summary>
    public string GetRandomName()
    {
        EnsureInitialized();

        if (names.Count == 0)
            return "NoName";

        int index = UnityEngine.Random.Range(0, names.Count);
        return names[index];
    }

    #endregion
}
