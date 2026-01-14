using System;
using UnityEngine;

public partial class PRSDKSettings
{
    [field: SerializeField] public GameStorageSettings GameStorage { get; protected set; }

    [MethodHook(MethodHookStage.Initializing)]
    public void SetDefaultGameStorageSettings()
    {
        GameStorage.SetDefaultSettings();
    }
}

/// <summary>
/// Настройки для сохранения.
/// </summary>
[Serializable]
public class GameStorageSettings
{
    /// <summary>
    /// Признак включенного автоматического сохранения данных.
    /// </summary>
    [field: SerializeField] public bool EnabledAutoSave { get; private set; }

    /// <summary>
    /// Период автоматического сохранения если оно включено.
    /// </summary>
    [field: SerializeField] public uint AutoSaveSeconds { get; private set; }

    /// <summary>
    /// Стратегия сохранения/загрузки.
    /// </summary>
    [field: SerializeField] public SaveStrategy SaveStrategy { get; private set; }

    /// <summary>
    /// Признак использования шифрования.
    /// </summary>
    [field: SerializeField] public bool UseEncryption { get; private set; } = true;

    /// <summary>
    /// Стратегия при загрузке шифрованных данных.
    /// </summary>
    [field: SerializeField] public EncryptionLoadingStrategy EncryptionStrategy { get; private set; }

    #region Базовый класс

    /// <inheridoc />
    public void SetDefaultSettings()
    {
        EnabledAutoSave = true;
        AutoSaveSeconds = 180;
        SaveStrategy = SaveStrategy.Serialize;
        UseEncryption = true;
        EncryptionStrategy = EncryptionLoadingStrategy.Convert;
    }

    #endregion
}

/// <summary>
/// 
/// </summary>
public enum SaveStrategy
{
    /// <summary>
    /// 
    /// </summary>
    Serialize,
    /// <summary>
    /// 
    /// </summary>
    Class
}

/// <summary>
/// 
/// </summary>
public enum EncryptionLoadingStrategy
{
    /// <summary>
    /// 
    /// </summary>
    Convert,
    /// <summary>
    /// 
    /// </summary>
    OnlyEncryption
}

