using UnityEngine;

/// <summary>
/// Сохранение в PlayerPrefs: хранилище по умолчанию, когда площадка не подключена.
/// </summary>
/// <remarks>
/// Весь <see cref="PRSaveData"/> лежит одной строкой под ключом <see cref="SaveDataKey"/>. Сериализация идёт через
/// Newtonsoft (<see cref="SaveDataVersioning"/>): JsonUtility не умеет ни свойств (а всё в ProjectData - свойства),
/// ни словарей.
/// </remarks>
public class PlayerPrefsSaveLoadManager : GameDataStorageBase
{
    /// <summary>
    /// Ключ, под которым лежит весь PRSaveData целиком.
    /// </summary>
    public const string SaveDataKey = "PRSaveData";

    /// <inheritdoc />
    protected override string ReadRaw()
    {
        return PlayerPrefs.GetString(SaveDataKey, string.Empty);
    }

    /// <inheritdoc />
    protected override bool WriteRaw(string raw)
    {
        PlayerPrefs.SetString(SaveDataKey, raw);
        PlayerPrefs.Save();

        return true;
    }
}
