/// <summary>
/// Читает сохранённое переопределение настройки или её значение по умолчанию.
/// </summary>
public static class OverrideProperties
{
    /// <summary>
    /// Возвращает сохранённое значение по имени, если оно есть в данных игрока.
    /// Доступен после готовности <see cref="GameManager.ReadySignal"/>.
    /// </summary>
    public static T Get<T>(string name, T fallback)
    {
        return ProjectPropertiesManager.Instance.GetValue(name, fallback);
    }

    /// <summary>
    /// Возвращает сохранённое значение по типизированному ключу или fallback.
    /// Доступен после готовности <see cref="GameManager.ReadySignal"/>.
    /// </summary>
    public static T Get<T>(EnumerationType<T> key, T fallback)
    {
        return ProjectPropertiesManager.Instance.GetValue(key, fallback);
    }
}
