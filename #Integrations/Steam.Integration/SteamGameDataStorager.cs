#if !PRSDK_DISABLE_STEAM
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Steamworks;
using UnityEngine;

/// <summary>
/// Сохранение игры в Steam Cloud и на диске: весь <see cref="PRSaveData"/> одним файлом.
/// </summary>
/// <remarks>
/// Пишется в оба места: в Steam Cloud (<c>ISteamRemoteStorage</c>) и в <see cref="Application.persistentDataPath"/>.
/// Диск нужен, когда Steam не поднялся: прогресс не пропадёт и дождётся облака. При загрузке берётся более новое
/// из двух по дате изменения сохранения. Облаку нужна квота в Steamworks (Steam Cloud → Byte quota per user).
/// <para>
/// Стратегия сохранения из настроек здесь не важна: данные всегда идут строкой, как при <see cref="SaveStrategy.Serialize"/>.
/// </para>
/// </remarks>
public class SteamGameDataStorager : IGameDataStorage, IGameDataStorageSaveInfo
{
    private PRSaveData saveData;

    private static string FileName => PRUnitySDK.Settings.Steam.SaveFileName;

    private static string LocalPath => Path.Combine(Application.persistentDataPath, FileName);

    public DateTime? CreationDate => saveData == null || saveData.SaveDate == default
        ? null
        : saveData.SaveDate;

    public DateTime? LastUpdateDate => saveData == null
        ? null
        : saveData.UpdateDate != default
            ? saveData.UpdateDate
            : CreationDate;

    #region IGameDataStorage

    public bool TryLoad()
    {
        var stopwatch = Stopwatch.StartNew();

        bool cloudLoaded = SaveDataVersioning.TryReadRaw(ReadCloud(), out PRSaveData cloud);
        bool localLoaded = SaveDataVersioning.TryReadRaw(ReadLocal(), out PRSaveData local);

        bool loaded;
        if (cloudLoaded && localLoaded)
        {
            saveData = cloud.UpdateDate >= local.UpdateDate ? cloud : local;
            loaded = true;
        }
        else if (cloudLoaded || localLoaded)
        {
            saveData = cloudLoaded ? cloud : local;
            loaded = true;
        }
        else
        {
            // Ни одного сохранения: TryReadRaw всё равно отдаёт новое пустое.
            saveData = cloud ?? local;
            loaded = false;
        }

        stopwatch.Stop();
        readySignal.SetReady();
        PRLog.WriteDebug(this, $"Loading end. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
        return loaded;
    }

    public void Save()
    {
        var stopwatch = Stopwatch.StartNew();

        saveData.UpdateDate = PRUnitySDK.ServerTime.GetNow();
        string raw = SaveDataVersioning.Serialize(saveData);

        try
        {
            File.WriteAllText(LocalPath, raw, Encoding.UTF8);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            PRLog.WriteWarning(this, $"Сохранение не записано на диск: {exception.Message}");
        }

        if (SteamPlatform.IsReady)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(raw);
            if (!SteamRemoteStorage.FileWrite(FileName, bytes, bytes.Length))
                PRLog.WriteWarning(this, "Steam Cloud не принял сохранение: нет квоты в Steamworks или облако выключено.");
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

    #endregion

    private static string ReadCloud()
    {
        if (!SteamPlatform.IsReady || !SteamRemoteStorage.FileExists(FileName))
            return string.Empty;

        int size = SteamRemoteStorage.GetFileSize(FileName);
        if (size <= 0)
            return string.Empty;

        var bytes = new byte[size];
        int read = SteamRemoteStorage.FileRead(FileName, bytes, size);
        return read > 0 ? Encoding.UTF8.GetString(bytes, 0, read) : string.Empty;
    }

    private static string ReadLocal()
    {
        try
        {
            return File.Exists(LocalPath) ? File.ReadAllText(LocalPath, Encoding.UTF8) : string.Empty;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            PRLog.WriteWarning(typeof(SteamGameDataStorager), $"Сохранение не прочитано с диска: {exception.Message}");
            return string.Empty;
        }
    }

    #region IReadySignalProvider

    protected readonly ReadySignal readySignal = new ReadySignal(typeof(SteamGameDataStorager));

    public IReadySignal ReadySignal => readySignal;

    #endregion
}
#endif
#endif
