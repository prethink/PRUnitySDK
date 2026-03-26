using System;
using System.Collections.Generic;
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

    public void SetValue<T>(Enumeration category, Enumeration<T> enumeration, T value, bool isRequiredSave = true)
    {
        if (!this.saveData.Data.TryGetValue(category.Value, out var categoryDict))
        {
            categoryDict = new Dictionary<string, object>();
            this.saveData.Data[category.Value] = categoryDict;
        }

        categoryDict[enumeration.Value] = value;

        if (isRequiredSave)
            Save();
    }

    public T GetValue<T>(Enumeration category, Enumeration<T> enumeration, T defaultValue)
    {
        if (this.saveData.Data.TryGetValue(category.Value, out var categoryDict))
        {
            if (categoryDict.TryGetValue(enumeration.Value, out var value))
            {
                if (value is T typedValue)
                    return typedValue;

                return ConvertValue<T>(value, enumeration.ValueType);
            }
        }

        return defaultValue;
    }

    private T ConvertValue<T>(object value, Type targetType)
    {
        if (targetType == typeof(float))
            return (T)(object)Convert.ToSingle(value);

        if (targetType == typeof(int))
            return (T)(object)Convert.ToInt32(value);

        if (targetType == typeof(bool))
            return (T)(object)Convert.ToBoolean(value);

        if (targetType.IsEnum)
            return (T)Enum.Parse(targetType, value.ToString());

        throw new Exception($"Unsupported type {targetType}");
    }

    public GameStorageSettings GetSettings()
    {
        return PRUnitySDK.Settings.GameStorage;
    }

    #endregion
}
