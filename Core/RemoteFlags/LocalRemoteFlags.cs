/// <summary>
/// Флаги проекта без площадки: значения из настроек проекта.
/// </summary>
/// <remarks>
/// Работает в редакторе, на платформах без флагов и в проектах без интеграции с площадкой.
/// Реализация площадки берёт эти же значения для флагов, которых площадка не прислала.
/// </remarks>
public class LocalRemoteFlags : IRemoteFlags
{
    /// <inheritdoc />
    public bool TryGetString(string name, out string value)
    {
        value = null;

        return PRUnitySDK.Settings != null && PRUnitySDK.Settings.RemoteFlags.TryGetValue(name, out value);
    }
}
