using System;
using YG;

/// <summary>
/// Менеджер сохранения для работы с yandexSDK.
/// </summary>
public class YandexGameDataStorager : IGameDataStorage
{
    #region Поля и свойства

    private PRSaveData saveData;

    #endregion

    #region ISaveLoad

    public void Load()
    {
        PRLog.WriteDebug(this, $"Try loading data use strategy {GetSettings().SaveStrategy}");
        saveData = new PRSaveData();
        if (GetSettings().SaveStrategy == SaveStrategy.Serialize)
        {
            if(string.IsNullOrEmpty(YG2.saves.RawData))
                PRLog.WriteWarning(this, $"Cannot loading. Raw data is empty.");
            else if (GetSettings().UseEncryption)
                LoadingJsonEncryptedData();
            else
                LoadingJsonData();
        }
        else if (GetSettings().SaveStrategy == SaveStrategy.Class)
        {
            saveData = ((PRSaveData)YG2.saves?.PRSaveData?.Clone() ?? new PRSaveData());
        }
        else
        {
            throw new NotImplementedException();
        }
        PRLog.WriteDebug(this, $"Loading end.");
    }

    private void LoadingJsonEncryptedData()
    {
        PRLog.WriteDebug(this, $"Use Encryption");
        PRSaveData result;
        if (GetSettings().EncryptionStrategy == EncryptionLoadingStrategy.Convert)
        {
            if (PRJsonUtils.TryDeserializeObject<PRSaveData>(YG2.saves.RawData, out result, false))
            {
                saveData = result;
                PRLog.WriteDebug(this, $"Success loading data.");
            }
            else if(PRJsonUtils.TryDeserializeObjectDecrypt(YG2.saves.RawData, out result))
            {
                saveData = result;
                PRLog.WriteDebug(this, $"Cannot convert data");
                PRLog.WriteDebug(this, $"Success loading encryption data.");
            }
            else
            {
                PRLog.WriteError(this, $"Cannot loading data");
            }
        }
        else if (GetSettings().EncryptionStrategy == EncryptionLoadingStrategy.OnlyEncryption)
        {
            if (PRJsonUtils.TryDeserializeObjectDecrypt(YG2.saves.RawData, out result))
            {
                saveData = result;
                PRLog.WriteDebug(this, $"Success loading encryption data.");
            }
            else
            {
                PRLog.WriteError(this, $"Cannot loading data");
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private void LoadingJsonData()
    {
        PRSaveData result;
        if (PRJsonUtils.TryDeserializeObjectDecrypt(YG2.saves.RawData, out result))
        {
            saveData = result;
            PRLog.WriteDebug(this, $"Success loading encryption data.");
        }
        if (PRJsonUtils.TryDeserializeObject<PRSaveData>(YG2.saves.RawData, out result))
        {
            saveData = result;
            PRLog.WriteDebug(this, $"Cannot convert encryption data");
            PRLog.WriteDebug(this, $"Success loading data.");
        }
        else
        {
            PRLog.WriteError(this, $"Cannot loading data");
        }
    }

    public void Save()
    {
        PRLog.WriteDebug(this, $"Try save data use strategy {GetSettings().SaveStrategy}");
        if (GetSettings().SaveStrategy == SaveStrategy.Serialize)
        {
            if(GetSettings().UseEncryption)
                YG2.saves.RawData = PRJsonUtils.SerializeObjectWithEncryption(saveData);
            else
                YG2.saves.RawData = PRJsonUtils.SerializeObject(saveData);

            if (YG2.isSDKEnabled)
                YG2.SaveProgress();
        }
        else if(GetSettings().SaveStrategy == SaveStrategy.Class)
        {
            YG2.saves.PRSaveData = (PRSaveData)saveData.Clone();

            if (YG2.isSDKEnabled)
                YG2.SaveProgress();
        }
        else
        {
            throw new NotImplementedException();
        }

    }

    public GameSettings GetGameSettings()
    {
        return saveData.GameSettings.Clone() as GameSettings;
    }

    public ProjectData GetProjectData()
    {
        return saveData.ProjectData.Clone() as ProjectData;
    }

    public void UpdateGameSettings(GameSettings gameSettings, bool requiredSave = false)
    {
        this.saveData.GameSettings = gameSettings.Clone() as GameSettings;

        if (requiredSave)
            Save();
    }

    public void UpdateProjectData(ProjectData projectData, bool requiredSave = false)
    {
        this.saveData.ProjectData = projectData.Clone() as ProjectData;

        if(requiredSave)
            Save();
    }

    public GameStorageSettings GetSettings()
    {
        return PRUnitySDK.Settings.GameStorageSettings;
    }

    #endregion
}
