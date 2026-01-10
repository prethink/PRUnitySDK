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
    public bool isTutorialPaused;

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
    public bool isFocusPaused;

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

    #endregion

    #region Методы

    /// <inheritdoc />
    public void SetProjectPaused(bool isPaused, object executer, bool isUserAction = false)
        => SetPauseState(isPaused, executer, e => e.IsProjectStateChange = true, ref isProjectPaused, isUserAction);

    /// <inheritdoc />
    public void SetLogicPaused(bool isPaused, object executer, bool isUserAction = false)
        => SetPauseState(isPaused, executer, e => e.isLogicStateChange = true, ref isLogicPaused, isUserAction);

    /// <inheritdoc />
    public void SetMusicPaused(bool isPaused, object executer, bool isUserAction = false)
        => SetPauseState(isPaused, executer, e => e.IsMusicStateChange = true, ref isMusicPaused, isUserAction);

    /// <inheritdoc />
    public void SetFocusPaused(bool isPaused, object executer, bool isUserAction = false)
        => SetPauseState(isPaused, executer, e => e.IsFocusStateChange = true, ref isFocusPaused, isUserAction);

    /// <inheritdoc />
    public void SetTutorialPause(bool isPaused, object executer, bool isUserAction = false)
        => SetPauseState(isPaused, executer, e => e.IsTutorialStateChange = true, ref isTutorialPaused, isUserAction);

    /// <inheritdoc />
    public void SetCutScenePause(bool isPaused, object executer, bool isUserAction = false) 
        => SetPauseState(isPaused, executer, e => e.IsCutSceneStateChange = true, ref isCutScenePaused, isUserAction);

    /// <inheritdoc />
    private void SetPauseState(bool isPaused, object executer, Action<PauseEventArgs> setStateFlag, ref bool property, bool isUserAction = false)
    {
        var previousValue = property;
        property = isPaused;
        var pauseArgs = new PauseEventArgs()
        {
            IsCutSceneStateChange = true,
            PreviousValue = previousValue,
            IsUserValue = isUserAction,
            Executer = executer
        };
        setStateFlag(pauseArgs);
        SetNotifyPauseChange(pauseArgs);
    }

    private void SetNotifyPauseChange(PauseEventArgs pauseArgs)
        => EventBus.RaiseEvent<IPauseStateListener>(invoke => invoke.OnPauseStateChanged(pauseArgs));

    public static void SetNotifyPauseChange()
        => EventBus.RaiseEvent<IPauseStateListener>(invoke => invoke.OnPauseStateChanged(new PauseEventArgs() { IsCustom = true }));

    #endregion
}
