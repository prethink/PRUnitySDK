/// <summary>
/// Контракт менеджера управления паузой.
/// Предоставляется SDK и используется внешними системами.
/// </summary>
public interface IPauseManager
{
    #region Поля и свойства

    /// <summary>
    /// Глобальная пауза проекта.
    /// Учитывает паузу проекта и потерю фокуса приложения.
    /// </summary>
    bool IsProjectPaused { get; }

    /// <summary>
    /// Пауза музыки.
    /// Учитывает глобальную паузу проекта, паузу музыки и потерю фокуса.
    /// </summary>
    bool IsMusicPaused { get; }

    /// <summary>
    /// Пауза логики.
    /// Учитывает глобальную паузу, паузу логики, туториал,
    /// катсцены, потерю фокуса и готовность сцены.
    /// </summary>
    bool IsLogicPaused { get; }

    /// <summary>
    /// Пауза туториала.
    /// </summary>
    bool IsTutorialPaused { get; }

    /// <summary>
    /// Пауза во время катсцены.
    /// </summary>
    bool IsCutScenePaused { get; }

    /// <summary>
    /// Пауза по причине потери фокуса приложения.
    /// </summary>
    bool IsFocusPaused { get; }

    #endregion

    #region Методы

    /// <summary>
    /// Установить состояние глобальной паузы проекта.
    /// </summary>
    /// <param name="isPaused">Признак паузы.</param>
    /// <param name="executer">Объект, инициировавший изменение.</param>
    /// <param name="isUserAction">Признак пользовательского действия.</param>
    void SetProjectPaused(bool isPaused, object executer, bool isUserAction = false);

    /// <summary>
    /// Установить состояние паузы логики.
    /// </summary>
    void SetLogicPaused(bool isPaused, object executer, bool isUserAction = false);

    /// <summary>
    /// Установить состояние паузы музыки.
    /// </summary>
    void SetMusicPaused(bool isPaused, object executer, bool isUserAction = false);

    /// <summary>
    /// Установить состояние паузы при потере или возврате фокуса приложения.
    /// </summary>
    void SetFocusPaused(bool isPaused, object executer, bool isUserAction = false);

    /// <summary>
    /// Установить состояние паузы туториала.
    /// </summary>
    void SetTutorialPause(bool isPaused, object executer, bool isUserAction = false);

    /// <summary>
    /// Установить состояние паузы во время катсцены.
    /// </summary>
    void SetCutScenePause(bool isPaused, object executer, bool isUserAction = false);

    #endregion
}
