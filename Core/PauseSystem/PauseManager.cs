using System;

/// <summary>
/// Менеджер управления паузой.
/// </summary>
public class PauseManager : IPauseManager
{
    #region Поля и свойства

    /// <inheritdoc />
    public bool IsProjectPaused => isProjectPaused || IsFocusPaused;

    /// <inheritdoc />
    public bool IsMusicPaused => isProjectPaused || isMusicPaused || IsFocusPaused;

    /// <inheritdoc />
    public bool IsLogicPaused => isProjectPaused || isLogicPaused || IsFocusPaused /* || !SceneChanger.IsReady*/ || IsTutorialPaused || IsCutScenePaused;

    /// <inheritdoc />
    public bool IsTutorialPaused => isTutorialPaused;

    /// <inheritdoc />
    private bool isTutorialPaused;

    /// <inheritdoc />
    public bool IsCutScenePaused => isCutScenePaused;

    /// <inheritdoc />
    public bool IsFocusPaused => isFocusPaused;

    /// <summary>
    /// Пауза во время катсцены.
    /// </summary>
    private bool isCutScenePaused;

    /// <summary>
    /// Пауза по причине потери фокуса приложения.
    /// </summary>
    private bool isFocusPaused;

    /// <summary>
    /// Музыкальная пауза.
    /// </summary>
    private bool isMusicPaused;

    /// <summary>
    /// Логическая пауза.
    /// </summary>
    private bool isLogicPaused;

    /// <summary>
    /// Логическая пауза.
    /// </summary>
    private bool isProjectPaused;

    /// <summary>
    /// Кто поставил паузу проекта (последним); <c>null</c> — паузы нет.
    /// </summary>
    /// <remarks>
    /// Это и следующие «…By» — для вкладки Time окна отладки: пауза — флаг, и без источника не понять, кто остановил
    /// игру. Флаг общий, поэтому показан последний, кто его поставил.
    /// </remarks>
    public object ProjectPausedBy => projectPausedBy;

    /// <summary>
    /// Кто поставил логическую паузу (последним); <c>null</c> — паузы нет.
    /// </summary>
    public object LogicPausedBy => logicPausedBy;

    /// <summary>
    /// Кто поставил паузу по фокусу; <c>null</c> — паузы нет.
    /// </summary>
    public object FocusPausedBy => focusPausedBy;

    /// <summary>
    /// Кто поставил паузу музыки; <c>null</c> — паузы нет.
    /// </summary>
    public object MusicPausedBy => musicPausedBy;

    /// <summary>
    /// Кто поставил паузу обучения; <c>null</c> — паузы нет.
    /// </summary>
    public object TutorialPausedBy => tutorialPausedBy;

    /// <summary>
    /// Кто поставил паузу катсцены; <c>null</c> — паузы нет.
    /// </summary>
    public object CutScenePausedBy => cutScenePausedBy;

    private object projectPausedBy;
    private object logicPausedBy;
    private object focusPausedBy;
    private object musicPausedBy;
    private object tutorialPausedBy;
    private object cutScenePausedBy;

    #endregion

    #region Методы

    /// <inheritdoc />
    public void SetProjectPaused(bool isPaused, object executer, bool isUserAction = false)
        => SetPauseState(isPaused, executer, e => e.IsProjectStateChange = true, ref isProjectPaused, ref projectPausedBy, isUserAction);

    /// <inheritdoc />
    public void SetLogicPaused(bool isPaused, object executer, bool isUserAction = false)
        => SetPauseState(isPaused, executer, e => e.isLogicStateChange = true, ref isLogicPaused, ref logicPausedBy, isUserAction);

    /// <inheritdoc />
    public void SetMusicPaused(bool isPaused, object executer, bool isUserAction = false)
        => SetPauseState(isPaused, executer, e => e.IsMusicStateChange = true, ref isMusicPaused, ref musicPausedBy, isUserAction);

    /// <inheritdoc />
    public void SetFocusPaused(bool isPaused, object executer, bool isUserAction = false)
    {
        // Автоматическая проверка идёт и без фокуса: окно Unity может быть в фоне.
        if (isPaused && PRUnitySDK.IsAutomatedRun)
            return;

        SetPauseState(isPaused, executer, e => e.IsFocusStateChange = true, ref isFocusPaused, ref focusPausedBy, isUserAction);
    }

    /// <inheritdoc />
    public void SetTutorialPause(bool isPaused, object executer, bool isUserAction = false)
        => SetPauseState(isPaused, executer, e => e.IsTutorialStateChange = true, ref isTutorialPaused, ref tutorialPausedBy, isUserAction);

    /// <inheritdoc />
    public void SetCutScenePause(bool isPaused, object executer, bool isUserAction = false) 
        => SetPauseState(isPaused, executer, e => e.IsCutSceneStateChange = true, ref isCutScenePaused, ref cutScenePausedBy, isUserAction);

    /// <inheritdoc />
    private void SetPauseState(bool isPaused, object executer, Action<PauseStateEventArgs> setStateFlag, ref bool property,
        ref object pausedBy, bool isUserAction = false)
    {
        var previousValue = property;
        property = isPaused;
        // Без источника флаг всё равно поставлен: отметка нужна, иначе пауза выглядела бы снятой.
        pausedBy = isPaused ? executer ?? "источник не указан" : null;
        var pauseArgs = new PauseStateEventArgs()
        {
            PreviousValue = previousValue,
            CurrentValue = isPaused,
            IsUserValue = isUserAction,
            Executer = executer
        };
        setStateFlag(pauseArgs);
        SetNotifyPauseChange(pauseArgs);
    }

    private void SetNotifyPauseChange(PauseStateEventArgs pauseArgs)
        => EventBus.RaiseEvent<IPauseStateListener>(invoke => invoke.OnPauseStateChanged(pauseArgs));

    public static void SetNotifyPauseChange()
        => EventBus.RaiseEvent<IPauseStateListener>(invoke => invoke.OnPauseStateChanged(new PauseStateEventArgs() { IsCustom = true }));

    #endregion
}
