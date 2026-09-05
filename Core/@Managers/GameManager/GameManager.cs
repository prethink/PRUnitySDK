using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public partial class GameManager : MonoBehaviourSingletonBase<GameManager>, IReadySignalProvider
{
    #region Поля и свойства

    /// <summary>
    /// Игровые настройки.
    /// </summary>
    private GameSettings gameSettings { get; set; }

    /// <summary>
    /// Данные проекта.
    /// </summary>
    private ProjectData projectData { get; set; }

    /// <summary>
    /// Глобальные настройки сессии.
    /// </summary>
    public GlobalGameSettingsSession GameSettingsSession { get; private set; }

    private IGameDataStorage gameDataStorage { get; set; }

    private bool isInitialize;
    private bool isSaving;
    private long saveCooldownCounter;
    private SynchronizationContext synchronizationContext;
    private readonly object saveDiagnosticsLock = new();
    private int activeSaveOperationCount;
    private bool activeSaveOperationFailed;
    private GameSaveState saveState = GameSaveState.NotStarted;
    private DateTime? saveCreationTimeUtc;
    private DateTime? lastSaveTimeUtc;
    private bool hasLoadedSave;

    /// <summary>
    /// Состояние save-операций в текущей игровой сессии.
    /// </summary>
    public GameSaveState SaveState
    {
        get
        {
            lock (saveDiagnosticsLock)
                return saveState;
        }
    }

    /// <summary>
    /// UTC-время последнего сохранения. После успешной загрузки восстанавливается
    /// из storage; null означает, что дата ещё неизвестна.
    /// </summary>
    public DateTime? LastSaveTimeUtc
    {
        get
        {
            lock (saveDiagnosticsLock)
                return lastSaveTimeUtc;
        }
    }

    /// <summary>
    /// UTC-время создания текущего save или null, если storage не предоставляет метаданные.
    /// </summary>
    public DateTime? SaveCreationTimeUtc
    {
        get
        {
            lock (saveDiagnosticsLock)
                return saveCreationTimeUtc;
        }
    }

    /// <summary>
    /// True when the current session successfully loaded an existing save.
    /// </summary>
    public bool HasLoadedSave
    {
        get
        {
            lock (saveDiagnosticsLock)
                return hasLoadedSave;
        }
    }

    /// <summary>
    /// Whole seconds remaining before a regular save can start.
    /// </summary>
    public long SaveCooldownRemainingSeconds
    {
        get
        {
            long cooldownSeconds = GetStorageSettings().SaveCooldownSeconds;
            if (cooldownSeconds <= 0)
                return 0;

            long elapsedSeconds = PRTime.Instance.CurrentRealSecond - saveCooldownCounter;
            return Math.Max(0, cooldownSeconds - elapsedSeconds);
        }
    }

    #endregion

    #region MonoBehaviour

    private void Awake()
    {
        this.RunMethodHooks(MethodHookStage.PreAwake);

        this.InitializeGameManager();

        this.RunMethodHooks(MethodHookStage.PostAwake);
    }

    private void Start()
    {
        this.RunMethodHooks(MethodHookStage.PreStart);
        this.RunMethodHooks(MethodHookStage.PostStart);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        PRLog.WriteDebug(this, $"{nameof(OnApplicationPause)} pauseStatus - {pauseStatus}", new PRLogSettings() { LevelDebug = 9 });

        PRUnitySDK.PauseManager.SetProjectPaused(pauseStatus, this);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        PRLog.WriteDebug(this, $"{nameof(OnApplicationFocus)} pauseStatus - {hasFocus}", new PRLogSettings() { LevelDebug = 9 });

        var requiredPause = !hasFocus;
        PRUnitySDK.PauseManager.SetFocusPaused(requiredPause, this);
    }

    public void OnPageVisibilityChange(int isHiddenInt)
    {
        if (!PRUnitySDK.DeviceInfo.IsIOS())
            return;

        bool isHidden = isHiddenInt == 1;
        PRLog.WriteDebug(this, $"WebGL Visibility Changed. Hidden: {isHidden}", new PRLogSettings() { LevelDebug = 5 });

        // Скрытая страница — повод поставить паузу, поэтому isHidden передаётся как есть:
        // SetFocusPaused принимает признак «нужна пауза», как и в OnApplicationFocus.
        PRUnitySDK.PauseManager.SetFocusPaused(isHidden, this);
    }

    #endregion

    #region Методы

    public void InitializeGameManager()
    {
        if (isInitialize)
            return;

        synchronizationContext = SynchronizationContext.Current;

        gameDataStorage = PRUnitySDK.GameDataStorage;
        bool loadedExistingSave = gameDataStorage.TryLoad();
        CaptureLoadedSaveInfo(loadedExistingSave);
        bool isRequiredFirstInitialize = !loadedExistingSave;
        gameDataStorage.ReadySignal.SubscribeOnReady(() =>
        {
            LoadingData();

            if (isRequiredFirstInitialize)
                InitializeDefaultData();

            AutoSaveHandler();

            GameplayEvents.RaiseGameReady();
            readySignal.SetReady();
            isInitialize = true;
        });
    }

    private void InitializeDefaultData()
    {
        var defaultSettings = PRUnitySDK.Settings.Default;

        gameSettings.Sensitivity = defaultSettings.Sensitivity;
        gameSettings.InvertHorizontalInput = defaultSettings.InvertHorizontalInput;
        gameSettings.InvertVerticalInput = defaultSettings.InvertVerticalInput;
        gameSettings.MasterVolume = defaultSettings.MasterVolume;
        gameSettings.MusicVolume = defaultSettings.MusicVolume;
        gameSettings.EffectVolume = defaultSettings.EffectVolume;
        gameSettings.UIVolume = defaultSettings.UIVolume;

        gameSettings.OffEffect = defaultSettings.OffEffect;
        gameSettings.OffSound = defaultSettings.OffSound;
        gameSettings.UIVolume = defaultSettings.UIVolume;

        gameSettings.IsShowCursor = defaultSettings.IsShowCursor;

        StartSaveTask();
    }

    public async void StartSaveTask(bool isUserExecuter = false)
    {
        if (!CanStartSave(isUserExecuter))
            return;

        await InternalSave();
    }

    /// <summary>
    /// Returns whether a full save can start without changing the cooldown.
    /// </summary>
    public bool CanStartSave(bool ignoreCooldown = false)
    {
        if (isSaving)
            return false;

        if (ignoreCooldown)
            return true;

        return SaveCooldownRemainingSeconds <= 0;
    }

    private async Task InternalSave()
    {
        if (isSaving)
            return;

        bool succeeded = false;
        BeginSaveOperation();

        try
        {
            isSaving = true;

            CollectSaveableState();

            await SwitchToMainThread();
            GameplayEvents.RaiseBeforeSaveEvent();
            gameDataStorage.UpdateProjectData(projectData);
            gameDataStorage.UpdateGameSettings(gameSettings);
            gameDataStorage.Save();
            succeeded = true;

            GameplayEvents.RaiseSaveEvent();
        }
        catch(Exception ex) 
        {
            Debug.LogException(ex);
        }
        finally
        {
            isSaving = false;
            CompleteSaveOperation(succeeded, GetSuccessfulSaveInfo(succeeded));
        }
    }

    /// <summary>
    /// Собирает состояние объектов сцены в данные проекта.
    /// </summary>
    /// <remarks>
    /// Часть данных живёт не в <c>projectData</c>, а в самих объектах: брейнрот на холдере
    /// и накопленные им деньги. Без этого шага на диск ушла бы копия без них.
    /// <para>
    /// Сломавшийся объект не отменяет сохранение остальных: потерять состояние одного
    /// холдера неприятно, потерять всё сразу — гораздо хуже.
    /// </para>
    /// </remarks>
    private void CollectSaveableState()
    {
        foreach (ISaveable saveable in PRUnitySDK.Trackers.Saveables.Collect())
        {
            if (saveable.IsNull())
                continue;

            try
            {
                if (!saveable.TrySaveData())
                    PRLog.WriteWarning(this, $"{saveable.GetType().Name} не отдал состояние — в сохранении останется прежнее.");
            }
            catch (Exception exception)
            {
                PRLog.WriteError(this, $"{saveable.GetType().Name} сорвал сбор состояния: {exception}");
            }
        }
    }

    public Task SwitchToMainThread()
    {
        var tcs = new TaskCompletionSource<bool>();
        synchronizationContext.Post(_ =>
        {
            tcs.SetResult(true);
        }, null);

        return tcs.Task;
    }

    public void LoadingUserCursorState()
    {
        Cursor.visible = GetGameSettings().IsShowCursor;
        //TODO
    }

    public void ChangeCursorState()
    {
        if (!Cursor.visible && GetGameSettings().IsShowCursor)
        {
            Cursor.visible = GetGameSettings().IsShowCursor;
        }
        else
        {
            GetGameSettings().IsShowCursor = !GetGameSettings().IsShowCursor;
            Cursor.visible = GetGameSettings().IsShowCursor;
            StartSaveTask();
        }

    }

    /// <summary>
    /// Сохраняет проектные данные.
    /// </summary>
    /// <remarks>
    /// Кулдаун бережёт диск и облако от частых записей, но подходит не всему. Покупка
    /// за ресурсы или отключение рекламы должны лечь на диск сразу: игрок уже заплатил,
    /// и потерять это при закрытии вкладки нельзя.
    /// </remarks>
    /// <param name="ignoreCooldown">Сохранить не дожидаясь окончания кулдауна.</param>
    public void SaveProjectData(bool ignoreCooldown = false)
    {
        // Полным путём, вместе со сбором состояния сущностей. Часть данных живёт в сцене,
        // а не в projectData: брейнрот на холдере и накопленные им деньги попадают туда
        // только через TrySaveData. Запись без сбора кладёт на диск копию без них
        // и вдобавок сдвигает кулдаун — автосохранение, которое собрало бы состояние,
        // откладывается, и при следующем запуске холдеры оказываются пустыми.
        StartSaveTask(ignoreCooldown);
    }

    /// <summary>
    /// Сохраняет настройки игры.
    /// </summary>
    /// <param name="ignoreCooldown">Сохранить не дожидаясь окончания кулдауна.</param>
    public void SaveGameSettingsData(bool ignoreCooldown = false)
    {
        if (!CanStartSave(ignoreCooldown))
            return;

        ExecuteImmediateSave(() => gameDataStorage.UpdateGameSettings(gameSettings, true));
    }

    private void ExecuteImmediateSave(Action saveAction)
    {
        bool succeeded = false;
        BeginSaveOperation();

        try
        {
            saveAction.Invoke();
            succeeded = true;
        }
        finally
        {
            CompleteSaveOperation(succeeded, GetSuccessfulSaveInfo(succeeded));
        }
    }

    private void BeginSaveOperation()
    {
        lock (saveDiagnosticsLock)
        {
            if (activeSaveOperationCount == 0)
                activeSaveOperationFailed = false;

            activeSaveOperationCount++;
            saveState = GameSaveState.Saving;
        }
    }

    private void CompleteSaveOperation(bool succeeded, (DateTime? creationTimeUtc, DateTime? updateTimeUtc) saveInfo)
    {
        lock (saveDiagnosticsLock)
        {
            if (succeeded)
            {
                saveCreationTimeUtc = saveInfo.creationTimeUtc ?? saveCreationTimeUtc;
                lastSaveTimeUtc = saveInfo.updateTimeUtc ?? ToUtc(PRUnitySDK.ServerTime.GetNow());
                saveCooldownCounter = PRTime.Instance.CurrentRealSecond;
            }
            else
                activeSaveOperationFailed = true;

            activeSaveOperationCount = Math.Max(0, activeSaveOperationCount - 1);
            saveState = activeSaveOperationCount > 0
                ? GameSaveState.Saving
                : activeSaveOperationFailed
                    ? GameSaveState.Failed
                    : GameSaveState.Succeeded;
        }
    }

    private void CaptureLoadedSaveInfo(bool loadedExistingSave)
    {
        var saveInfo = loadedExistingSave
            ? GetStorageSaveInfoUtc()
            : (creationTimeUtc: (DateTime?)null, updateTimeUtc: (DateTime?)null);

        lock (saveDiagnosticsLock)
        {
            hasLoadedSave = loadedExistingSave;
            saveCreationTimeUtc = saveInfo.creationTimeUtc;
            lastSaveTimeUtc = saveInfo.updateTimeUtc;
        }
    }

    private (DateTime? creationTimeUtc, DateTime? updateTimeUtc) GetSuccessfulSaveInfo(bool succeeded)
    {
        return succeeded
            ? GetStorageSaveInfoUtc()
            : (null, null);
    }

    private (DateTime? creationTimeUtc, DateTime? updateTimeUtc) GetStorageSaveInfoUtc()
    {
        if (!(gameDataStorage is IGameDataStorageSaveInfo saveInfo))
            return (null, null);

        return (ToUtc(saveInfo.CreationDate), ToUtc(saveInfo.LastUpdateDate));
    }

    private static DateTime? ToUtc(DateTime? date)
    {
        if (!date.HasValue)
            return null;

        return date.Value.Kind == DateTimeKind.Utc
            ? date.Value
            : date.Value.ToUniversalTime();
    }

    public void AutoSaveHandler()
    {
        if (GetStorageSettings().EnabledAutoSave)
            StartCoroutine(AutoSave());
    }

    public void LoadDefaultControlSettings(bool overrideSettings, bool requiredSave = true)
    {
        if (overrideSettings)
            SetDefaultControlSettings();

        else if (gameSettings.Sensitivity == 0)
            SetDefaultControlSettings();

        if (requiredSave)
            StartSaveTask();
    }

    protected void SetDefaultControlSettings()
    {
        //gameSettings.Sensitivity = globalGameSettings.DefaultControlSettings.Sensitivity;
        //gameSettings.InvertHorizontalInput = globalGameSettings.DefaultControlSettings.InvertHorizontalInput;
        //gameSettings.InvertVerticalInput = globalGameSettings.DefaultControlSettings.InvertVerticalInput;
    }

    public IEnumerator AutoSave()
    {
        while (true)
        {
            yield return new WaitForSeconds(GetStorageSettings().AutoSaveSeconds);
            if (!isSaving)
                StartSaveTask();
        }
    }

    private void LoadingData()
    {
        gameSettings = gameDataStorage.GetGameSettings();
        projectData = gameDataStorage.GetProjectData();
    }

    public ProjectData GetProjectData()
    {
        return projectData ?? throw new InvalidOperationException($"{nameof(ProjectData)} is not initialized.");
    }

    public GameSettings GetGameSettings()
    {
        return gameSettings ?? throw new InvalidOperationException($"{nameof(GameSettings)} is not initialized.");
    }

    public GameStorageSettings GetStorageSettings()
    {
        return PRUnitySDK.Settings.GameStorage;
    }

    /// <summary>
    /// Событие старта подготовленной сцены.
    /// </summary>
    /// <param name="scene">Название сцены.</param>
    public void OnStartScene(string scene)
    {
        GameSettingsSession.Reset();
    }

    #endregion

    #region IReadySignalProvider

    protected readonly ReadySignal readySignal = new ReadySignal(typeof(GameManager));

    public IReadySignal ReadySignal => readySignal;

    #endregion
}
