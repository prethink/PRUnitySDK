/// <summary>
/// Аргументы при изменение состояния паузы.
/// </summary>
public class PauseEventArgs
{
    /// <summary>
    /// Признак изменения музыкальной паузы.
    /// </summary>
    public bool IsMusicStateChange;

    /// <summary>
    /// Признак изменения логической паузы.
    /// </summary>
    public bool isLogicStateChange;

    /// <summary>
    /// Признак изменения фокуса.
    /// </summary>
    public bool IsFocusStateChange;

    /// <summary>
    /// Признак изменения паузы проекта.
    /// </summary>
    public bool IsProjectStateChange;

    /// <summary>
    /// Признак изменения паузы проекта.
    /// </summary>
    public bool IsTutorialStateChange;

    /// <summary>
    /// Признак изменения паузы проекта.
    /// </summary>
    public bool IsCutSceneStateChange;

    /// <summary>
    /// Предыдущее значение изменяемого элемента.
    /// </summary>
    public bool PreviousValue;

    /// <summary>
    /// Признак, того что паузу вызвал пользователь.
    /// </summary>
    public bool IsUserValue;

    /// <summary>
    /// Кто вызвал изменения паузы.
    /// </summary>
    public object Executer;

    /// <summary>
    /// Кастомный запрос.
    /// </summary>
    public bool IsCustom;
}