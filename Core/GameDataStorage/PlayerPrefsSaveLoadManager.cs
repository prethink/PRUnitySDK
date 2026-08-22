using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Менеджер сохранения/загрузки данных через PlayerPrefs.
/// Функционально заменяет YandexSaveLoadManager, если внешнего SDK нет.
/// Сериализация идёт через Newtonsoft (PRJsonUtils), как и в Yandex-хранилище:
/// JsonUtility не умеет ни свойств (а всё в ProjectData - свойства), ни словарей,
/// поэтому раньше в PlayerPrefs уезжал фактически пустой объект.
/// </summary>
public class PlayerPrefsSaveLoadManager : IGameDataStorage
{
    #region Поля и свойства

    /// <summary>
    /// Ключ, под которым лежит весь PRSaveData целиком - аналог YG2.saves.RawData.
    /// </summary>
    private const string SaveDataKey = "PRSaveData";

    private PRSaveData saveData;

    #endregion

    #region IGameDataStorage

    /// <summary>
    /// Загружает данные игры из PlayerPrefs.
    /// </summary>
    public bool TryLoad()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        PRLog.WriteDebug(this, $"Try loading data use strategy {GetSettings().SaveStrategy}");

        saveData = new PRSaveData();
        bool result = false;

        var rawData = PlayerPrefs.GetString(SaveDataKey, string.Empty);

        if (string.IsNullOrEmpty(rawData))
            PRLog.WriteWarning(this, "Cannot loading. Raw data is empty.");
        else if (GetSettings().UseEncryption)
            result = LoadingJsonEncryptedData(rawData);
        else
            result = LoadingJsonData(rawData);

        stopwatch.Stop();
        readySignal.SetReady();
        PRLog.WriteDebug(this, $"Loading end. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");

        return result;
    }

    /// <summary>
    /// Сохраняет данные игры в PlayerPrefs.
    /// </summary>
    public void Save()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var rawData = GetSettings().UseEncryption
            ? PRJsonUtils.SerializeObjectWithEncryption(saveData)
            : PRJsonUtils.SerializeObject(saveData);

        PlayerPrefs.SetString(SaveDataKey, rawData);
        PlayerPrefs.Save();

        stopwatch.Stop();
        PRLog.WriteDebug(this, $"Save end. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
    }

    public GameSettings GetGameSettings()
    {
        return saveData?.GameSettings?.Clone() as GameSettings;
    }

    public ProjectData GetProjectData()
    {
        return saveData?.ProjectData?.Clone() as ProjectData;
    }

    public void UpdateGameSettings(GameSettings gameSettings, bool requiredSave = false)
    {
        saveData.GameSettings = gameSettings.Clone() as GameSettings;

        if (requiredSave)
            Save();
    }

    public void UpdateProjectData(ProjectData projectData, bool requiredSave = false)
    {
        saveData.ProjectData = projectData.Clone() as ProjectData;

        if (requiredSave)
            Save();
    }

    public GameStorageSettings GetSettings()
    {
        return PRUnitySDK.Settings.GameStorage;
    }

    #endregion

    #region Методы

    /// <summary>
    /// Загрузка при включённом шифровании. Стратегия Convert дополнительно пробует
    /// прочитать данные как обычный (нешифрованный) JSON - это позволяет подхватить
    /// сейв, записанный до включения шифрования.
    /// </summary>
    private bool LoadingJsonEncryptedData(string rawData)
    {
        PRLog.WriteDebug(this, "Use Encryption");

        PRSaveData result;

        if (GetSettings().EncryptionStrategy == EncryptionLoadingStrategy.Convert
            && PRJsonUtils.TryDeserializeObject(rawData, out result, false))
        {
            saveData = result;
            PRLog.WriteDebug(this, "Success loading data.");
            return true;
        }

        if (PRJsonUtils.TryDeserializeObjectDecrypt(rawData, out result))
        {
            saveData = result;
            PRLog.WriteDebug(this, "Success loading encryption data.");
            return true;
        }

        PRLog.WriteError(this, "Cannot loading data");
        return false;
    }

    /// <summary>
    /// Загрузка при выключенном шифровании: сначала обычный JSON, затем - попытка
    /// расшифровать, чтобы не потерять сейв, записанный до выключения шифрования.
    /// </summary>
    private bool LoadingJsonData(string rawData)
    {
        PRSaveData result;

        if (PRJsonUtils.TryDeserializeObject(rawData, out result, false))
        {
            saveData = result;
            PRLog.WriteDebug(this, "Success loading data.");
            return true;
        }

        if (PRJsonUtils.TryDeserializeObjectDecrypt(rawData, out result))
        {
            saveData = result;
            PRLog.WriteDebug(this, "Success loading encryption data.");
            return true;
        }

        PRLog.WriteError(this, "Cannot loading data");
        return false;
    }

    #endregion

    #region IReadySignalProvider

    protected readonly ReadySignal readySignal = new ReadySignal(typeof(PlayerPrefsSaveLoadManager));

    public IReadySignal ReadySignal => readySignal;

    #endregion
}
