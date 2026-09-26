using YG;

/// <summary>
/// Флаги проекта с Яндекса (флаги в консоли разработчика), с запасом из настроек проекта.
/// </summary>
/// <remarks>
/// Флага нет у Яндекса — берётся значение из настроек (<see cref="LocalRemoteFlags"/>):
/// консоль задаёт только то, что меняют, остальное живёт умолчаниями в проекте.
/// В редакторе YG2 отдаёт флаги из своих настроек (InfoYG → Flags).
/// </remarks>
public class YandexRemoteFlags : IRemoteFlags
{
    private readonly LocalRemoteFlags defaults = new();

    /// <inheritdoc />
    public bool TryGetString(string name, out string value)
    {
        if (!string.IsNullOrEmpty(name) && YG2.TryGetFlag(name, out value))
            return true;

        return defaults.TryGetString(name, out value);
    }
}
