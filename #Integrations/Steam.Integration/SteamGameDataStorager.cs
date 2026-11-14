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
/// Файл на диске есть, но не открылся (занят, нет прав), или файл в облаке есть, но Steam его не отдал, — это не
/// «сохранения нет»: что в нём, неизвестно, оно может быть новее второй копии. Запись в таком запуске выключена,
/// игра идёт со второй копией, если она есть.
/// На диск сохранение пишется через временный файл: оборванная запись не портит прежнее.
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
    /// Сохранение лежит и на диске: без Steam игра читает и пишет его там.
    /// </remarks>
    protected override bool NeedsPlatform => false;

    /// <remarks>
    /// Строкой здесь не обойтись: копий две, и выбирать между ними нужно по дате внутри сохранения.
    /// </remarks>
    protected override bool ReadSave(out PRSaveData data, out bool unreadable)
    {
        bool cloudLoaded = SaveDataVersioning.TryReadRaw(ReadCloud(out bool cloudFailed), out PRSaveData cloud, out bool cloudUnreadable);

        // Файл в облаке есть, но Steam его не отдал: что в нём, неизвестно, и запись этого запуска могла бы заменить более новое.
        if (cloudFailed)
            MarkUnavailable();

        bool localLoaded = SaveDataVersioning.TryReadRaw(ReadLocal(out bool localFailed), out PRSaveData local, out bool localUnreadable);

        // Файл не открылся: его содержимое неизвестно, и запись этого запуска могла бы заменить более новое.
        if (localFailed)
            MarkUnavailable();

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
        string cloud = ReadCloud(out _);
        return string.IsNullOrEmpty(cloud) ? ReadLocal(out _) : cloud;
    }

    /// <returns><see langword="true"/>, если сохранение легло хотя бы в одно место.</returns>
    protected override bool WriteRaw(string raw)
    {
        bool written = false;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LocalPath)!);

            // Через временный файл: оборванная запись (выключили питание, закрыли игру) не портит прежнее сохранение.
            string temporary = LocalPath + ".tmp";
            File.WriteAllText(temporary, raw, Encoding.UTF8);

            if (File.Exists(LocalPath))
                File.Replace(temporary, LocalPath, null);
            else
                File.Move(temporary, LocalPath);

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

    /// <param name="failed">Файл в облаке есть и не пуст, но прочитать его не удалось: это не то же, что «файла нет».</param>
    private static string ReadCloud(out bool failed)
    {
        failed = false;

        if (!UseCloud || !SteamRemoteStorage.FileExists(FileName))
            return string.Empty;

        // Пустой файл прогресса не содержит: беречь в нём нечего.
        int size = SteamRemoteStorage.GetFileSize(FileName);
        if (size <= 0)
            return string.Empty;

        var bytes = new byte[size];
        int read = SteamRemoteStorage.FileRead(FileName, bytes, size);

        // FileRead отдаёт число прочитанных байт; ноль или меньше размера — чтение не удалось.
        if (read != size)
        {
            failed = true;
            PRLog.WriteError(typeof(SteamGameDataStorager), $"Сохранение не прочитано из Steam Cloud: получено {read} из {size} байт.");
            return string.Empty;
        }

        return Encoding.UTF8.GetString(bytes, 0, read);
    }

    /// <param name="failed">Файл есть, но прочитать его не удалось: это не то же, что «файла нет».</param>
    private static string ReadLocal(out bool failed)
    {
        failed = false;

        try
        {
            return File.Exists(LocalPath) ? File.ReadAllText(LocalPath, Encoding.UTF8) : string.Empty;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            failed = true;
            PRLog.WriteError(typeof(SteamGameDataStorager), $"Сохранение не прочитано с диска: {exception.Message}");
            return string.Empty;
        }
    }
}
#endif
#endif
