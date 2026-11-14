#if !PRSDK_DISABLE_STEAM
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
using System;
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
/// Одна из копий не читается — берётся вторая, и следующая запись заменит испорченную. Не читаются обе (или
/// единственная) — запись до конца запуска выключена (<see cref="GameDataStorageBase.IsWriteBlocked"/>).
/// </para>
/// <para>
/// Стратегия сохранения из настроек здесь не важна: данные всегда идут строкой, как при <see cref="SaveStrategy.Serialize"/>.
/// </para>
/// </remarks>
public class SteamGameDataStorager : GameDataStorageBase
{
    private static string FileName => PRUnitySDK.Settings.Steam.SaveFileName;

    // В редакторе — свой файл у каждого проекта SDK: его переключает перенос сохранений (SteamEditorSaveStore).
    // persistentDataPath общий на все игры Unity-проекта.
    private static string LocalPath => Application.isEditor
        ? PRSDKEditorStorage.PathOf("Steam", PRSDKEditorStorage.FileName)
        : Path.Combine(Application.persistentDataPath, FileName);

    // Облако App ID 480 (Spacewar) общее на все игры: в редакторе оно подмешало бы прогресс другой игры.
    private static bool UseCloud => SteamPlatform.IsReady &&
                                    !(Application.isEditor && PRUnitySDK.Settings.Steam.AppId == SteamSettings.SpacewarAppId);

    /// <remarks>
    /// Строкой здесь не обойтись: копий две, и выбирать между ними нужно по дате внутри сохранения.
    /// </remarks>
    protected override bool ReadSave(out PRSaveData data, out bool unreadable)
    {
        bool cloudLoaded = SaveDataVersioning.TryReadRaw(ReadCloud(), out PRSaveData cloud, out bool cloudUnreadable);
        bool localLoaded = SaveDataVersioning.TryReadRaw(ReadLocal(), out PRSaveData local, out bool localUnreadable);

        if (cloudLoaded && localLoaded)
        {
            data = cloud.UpdateDate >= local.UpdateDate ? cloud : local;
            unreadable = false;
            return true;
        }

        if (cloudLoaded || localLoaded)
        {
            if (cloudUnreadable || localUnreadable)
                PRLog.WriteWarning(this, $"Копия сохранения {(cloudUnreadable ? "в Steam Cloud" : "на диске")} не читается: взята вторая.");

            data = cloudLoaded ? cloud : local;
            unreadable = false;
            return true;
        }

        // Ни одного сохранения: TryReadRaw всё равно отдаёт новое пустое.
        data = cloud ?? local;
        unreadable = cloudUnreadable || localUnreadable;
        return false;
    }

    /// <inheritdoc />
    protected override string ReadRaw()
    {
        string cloud = ReadCloud();
        return string.IsNullOrEmpty(cloud) ? ReadLocal() : cloud;
    }

    /// <returns><see langword="true"/>, если сохранение легло хотя бы в одно место.</returns>
    protected override bool WriteRaw(string raw)
    {
        bool written = false;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LocalPath)!);
            File.WriteAllText(LocalPath, raw, Encoding.UTF8);
            written = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            PRLog.WriteWarning(this, $"Сохранение не записано на диск: {exception.Message}");
        }

        if (UseCloud)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(raw);
            if (SteamRemoteStorage.FileWrite(FileName, bytes, bytes.Length))
                written = true;
            else
                PRLog.WriteWarning(this, "Steam Cloud не принял сохранение: нет квоты в Steamworks или облако выключено.");
        }

        return written;
    }

    private static string ReadCloud()
    {
        if (!UseCloud || !SteamRemoteStorage.FileExists(FileName))
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
}
#endif
#endif
