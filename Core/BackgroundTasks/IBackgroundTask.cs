/// <summary>
/// Контракт фоновой задачи для <see cref="BackgroundTaskTracker"/>.
/// </summary>
/// <remarks>
/// Интерфейс нужен, потому что задачей может быть и обычный класс
/// (<see cref="BackgroundTask"/>), и компонент сцены (<see cref="BackgroundTaskBehaviour"/>),
/// который уже наследует <see cref="PRMonoBehaviour"/> и второго базового класса иметь не может.
/// <para>
/// Общая механика - расписание, счётчики, обработка ошибок, состояние - не дублируется
/// в обеих реализациях, а вынесена в <see cref="BackgroundTaskRuntime"/>: интерфейс
/// описывает «что это за задача», а <see cref="Runtime"/> - «как она выполняется».
/// </para>
/// </remarks>
public interface IBackgroundTask
{
    #region Настройка

    /// <summary>
    /// Уникальный ключ задачи.
    /// </summary>
    Enumeration Key { get; }

    /// <summary>
    /// Человекочитаемое имя для логов.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Интервал между запусками в секундах.
    /// Значение меньше или равное нулю означает «каждый тик хоста».
    /// </summary>
    float RepeatSeconds { get; }

    /// <summary>
    /// Задержка перед первым запуском в секундах.
    /// </summary>
    float InitialDelaySeconds { get; }

    /// <summary>
    /// Максимальное количество запусков; меньше единицы - без ограничения.
    /// </summary>
    int MaxRepeatCount { get; }

    /// <summary>
    /// Использовать игровое время вместо реального.
    /// </summary>
    bool UseGameTime { get; }

    /// <summary>
    /// Зарегистрировать задачу, но не запускать до <c>Resume()</c>.
    /// </summary>
    bool StartPaused { get; }

    /// <summary>
    /// Сколько ошибок подряд допускается до отключения задачи.
    /// </summary>
    int MaxConsecutiveErrors { get; }

    #endregion

    #region Мост к общей реализации

    /// <summary>
    /// Состояние задачи и её выполнение: расписание, счётчики, ошибки, статус.
    /// </summary>
    BackgroundTaskRuntime Runtime { get; }

    #endregion

    #region Логика задачи

    /// <summary>
    /// Проверяет, можно ли выполнить задачу прямо сейчас.
    /// Возврат <see langword="false"/> не является ошибкой: запуск пропускается.
    /// </summary>
    bool CanExecute();

    /// <summary>
    /// Тело задачи. Вызывается из <see cref="BackgroundTaskRuntime"/> по расписанию.
    /// Напрямую вызывать не следует - иначе не отработают счётчики и обработка ошибок.
    /// </summary>
    void ExecuteTask();

    #endregion
}
