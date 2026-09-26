using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Менеджер сохранения/загрузки данных через PlayerPrefs.
/// Функционально заменяет YandexSaveLoadManager, если внешнего SDK нет.
/// Сериализация идёт через Newtonsoft (PRJsonUtils), как и в Yandex-хранилище:
/// JsonUtility не умеет ни свойств (а всё в ProjectData - свойства), ни словарей,
/// поэтому раньше в PlayerPrefs уезжал фактически пустой объект.
/// </summary>
public class PlayerPrefsSaveLoadManager : IGameDataStorage, IGameDataStorageSaveInfo
{
    #region Поля и свойства

    /// <summary>
    /// Ключ, под которым лежит весь PRSaveData целиком - аналог YG2.saves.RawData.
    /// </summary>
    private const string SaveDataKey = "PRSaveData";

    private PRSaveData saveData;

    public System.DateTime? CreationDate => saveData == null || saveData.SaveDate == default
        ? null
        : saveData.SaveDate;

    public System.DateTime? LastUpdateDate => saveData == null
        ? null
        : saveData.UpdateDate != default
            ? saveData.UpdateDate
            : CreationDate;

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

        bool loaded = SaveDataVersioning.TryReadRaw(PlayerPrefs.GetString(SaveDataKey, string.Empty), out saveData);

        stopwatch.Stop();
        readySignal.SetReady();
        PRLog.WriteDebug(this, $"Loading end. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");

        return loaded;
    }

    /// <summary>
    /// Сохраняет данные игры в PlayerPrefs.
    /// </summary>
    public void Save()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        saveData.UpdateDate = PRUnitySDK.ServerTime.GetNow();
        PlayerPrefs.SetString(SaveDataKey, SaveDataVersioning.Serialize(saveData));
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

    #region IReadySignalProvider

    protected readonly ReadySignal readySignal = new ReadySignal(typeof(PlayerPrefsSaveLoadManager));

    public IReadySignal ReadySignal => readySignal;

    #endregion
}
