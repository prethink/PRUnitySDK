using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public partial class GameManager : MonoBehaviourSingletonBase<GameManager>
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
        this.RunMethodHooks(MethodHookStage.PostAwake);
    }

    private void OnDestroy()
    {

    }

    private void OnApplicationPause(bool pauseStatus)
    {
        PRLog.WriteDebug(this, $"{nameof(OnApplicationPause)} pauseStatus - {pauseStatus}", new PRLogSettings() { LevelDebug = 5 });

        PRUnitySDK.PauseManager.SetProjectPaused(pauseStatus, this);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        PRLog.WriteDebug(this, $"{nameof(OnApplicationFocus)} pauseStatus - {hasFocus}", new PRLogSettings() { LevelDebug = 5 });

        var requiredPause = !hasFocus;
        PRUnitySDK.PauseManager.SetFocusPaused(requiredPause, this);
    }

    public void OnPageVisibilityChange(int isHiddenInt)
    {
        if (!PRUnitySDK.DeviceInfo.IsIOS())
            return;

        bool isHidden = isHiddenInt == 1;
        PRLog.WriteDebug(this, $"WebGL Visibility Changed. Hidden: {isHidden}", new PRLogSettings() { LevelDebug = 5 });

        PRUnitySDK.PauseManager.SetFocusPaused(!isHidden, this);
    }

    #endregion

    #region Методы

    public void InitializeGameManager()
    {
        if (isInitialize)
            return;

        synchronizationContext = SynchronizationContext.Current;

        gameDataStorage = PRUnitySDK.GameDataStorage;
        bool isRequiredFirstInitialize = !gameDataStorage.TryLoad();

        LoadingData();

        if(isRequiredFirstInitialize)
            InitializeDefaultData();

        AutoSaveHandler();

        GameplayEvents.RaiseGameReady();
        isInitialize = true;
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
        if (!CanSave(isUserExecuter))
            return;

        await InternalSave();
    }

    private bool CanSave(bool isUserExecuter)
    {
        if (isSaving)
            return false;

        if (isUserExecuter)
            return true;

        var saveCooldownSeconds = PRUnitySDK.Settings.GameStorage.SaveCooldownSeconds;
        if (saveCooldownSeconds <= 0)
            return true;

        if(PRTime.Instance.CurrentRealSecond > saveCooldownCounter + saveCooldownSeconds)
        {
            saveCooldownCounter = PRTime.Instance.CurrentRealSecond;
            return true;
        }

        return false;
    }

    private async Task InternalSave()
    {
        if (isSaving)
            return;

        try
        {
            isSaving = true;

            var saveTasks = PRUnitySDK.Trackers.Saveables.Where(x => !x.IsNull()).Select(x => x.TrySaveData());
            await Task.WhenAll(saveTasks);

            await SwitchToMainThread();
            GameplayEvents.RaiseBeforeSaveEvent();
            gameDataStorage.UpdateProjectData(projectData);
            gameDataStorage.UpdateGameSettings(gameSettings);
            gameDataStorage.Save();

            GameplayEvents.RaiseSaveEvent();

        }
        catch(Exception ex) 
        {
            Debug.LogException(ex);
        }
        finally
        {
            isSaving = false;
        }
    }

    public Task SwitchToMainThread()
    {
        var tcs = new TaskCompletionSource<bool>();
        Debug.Log(synchronizationContext);
        synchronizationContext.Post(_ =>
        {
            tcs.SetResult(true);
        }, null);

        return tcs.Task;
    }

    public void LoadingUserCursorState()
    {
        Cursor.visible = GetGameSettings().IsShowCursor;
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

    public void SaveProjectData()
    {
        gameDataStorage.UpdateProjectData(projectData, true);
    }

    public void SaveGameSettingsData()
    {
        gameDataStorage.UpdateGameSettings(gameSettings, true);
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
}
