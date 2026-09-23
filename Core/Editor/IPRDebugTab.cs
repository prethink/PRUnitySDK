/// <summary>
/// Вкладка окна отладки, объявленная модулем.
/// </summary>
/// <remarks>
/// Окно отладки лежит в ядре и не знает модулей игры, а ядро не должно ссылаться на них.
/// Поэтому модуль объявляет вкладку сам: окно находит реализации через <c>TypeCache</c>,
/// как и <see cref="IPRDebugHealthCheck"/>, и создаёт их конструктором без параметров.
/// <para>
/// Вкладка рисует себя сама, обычным <c>EditorGUILayout</c>.
/// </para>
/// </remarks>
public interface IPRDebugTab
{
    /// <summary>
    /// Название в списке вкладок; удобно добавлять число записей: <c>"Ghosts (12)"</c>.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Порядок среди вкладок модулей. Встроенные вкладки окна всегда идут первыми.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Есть ли вкладке что показать без Play Mode.
    /// </summary>
    /// <remarks>
    /// Большая часть окна — снимок живой игры, и вне игры оно пусто. Но данные на диске —
    /// сохранённые записи, кэши — смотрят и чистят как раз без запуска.
    /// </remarks>
    bool AvailableInEditMode { get; }

    /// <summary>
    /// Снимает данные. Зовётся вместе с обновлением окна, по кнопке и по таймеру.
    /// </summary>
    /// <param name="context">Состояние окна.</param>
    void Refresh(PRDebugTabContext context);

    /// <summary>
    /// Рисует вкладку.
    /// </summary>
    /// <param name="context">Состояние окна.</param>
    void Draw(PRDebugTabContext context);
}

/// <summary>
/// Что окно отладки сообщает вкладке модуля.
/// </summary>
public sealed class PRDebugTabContext
{
    /// <summary>
    /// Строка поиска окна; пустая, если не задана.
    /// </summary>
    public string Search { get; }

    /// <summary>
    /// Игрок, от чьего имени окно выполняет действия; ноль — «как решит само».
    /// </summary>
    public long Executor { get; }

    /// <summary>
    /// Идёт ли игра.
    /// </summary>
    public bool IsPlaying { get; }

    /// <param name="search">Строка поиска.</param>
    /// <param name="executor">Игрок-исполнитель.</param>
    /// <param name="isPlaying">Идёт ли игра.</param>
    public PRDebugTabContext(string search, long executor, bool isPlaying)
    {
        Search = search ?? string.Empty;
        Executor = executor;
        IsPlaying = isPlaying;
    }

    /// <summary>
    /// Подходит ли текст под строку поиска.
    /// </summary>
    public bool Matches(string text)
    {
        return string.IsNullOrEmpty(Search)
               || !string.IsNullOrEmpty(text) && text.IndexOf(Search, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
