#if !PRSDK_DISABLE_GAMEPUSH
using GamePush;

/// <summary>
/// Флаги проекта из переменных GamePush (панель → «Переменные»), с запасом из настроек проекта.
/// </summary>
/// <remarks>
/// Переменной нет у площадки — берётся значение из настроек (<see cref="LocalRemoteFlags"/>), как и у Яндекса.
/// В редакторе плагин на любой ключ отвечает «есть», но значение отдаёт пустым: тогда тоже берутся настройки.
/// </remarks>
public class GamePushRemoteFlags : IRemoteFlags
{
    private readonly LocalRemoteFlags defaults = new();

    /// <inheritdoc />
    public bool TryGetString(string name, out string value)
    {
        if (!string.IsNullOrEmpty(name) && GP_Init.isReady && GP_Variables.Has(name))
        {
            value = GP_Variables.GetString(name);

            if (value != null)
                return true;
        }

        return defaults.TryGetString(name, out value);
    }
}
#endif
