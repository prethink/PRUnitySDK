using System;
using System.Globalization;

/// <summary>
/// Чтение флагов проекта числом, дробным и логическим значением.
/// </summary>
/// <remarks>
/// Разбор один на все реализации: площадка отдаёт строку, и правила чтения не должны
/// зависеть от того, откуда она пришла. Числа — в инвариантной культуре, дробная часть
/// через точку или запятую: «0,5» из консоли и «0.5» из настроек читаются одинаково.
/// </remarks>
public static class RemoteFlagsExtensions
{
    /// <summary>
    /// Флаг целым числом.
    /// </summary>
    /// <returns><c>false</c>, если флага нет или он не число.</returns>
    public static bool TryGetInt(this IRemoteFlags flags, string name, out int value)
    {
        value = 0;

        return flags.TryGetString(name, out string raw) &&
               int.TryParse(raw?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Флаг дробным числом.
    /// </summary>
    /// <returns><c>false</c>, если флага нет или он не число.</returns>
    public static bool TryGetFloat(this IRemoteFlags flags, string name, out float value)
    {
        value = 0f;

        return flags.TryGetString(name, out string raw) && raw != null &&
               float.TryParse(raw.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Флаг логическим значением: <c>true/false</c>, <c>1/0</c>, <c>yes/no</c>, <c>on/off</c>.
    /// </summary>
    /// <returns><c>false</c>, если флага нет или значение не распознано.</returns>
    public static bool TryGetBool(this IRemoteFlags flags, string name, out bool value)
    {
        value = false;

        if (!flags.TryGetString(name, out string raw) || raw == null)
            return false;

        switch (raw.Trim().ToLowerInvariant())
        {
            case "true":
            case "1":
            case "yes":
            case "on":
                value = true;
                return true;

            case "false":
            case "0":
            case "no":
            case "off":
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Флаг строкой или <paramref name="fallback"/>, если его нет.
    /// </summary>
    public static string GetString(this IRemoteFlags flags, string name, string fallback = null)
    {
        return flags.TryGetString(name, out string value) ? value : fallback;
    }

    /// <summary>
    /// Флаг целым числом или <paramref name="fallback"/>.
    /// </summary>
    public static int GetInt(this IRemoteFlags flags, string name, int fallback = 0)
    {
        return flags.TryGetInt(name, out int value) ? value : fallback;
    }

    /// <summary>
    /// Флаг дробным числом или <paramref name="fallback"/>.
    /// </summary>
    public static float GetFloat(this IRemoteFlags flags, string name, float fallback = 0f)
    {
        return flags.TryGetFloat(name, out float value) ? value : fallback;
    }

    /// <summary>
    /// Флаг логическим значением или <paramref name="fallback"/>.
    /// </summary>
    public static bool GetBool(this IRemoteFlags flags, string name, bool fallback = false)
    {
        return flags.TryGetBool(name, out bool value) ? value : fallback;
    }

    /// <summary>
    /// Флаг значением перечисления по имени (без учёта регистра) или <paramref name="fallback"/>.
    /// </summary>
    public static T GetEnum<T>(this IRemoteFlags flags, string name, T fallback = default)
        where T : struct, Enum
    {
        return flags.TryGetString(name, out string raw) && Enum.TryParse(raw?.Trim(), true, out T value)
            ? value
            : fallback;
    }
}
