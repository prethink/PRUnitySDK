#if !PRSDK_DISABLE_GAMEPUSH
using System;
using System.Diagnostics;
using GamePush;

/// <summary>
/// Сохранение игры в облаке GamePush: весь <see cref="PRSaveData"/> одной строкой в поле игрока.
/// </summary>
/// <remarks>
/// Поле <see cref="SaveFieldKey"/> нужно завести в панели GamePush (раздел «Игроки», тип — строка):
/// значения полей, которых в панели нет, площадка не хранит. Всё сохранение игрока у GamePush
/// ограничено 1 МБ.
/// <para>
/// Стратегия сохранения из настроек здесь не важна: поле — строка, поэтому данные всегда идут
/// сериализованными, как при <see cref="SaveStrategy.Serialize"/>.
/// </para>
/// <para>
/// Запись уходит на сервер вызовом <c>GP_Player.Sync</c> на каждое сохранение. Как часто сохраняться,
/// решает <see cref="GameManager"/> со своим кулдауном; автосинхронизацию плагина не включаем,
/// чтобы не было второго расписания.
/// </para>
/// <para>
/// Событие сохранения игры приходит, когда данные отданы плагину, а не когда сервер их принял. Кому нужно
/// подтверждение облака — списанию покупок, — тот ждёт <see cref="SyncFinished"/>: синхронизации нумеруются
/// (<see cref="RequestedSyncs"/>), и номер в событии — сколько их завершилось. Площадка завершает их по порядку.
/// Все вызовы синхронизации игрока поэтому идут через <see cref="Sync"/>: чужой вызов мимо счётчика сдвинул бы номера.
/// </para>
/// </remarks>
public class GamePushGameDataStorager : IGameDataStorage, IGameDataStorageSaveInfo
{
    /// <summary>
    /// Поле игрока в панели GamePush, где лежит сохранение.
    /// </summary>
    public const string SaveFieldKey = "prsave";

    #region Поля и свойства

    private PRSaveData saveData;

    /// <summary>
    /// Хранилище запущенной игры; <c>null</c>, пока его не создали.
    /// </summary>
    public static GamePushGameDataStorager Current { get; private set; }

    /// <summary>
    /// Сколько синхронизаций с сервером запрошено.
    /// </summary>
    public static int RequestedSyncs { get; private set; }

    /// <summary>
    /// Сколько синхронизаций завершилось — успешно или нет.
    /// </summary>
    public static int FinishedSyncs { get; private set; }

    /// <summary>
    /// Синхронизация завершилась: её номер по порядку и успех.
    /// </summary>
    public static event Action<int, bool> SyncFinished;

    public DateTime? CreationDate => saveData == null || saveData.SaveDate == default
        ? null
        : saveData.SaveDate;

    public DateTime? LastUpdateDate => saveData == null
        ? null
        : saveData.UpdateDate != default
            ? saveData.UpdateDate
            : CreationDate;

    #endregion

    public GamePushGameDataStorager()
    {
        if (Current != null)
        {
            GP_Player.OnSyncComplete -= HandleSyncComplete;
            GP_Player.OnSyncError -= HandleSyncError;
        }

        Current = this;
        GP_Player.OnSyncComplete += HandleSyncComplete;
        GP_Player.OnSyncError += HandleSyncError;
    }

    /// <summary>
    /// Отправляет данные игрока на сервер и считает синхронизацию.
    /// </summary>
    public static void Sync()
    {
        RequestedSyncs++;
        GP_Player.Sync();
    }

    private static void HandleSyncComplete() => Finish(true);

    private static void HandleSyncError() => Finish(false);

    private static void Finish(bool success)
    {
        FinishedSyncs++;
        SyncFinished?.Invoke(FinishedSyncs, success);
    }

    #region IGameDataStorage

    /// <remarks>
    /// Игрок с сохранением приходит вместе с запуском площадки, поэтому читать можно сразу:
    /// загрузчик собирает SDK только после него. Площадка не поднялась — игра начинается с нуля.
    /// </remarks>
    public bool TryLoad()
    {
        var stopwatch = Stopwatch.StartNew();

        string raw = GP_Init.isReady ? GP_Player.GetString(SaveFieldKey) : string.Empty;

        if (!GP_Init.isReady)
            PRLog.WriteWarning(this, "GamePush не запущен: сохранение не прочитано, игра начнётся с нуля.");

        bool loaded = SaveDataVersioning.TryReadRaw(raw ?? string.Empty, out saveData);

        stopwatch.Stop();
        readySignal.SetReady();
        PRLog.WriteDebug(this, $"Loading end. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
        return loaded;
    }

    public void Save()
    {
        var stopwatch = Stopwatch.StartNew();

        saveData.UpdateDate = PRUnitySDK.ServerTime.GetNow();

        // Без площадки записывать некуда: обращение к ней в браузере упало бы в JavaScript.
        if (!GP_Init.isReady)
        {
            PRLog.WriteWarning(this, "GamePush не запущен: сохранение не отправлено.");
            return;
        }

        GP_Player.Set(SaveFieldKey, SaveDataVersioning.Serialize(saveData));
        Sync();

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

    protected readonly ReadySignal readySignal = new ReadySignal(typeof(GamePushGameDataStorager));

    public IReadySignal ReadySignal => readySignal;

    #endregion
}
#endif
