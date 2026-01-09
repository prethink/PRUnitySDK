using System;
using UnityEngine;

/// <summary>
/// Менеджер сохранения/загрузки данных через PlayerPrefs.
/// Функционально заменяет YandexSaveLoadManager, если внешнего SDK нет.
/// </summary>
public class PlayerPrefsSaveLoadManager : IGameDataStorage
{
    private const string GameSettingsKey = "GameSettings";
    private const string ProjectDataKey = "ProjectData";

    private PRSaveData saveData;

    public event Action Ready;

    /// <summary>
    /// Загружает данные игры из PlayerPrefs.
    /// </summary>
    public void Load()
    {
        saveData = new PRSaveData();

        // Загружаем GameSettings
        if (PlayerPrefs.HasKey(GameSettingsKey))
        {
            string json = PlayerPrefs.GetString(GameSettingsKey);
            try
            {
                saveData.GameSettings = JsonUtility.FromJson<GameSettings>(json);
                PRLog.WriteDebug(this, "GameSettings loaded from PlayerPrefs.");
            }
            catch
            {
                PRLog.WriteWarning(this, "Failed to deserialize GameSettings from PlayerPrefs.");
            }
        }

        // Загружаем ProjectData
        if (PlayerPrefs.HasKey(ProjectDataKey))
        {
            string json = PlayerPrefs.GetString(ProjectDataKey);
            try
            {
                saveData.ProjectData = JsonUtility.FromJson<ProjectData>(json);
                PRLog.WriteDebug(this, "ProjectData loaded from PlayerPrefs.");
            }
            catch
            {
                PRLog.WriteWarning(this, "Failed to deserialize ProjectData from PlayerPrefs.");
            }
        }

        Ready?.Invoke();
    }

    /// <summary>
    /// Сохраняет данные игры в PlayerPrefs.
    /// </summary>
    public void Save()
    {
        if (saveData.GameSettings != null)
        {
            string json = JsonUtility.ToJson(saveData.GameSettings);
            PlayerPrefs.SetString(GameSettingsKey, json);
        }

        if (saveData.ProjectData != null)
        {
            string json = JsonUtility.ToJson(saveData.ProjectData);
            PlayerPrefs.SetString(ProjectDataKey, json);
        }

        PlayerPrefs.Save();
        PRLog.WriteDebug(this, "Data saved to PlayerPrefs.");
    }

    public GameSettings GetGameSettings()
    {
        return saveData.GameSettings?.Clone() as GameSettings;
    }

    public ProjectData GetProjectData()
    {
        return saveData.ProjectData?.Clone() as ProjectData;
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
}
