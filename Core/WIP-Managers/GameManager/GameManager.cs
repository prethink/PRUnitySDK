using System;
using System.Collections;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

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

    #endregion

    #region MonoBehaviour

#if UNITY_WEBGL && !UNITY_EDITOR
[DllImport("__Internal")]
private static extern void RegisterPageVisibilityCallback(string gameObjectName, string methodName);

[DllImport("__Internal")]
private static extern void UnregisterPageVisibilityCallback();
#endif

    private void Awake()
    {
        this.RunMethodHooks(MethodHookStage.PreAwake);

        this.InitializeGameManager();

        this.RunMethodHooks(MethodHookStage.PostAwake);
    }

    private void Start()
    {
        this.RunMethodHooks(MethodHookStage.PreStart);

#if UNITY_WEBGL && !UNITY_EDITOR
        PageVisibility.Subscribe(gameObject.name, nameof(OnPageVisibilityChange));
#endif

        this.RunMethodHooks(MethodHookStage.PostAwake);
    }

    private void OnDestroy()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        PageVisibility.Unsubscribe();
#endif
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
        gameDataStorage = PRUnitySDK.GameDataStorage;
        gameDataStorage.Load();

        LoadingData();

        AutoSaveHandler();

        GameplayEvents.RaiseGameReady();
    }

    public void SaveData()
    {
        gameDataStorage.UpdateProjectData(projectData);
        gameDataStorage.UpdateGameSettings(gameSettings);
        gameDataStorage.Save();

        GameplayEvents.RaiseSaveEvent();
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
            SaveData();
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
            SaveData();
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
            SaveData();
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
