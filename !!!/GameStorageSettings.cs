using UnityEditor;
using UnityEngine;

/// <summary>
/// Настройки для сохранения.
/// </summary>
public class GameStorageSettings : ResourceScriptableObject
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

    [MenuItem("Assets/Create/PRUnitySDK/Settings/GameStorage settings", false, 40)]
    public static void Create()
    {
        Create<GameStorageSettings>();
    }

    #region Базовый класс

    /// <inheridoc />
    protected override void SetDefaultSettings()
    {
        EnabledAutoSave = true;
        AutoSaveSeconds = 180;
        SaveStrategy = SaveStrategy.Serialize;
        UseEncryption = true;
        EncryptionStrategy = EncryptionLoadingStrategy.Convert;
    }

    #endregion
}


public enum SaveStrategy
{
    Serialize,
    Class
}

public enum EncryptionLoadingStrategy
{
    Convert,
    OnlyEncryption
}