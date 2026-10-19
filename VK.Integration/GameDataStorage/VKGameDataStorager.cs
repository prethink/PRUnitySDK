#if !PRSDK_DISABLE_VK
using System;
using System.Diagnostics;

/// <summary>
/// Сохранение игры в VK Storage: весь <see cref="PRSaveData"/> одной строкой.
/// </summary>
/// <remarks>
/// Сохранение приходит вместе с запуском площадки (<see cref="VKInitResult.save"/>), поэтому чтение синхронное.
/// Запись отдаётся мосту (<see cref="VKBridge.StorageSave"/>), а он пишет её сам: сжатой, частями в одну из двух ячеек
/// по очереди и не чаще раза в 30 секунд — у VK Storage значение до 4096 символов и 1000 записей в час на игрока.
/// На скрытой странице несохранённое пишется сразу. Частые сохранения игры поэтому склеиваются: в облако уходит последнее.
/// <para>
/// Сохранение не прочиталось при запуске (сеть, повреждение обеих ячеек) — облако в этой сессии не пишется:
/// пустой прогресс затёр бы настоящий.
/// </para>
/// <para>
/// Стратегия сохранения из настроек здесь не важна: данные всегда идут строкой, как при <see cref="SaveStrategy.Serialize"/>.
/// </para>
/// </remarks>
public class VKGameDataStorager : IGameDataStorage, IGameDataStorageSaveInfo
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

    #region IGameDataStorage

    public bool TryLoad()
    {
        var stopwatch = Stopwatch.StartNew();

        if (!VKBridge.IsReady)
            PRLog.WriteWarning(this, "VK Bridge не запущен: сохранение не прочитано, игра начнётся с нуля.");

        bool loaded = SaveDataVersioning.TryReadRaw(VKBridge.Launch?.save ?? string.Empty, out saveData);

        stopwatch.Stop();
        readySignal.SetReady();
        PRLog.WriteDebug(this, $"Loading end. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
        return loaded;
    }

    public void Save()
    {
        var stopwatch = Stopwatch.StartNew();

        saveData.UpdateDate = PRUnitySDK.ServerTime.GetNow();

        if (!VKBridge.IsReady)
        {
            PRLog.WriteWarning(this, "VK Bridge не запущен: сохранение не отправлено.");
            return;
        }

        VKBridge.StorageSave(SaveDataVersioning.Serialize(saveData));

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

    #region IReadySignalProvider

    protected readonly ReadySignal readySignal = new ReadySignal(typeof(VKGameDataStorager));

    public IReadySignal ReadySignal => readySignal;

    #endregion
}
#endif
