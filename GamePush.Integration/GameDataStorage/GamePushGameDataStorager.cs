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
/// подтверждение облака — списанию покупок, — тот ждёт <see cref="SyncFinished"/>. Ответ площадки не говорит, на какой
/// вызов он пришёл, поэтому синхронизация идёт по одной: пока идёт текущая, новые запросы склеиваются в следующую,
/// и каждый ответ относится ровно к своей. Номер, с которым уйдут последние данные, — <see cref="DataSyncNumber"/>.
/// Все вызовы синхронизации игрока поэтому идут через <see cref="Sync"/>: вызов мимо очереди спутал бы ответы.
/// Ответа нет дольше <see cref="SyncTimeoutSeconds"/> — синхронизация считается сорвавшейся.
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
    /// Сколько секунд ждать ответа синхронизации, прежде чем считать её сорвавшейся.
    /// </summary>
    public const float SyncTimeoutSeconds = 60f;

    /// <summary>
    /// Номер синхронизации, которая отправит последние переданные данные.
    /// </summary>
    public static int DataSyncNumber { get; private set; }

    /// <summary>
    /// Сколько синхронизаций завершилось — успешно или нет; номер последней завершённой.
    /// </summary>
    public static int FinishedSyncs { get; private set; }

    /// <summary>
    /// Синхронизация завершилась: её номер по порядку и успех.
    /// </summary>
    public static event Action<int, bool> SyncFinished;

    private static int startedSyncs;
    private static bool syncInFlight;
    private static bool syncQueued;
    private static float syncStartedAt;

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
    /// Отправляет данные игрока на сервер: сразу, а если синхронизация уже идёт — следующей за ней.
    /// </summary>
    /// <returns>Номер синхронизации, которая отправит текущие данные.</returns>
    public static int Sync()
    {
        if (syncInFlight)
        {
            if (UnityEngine.Time.realtimeSinceStartup - syncStartedAt < SyncTimeoutSeconds)
            {
                // Переданное плагину уйдёт следующей синхронизацией: она отправит всё поле целиком.
                syncQueued = true;
                return DataSyncNumber = startedSyncs + 1;
            }

            // Ответа нет слишком долго — считаем синхронизацию сорвавшейся и не ждём её больше.
            Finish(false);
            if (syncInFlight)
                return DataSyncNumber = startedSyncs;
        }

        StartSync();
        return DataSyncNumber = startedSyncs;
    }

    private static void StartSync()
    {
        startedSyncs++;
        syncInFlight = true;
        syncStartedAt = UnityEngine.Time.realtimeSinceStartup;
        GP_Player.Sync();
    }

    private static void HandleSyncComplete() => Finish(true);

    private static void HandleSyncError() => Finish(false);

    /// <remarks>
    /// Ответ без идущей синхронизации — опоздавший, на уже списанную по тайм-ауту: не считается.
    /// </remarks>
    private static void Finish(bool success)
    {
        if (!syncInFlight)
            return;

        syncInFlight = false;
        FinishedSyncs = startedSyncs;
        SyncFinished?.Invoke(FinishedSyncs, success);

        if (syncQueued)
        {
            syncQueued = false;
            StartSync();
        }
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
