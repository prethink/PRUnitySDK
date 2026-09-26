using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Читает и пишет сохранение с учётом его версии и шифрования.
/// </summary>
/// <remarks>
/// Версия проверяется на JSON до типизированного чтения, чтобы <see cref="ISaveMigration"/>
/// видел поля, которых в <see cref="PRSaveData"/> уже нет.
/// </remarks>
public static class SaveDataVersioning
{
    private const string VersionField = nameof(PRSaveData.Version);

    private static readonly JsonSerializer Serializer = JsonSerializer.Create(PRJsonUtils.GetJsonYandexSettings());

    private static Dictionary<int, ISaveMigration> migrations;

    private static GameStorageSettings Settings => PRUnitySDK.Settings.GameStorage;

    /// <summary>
    /// Версия формата, которую пишет эта сборка.
    /// </summary>
    public static int CurrentVersion => Settings.SaveVersion;

    /// <summary>
    /// Читает сохранение из строки хранилища.
    /// </summary>
    /// <remarks>
    /// Обычный JSON пробуется первым, если настройки не требуют только шифрования: так
    /// подхватывается сохранение, записанное до включения или выключения шифрования.
    /// </remarks>
    /// <param name="data">Прочитанное сохранение. Если прочитать не удалось или сохранение отброшено из-за версии, новое.</param>
    /// <returns><c>true</c>, если прочитано существующее сохранение.</returns>
    public static bool TryReadRaw(string raw, out PRSaveData data)
    {
        return Complete(ReadRaw(raw, out PRSaveData loaded), loaded, out data);
    }

    /// <summary>
    /// Проверяет версию сохранения, которое хранилище держит объектом.
    /// </summary>
    /// <param name="data">Прочитанное сохранение. Если его нет или оно отброшено из-за версии, новое.</param>
    /// <returns><c>true</c>, если прочитано существующее сохранение.</returns>
    public static bool TryRead(PRSaveData stored, out PRSaveData data)
    {
        if (stored == null)
            return Complete(SaveReadResult.Failed, null, out data);

        return Complete(Read(JObject.FromObject(stored, Serializer), out PRSaveData loaded), loaded, out data);
    }

    /// <summary>
    /// Ставит версию и превращает сохранение в строку для хранилища, шифруя по настройкам.
    /// </summary>
    public static string Serialize(PRSaveData data)
    {
        Stamp(data);

        return Settings.UseEncryption
            ? PRJsonUtils.SerializeObjectWithEncryption(data)
            : PRJsonUtils.SerializeObject(data);
    }

    /// <summary>
    /// Ставит сохранению версию перед записью.
    /// </summary>
    /// <remarks>
    /// Версию новее текущей не понижает, иначе старая сборка выдала бы чужие данные за свои.
    /// </remarks>
    public static void Stamp(PRSaveData data)
    {
        if (data != null)
            data.Version = Math.Max(data.Version, CurrentVersion);
    }

    /// <summary>
    /// Доводит JSON сохранения до текущей версии.
    /// </summary>
    /// <param name="getSteps">Шаги преобразования по версии, из которой они переводят. Запрашиваются, только когда нужны.</param>
    /// <returns><c>false</c>, если сохранение нужно отбросить.</returns>
    public static bool Upgrade(JObject root, int currentVersion, SaveVersionMismatchAction action, Func<IReadOnlyDictionary<int, ISaveMigration>> getSteps)
    {
        int savedVersion = ReadVersion(root);

        if (savedVersion == currentVersion)
            return true;

        if (savedVersion > currentVersion)
        {
            PRLog.WriteWarning(typeof(SaveDataVersioning), $"Сохранение версии {savedVersion} новее текущей {currentVersion}, загружается как есть.");
            return true;
        }

        if (action == SaveVersionMismatchAction.StartNew)
        {
            PRLog.WriteWarning(typeof(SaveDataVersioning), $"Сохранение версии {savedVersion} старше текущей {currentVersion}, по настройке начинается новое.");
            return false;
        }

        // Keep - то же преобразование, только без шагов.
        IReadOnlyDictionary<int, ISaveMigration> steps = action == SaveVersionMismatchAction.Convert ? getSteps() : null;

        for (int version = savedVersion; version < currentVersion; version++)
        {
            if (steps == null || !steps.TryGetValue(version, out ISaveMigration migration))
                continue;

            try
            {
                migration.Migrate(root);
            }
            catch (Exception ex)
            {
                PRLog.WriteError(typeof(SaveDataVersioning), $"Шаг {migration.GetType().Name} ({version} → {version + 1}) не преобразовал сохранение, начинается новое: {ex}");
                return false;
            }
        }

        root[VersionField] = currentVersion;
        PRLog.WriteDebug(typeof(SaveDataVersioning), $"Сохранение версии {savedVersion} загружено под версией {currentVersion} ({action}).");

        return true;
    }

    /// <summary>
    /// Версия сохранения. У сохранений без неё 0.
    /// </summary>
    public static int ReadVersion(JObject root)
    {
        JToken token = root?[VersionField];

        return token != null && token.Type == JTokenType.Integer
            ? token.Value<int>()
            : 0;
    }

    /// <summary>
    /// Разбирает JSON сохранения так, как его увидит <see cref="ISaveMigration"/>.
    /// </summary>
    /// <remarks>
    /// Даты остаются строками, как в файле, а дробные числа читаются в decimal, чтобы не терять точность на double.
    /// </remarks>
    public static JObject Parse(string json)
    {
        using var reader = new JsonTextReader(new StringReader(json))
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Decimal
        };

        return JObject.Load(reader);
    }

    /// <summary>
    /// Сводит результат чтения к тому, что нужно хранилищу: данные всегда есть, а
    /// отброшенное из-за версии сохранение для него то же, что первый запуск.
    /// </summary>
    private static bool Complete(SaveReadResult result, PRSaveData loaded, out PRSaveData data)
    {
        data = result == SaveReadResult.Failed ? new PRSaveData() : loaded;

        return result == SaveReadResult.Loaded;
    }

    private static SaveReadResult ReadRaw(string raw, out PRSaveData data)
    {
        data = null;

        if (string.IsNullOrEmpty(raw))
        {
            PRLog.WriteWarning(typeof(SaveDataVersioning), "Cannot loading. Raw data is empty.");
            return SaveReadResult.Failed;
        }

        bool plainAllowed = !Settings.UseEncryption || Settings.EncryptionStrategy == EncryptionLoadingStrategy.Convert;

        if (plainAllowed)
        {
            SaveReadResult plain = TryReadJson(raw, out data, false);

            if (plain != SaveReadResult.Failed)
            {
                PRLog.WriteDebug(typeof(SaveDataVersioning), "Success loading data.");
                return plain;
            }
        }

        SaveReadResult encrypted = PRJsonUtils.TryDecrypt(raw, out string json)
            ? TryReadJson(json, out data)
            : SaveReadResult.Failed;

        if (encrypted != SaveReadResult.Failed)
            PRLog.WriteDebug(typeof(SaveDataVersioning), "Success loading encryption data.");
        else
            PRLog.WriteError(typeof(SaveDataVersioning), "Cannot loading data");

        return encrypted;
    }

    /// <param name="showError">Писать ли в лог, что строка не JSON. Выключают, когда строка может быть зашифрованной.</param>
    private static SaveReadResult TryReadJson(string json, out PRSaveData data, bool showError = true)
    {
        data = null;

        if (string.IsNullOrWhiteSpace(json))
            return SaveReadResult.Failed;

        JObject root;

        try
        {
            root = Parse(json);
        }
        catch (JsonException ex)
        {
            if (showError)
                PRLog.WriteDebug(typeof(SaveDataVersioning), ex.ToString());

            return SaveReadResult.Failed;
        }

        return Read(root, out data);
    }

    private static SaveReadResult Read(JObject root, out PRSaveData data)
    {
        if (!Upgrade(root, CurrentVersion, Settings.VersionMismatchAction, GetMigrations))
        {
            data = new PRSaveData();
            return SaveReadResult.Discarded;
        }

        try
        {
            data = root.ToObject<PRSaveData>(Serializer);
        }
        catch (JsonException ex)
        {
            PRLog.WriteDebug(typeof(SaveDataVersioning), ex.ToString());
            data = null;
        }

        return data != null ? SaveReadResult.Loaded : SaveReadResult.Failed;
    }

    private static IReadOnlyDictionary<int, ISaveMigration> GetMigrations()
    {
        if (migrations != null)
            return migrations;

        migrations = new Dictionary<int, ISaveMigration>();

        // При двух шагах на одну версию по имени всегда выигрывает один и тот же.
        IEnumerable<Type> types = ReflectionExtension.FindClassesImplementingInterface<ISaveMigration>()
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

        foreach (Type type in types)
        {
            ISaveMigration migration;

            try
            {
                migration = (ISaveMigration)Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                PRLog.WriteError(typeof(SaveDataVersioning), $"Шаг {type.Name} не создан, нужен публичный конструктор без параметров: {ex.Message}");
                continue;
            }

            if (migrations.TryGetValue(migration.FromVersion, out ISaveMigration existing))
            {
                PRLog.WriteError(typeof(SaveDataVersioning), $"Два шага с версии {migration.FromVersion}: {existing.GetType().Name} и {type.Name}. Работает первый.");
                continue;
            }

            migrations.Add(migration.FromVersion, migration);
        }

        return migrations;
    }
}
