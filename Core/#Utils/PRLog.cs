using System;
using UnityEngine;

public static class PRLog 
{
    private static bool showDateTime;

    private const string DEBUG_COLOR         = "green";
    private const string WARNING_COLOR       = "yellow";
    private const string ERROR_COLOR         = "red";

    public static void WriteDebug(object obj, string message, PRLogSettings settings = null)
    {
        WriteDebug(obj.GetType(), message, settings);
    }

    public static void WriteDebug(object obj, object objectToMessage, PRLogSettings settings = null)
    {
        WriteDebug(obj.GetType(), objectToMessage.ToString(), settings);
    }

    public static void WriteDebug(Type type, string message, PRLogSettings settings = null)
    {
        CreateSettingsIfNull(ref settings);
        AddColorIfEmpty(settings, DEBUG_COLOR);

        if (PRUnitySDK.Settings.Project.ReleaseType == ReleaseType.Release && !settings.IgnoreBuildSettings || !settings.IgnoreBuildSettings && settings.LevelDebug > PRUnitySDK.Settings.Project.DebugLogLevel)
            return;

        Debug.Log(GetFormattedMessage(type, message, settings));
    }

    public static void WriteError(object obj, string message, PRLogSettings settings = null)
    {
        CreateSettingsIfNull(ref settings);
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
        WriteWarning(obj.GetType(), objString.ToString(), settings);
    }

    public static void WriteWarning(object obj, string message, PRLogSettings settings = null)
    {
        WriteWarning(obj.GetType(), message, settings);
    }

    public static void WriteWarning(Type type, string message, PRLogSettings settings = null)
    {
        CreateSettingsIfNull(ref settings);
        AddColorIfEmpty(settings, WARNING_COLOR);
        if(settings.ThrowException)
            throw new Exception(GetFormattedMessage(type, message, settings));
        else
            Debug.LogWarning(GetFormattedMessage(type, message, settings));

    }

    private static string GetFormattedMessage(Type type, string message, PRLogSettings settings = null)
    {
        CreateSettingsIfNull(ref settings);

        string messageBuild = string.Empty;
        if (showDateTime)
            messageBuild += $"{PRUnitySDK.ServerTime.GetNow()}: ";

        messageBuild += !string.IsNullOrEmpty(settings.Color)
            ? $"[<color={settings.Color}>{type}</color>] "
            : $"[{type}] ";

        messageBuild += message;

        return messageBuild;
    }

    private static void CreateSettingsIfNull(ref PRLogSettings settings)
    {
        if(settings == null)
            settings = new PRLogSettings();
    }

    private static void AddColorIfEmpty(PRLogSettings settings, string color)
    {
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
