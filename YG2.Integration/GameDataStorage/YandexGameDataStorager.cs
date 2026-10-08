#if !PRSDK_DISABLE_YG2
using System;
using System.Diagnostics;
using YG;

/// <summary>
/// Менеджер сохранения для работы с yandexSDK.
/// </summary>
public class YandexGameDataStorager : IGameDataStorage, IGameDataStorageSaveInfo
{
    #region Поля и свойства

    private PRSaveData saveData;

    public DateTime? CreationDate => saveData == null || saveData.SaveDate == default
        ? null
        : saveData.SaveDate;

    public DateTime? LastUpdateDate => saveData == null
        ? null
        : saveData.UpdateDate != default
            ? saveData.UpdateDate
            : CreationDate;

    #endregion

    #region ISaveLoad

    public bool TryLoad()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        PRLog.WriteDebug(this, $"Try loading data use strategy {GetSettings().SaveStrategy}");
        bool loaded = GetSettings().SaveStrategy switch
        {
            SaveStrategy.Serialize => SaveDataVersioning.TryReadRaw(YG2.saves.RawData, out saveData),
            SaveStrategy.Class => SaveDataVersioning.TryRead(YG2.saves?.PRSaveData, out saveData),
            _ => throw new NotImplementedException()
        };
        stopwatch.Stop();
        readySignal.SetReady();
        PRLog.WriteDebug(this, $"Loading end. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
        return loaded;
    }

    public void Save()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        saveData.UpdateDate = PRUnitySDK.ServerTime.GetNow();
        PRLog.WriteDebug(this, $"Try save data use strategy {GetSettings().SaveStrategy}");
        if (GetSettings().SaveStrategy == SaveStrategy.Serialize)
        {
            YG2.saves.RawData = SaveDataVersioning.Serialize(saveData);

            if (YG2.isSDKEnabled)
                YG2.SaveProgress();
        }
        else if(GetSettings().SaveStrategy == SaveStrategy.Class)
        {
            SaveDataVersioning.Stamp(saveData);
            YG2.saves.PRSaveData = (PRSaveData)saveData.Clone();

            if (YG2.isSDKEnabled)
                YG2.SaveProgress();
        }
        else
        {
            throw new NotImplementedException();
        }

        stopwatch.Stop();
        PRLog.WriteDebug(this, $"Save end. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");

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
        return PRUnitySDK.Settings.GameStorage;
    }

    #endregion

    #region IReadySignalProvider

    protected readonly ReadySignal readySignal = new ReadySignal(typeof(YandexGameDataStorager));

    public IReadySignal ReadySignal => readySignal;

    #endregion
}
#endif
