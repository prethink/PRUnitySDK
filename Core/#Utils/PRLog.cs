using System;
using UnityEngine;

public static class PRLog
{
    /// <summary>Если true - к каждому сообщению добавляется временная метка сервера.
    /// Раньше поле было приватным и никак не переключалось - оставалось всегда false.</summary>
    public static bool ShowDateTime { get; set; }

    private const string DEBUG_COLOR = "green";
    private const string WARNING_COLOR = "yellow";
    private const string ERROR_COLOR = "red";

    public static void WriteDebug(object obj, string message, PRLogSettings settings = null)
    {
        WriteDebug(obj.GetType(), message, settings);
    }

    public static void WriteDebug(object obj, object objectToMessage, PRLogSettings settings = null)
    {
        WriteDebug(obj.GetType(), objectToMessage?.ToString() ?? "null", settings);
    }

    public static void WriteDebug(Type type, string message, PRLogSettings settings = null)
    {
        CreateSettingsIfNull(ref settings);
        AddColorIfEmpty(settings, DEBUG_COLOR);

        if (ShouldSkipDebugLog(settings))
            return;

        Debug.Log(GetFormattedMessage(type, message, settings));
    }

    /// <summary>
    /// В релизной сборке обычный debug-лог глушится целиком (независимо от LevelDebug),
    /// в остальных случаях - только сообщения с уровнем выше настроенного порога.
    /// Единственное исключение - settings.IgnoreBuildSettings: такие сообщения
    /// показываются всегда, что бы ни было настроено выше.
    /// </summary>
    private static bool ShouldSkipDebugLog(PRLogSettings settings)
    {
        if (settings.IgnoreBuildSettings)
            return false;

        bool isRelease = PRUnitySDK.Settings.Project.ReleaseType == ReleaseType.Release;
        bool levelTooVerbose = settings.LevelDebug > PRUnitySDK.Settings.Project.DebugLogLevel;

        return isRelease || levelTooVerbose;
    }

    public static void WriteError(object obj, string message, PRLogSettings settings = null)
    {
        WriteError(obj.GetType(), message, settings);
    }

    public static void WriteError(Type type, string message, PRLogSettings settings = null)
    {
        CreateSettingsIfNull(ref settings);
        AddColorIfEmpty(settings, ERROR_COLOR);
        Debug.LogError(GetFormattedMessage(type, message, settings));
    }

    public static void WriteWarning(object obj, object objString, PRLogSettings settings = null)
    {
        WriteWarning(obj.GetType(), objString?.ToString() ?? "null", settings);
    }

    public static void WriteWarning(object obj, string message, PRLogSettings settings = null)
    {
        WriteWarning(obj.GetType(), message, settings);
    }

    public static void WriteWarning(Type type, string message, PRLogSettings settings = null)
    {
        CreateSettingsIfNull(ref settings);
        AddColorIfEmpty(settings, WARNING_COLOR);

        if (settings.ThrowException)
            throw new Exception(GetFormattedMessage(type, message, settings));

        Debug.LogWarning(GetFormattedMessage(type, message, settings));
    }

    private static string GetFormattedMessage(Type type, string message, PRLogSettings settings = null)
    {
        CreateSettingsIfNull(ref settings);

        string messageBuild = string.Empty;

        if (ShowDateTime)
            messageBuild += $"{PRUnitySDK.ServerTime.GetNow()}: ";

        messageBuild += !string.IsNullOrEmpty(settings.Color)
            ? $"[<color={settings.Color}>{type}</color>] "
            : $"[{type}] ";

        messageBuild += message;

        return messageBuild;
    }

    private static void CreateSettingsIfNull(ref PRLogSettings settings)
    {
        if (settings == null)
            settings = new PRLogSettings();
    }

    /// <summary>Задаёт цвет ТОЛЬКО если он ещё не задан явно вызывающим кодом -
    /// раньше метод безусловно перезаписывал settings.Color дефолтным значением,
    /// из-за чего кастомный цвет, переданный через PRLogSettings, никогда не мог
    /// сработать.</summary>
    private static void AddColorIfEmpty(PRLogSettings settings, string color)
    {
        if (string.IsNullOrEmpty(settings.Color))
            settings.Color = color;
    }
}

public class PRLogSettings
{
    public uint LevelDebug;
    public bool IgnoreBuildSettings;
    public string Color;
    public bool ThrowException;
}