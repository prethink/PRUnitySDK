#if PRSDK_TESTS
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Общая база хранилищ: сохранение, которое не прочиталось, не затирается пустым.
/// </summary>
public class GameDataStorageBaseTests
{
    /// <summary>
    /// Хранилище в памяти: строка «площадки» и счётчик записей.
    /// </summary>
    private sealed class MemoryStorage : GameDataStorageBase
    {
        public string Raw;

        public int Writes;

        public bool Accepts = true;

        /// <summary>
        /// «Площадка» не поднялась: читать нечего.
        /// </summary>
        public bool Offline;

        protected override string ReadRaw()
        {
            if (!Offline)
                return Raw;

            MarkUnavailable();
            return string.Empty;
        }

        protected override bool WriteRaw(string raw)
        {
            if (!Accepts)
                return false;

            Raw = raw;
            Writes++;
            return true;
        }
    }

    private sealed class FixedTime : IServerTime
    {
        public DateTime Now = new(2026, 11, 14, 12, 0, 0);

        public DateTime GetNow() => Now;
    }

    private const string Garbage = "{oops";

    /// <summary>
    /// Ключ хранилища по умолчанию.
    /// </summary>
    private const string PlayerPrefsKey = "PRSaveData";

    private IServerTime previousTime;
    private FixedTime time;
    private bool hadPrefs;
    private string previousPrefs;

    [SetUp]
    public void SetUp()
    {
        // В EditMode SDK не поднят: даты сохранения берутся из подставленных часов.
        previousTime = PRUnitySDK.ServerTime;
        time = new FixedTime();
        PRUnitySDK.ServerTime = time;


        hadPrefs = PlayerPrefs.HasKey(PlayerPrefsKey);
        previousPrefs = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
    }

    /// <summary>
    /// Нечитаемое сохранение пишет в журнал ошибку — это и есть проверяемое поведение, тест из-за неё не падает.
    /// </summary>
    /// <remarks>
    /// Зовётся в теле теста: выставленный в SetUp флаг на тест не действует. Сбрасывает его сам Unity.
    /// </remarks>
    private static void IgnoreLoggedErrors()
    {
        LogAssert.ignoreFailingMessages = true;
    }

    [TearDown]
    public void TearDown()
    {
        if (hadPrefs)
            PlayerPrefs.SetString(PlayerPrefsKey, previousPrefs);
        else
            PlayerPrefs.DeleteKey(PlayerPrefsKey);

        PlayerPrefs.Save();

        PRUnitySDK.ServerTime = previousTime;
    }

    /// <summary>
    /// Строка настоящего сохранения — в том виде, в каком её пишет проект (с шифрованием, если оно включено).
    /// </summary>
    private static string SaveWith(string saveId)
    {
        return SaveDataVersioning.Serialize(new PRSaveData { SaveId = saveId });
    }

    [Test]
    public void EmptyStorageIsFirstLaunchAndCanBeWritten()
    {
        IgnoreLoggedErrors();

        var storage = new MemoryStorage { Raw = string.Empty };

        Assert.IsFalse(storage.TryLoad());
        Assert.IsFalse(storage.IsWriteBlocked);

        storage.Save();

        Assert.AreEqual(1, storage.Writes);
        Assert.IsNotEmpty(storage.Raw);
    }

    [Test]
    public void NullFromStorageIsFirstLaunch()
    {
        IgnoreLoggedErrors();

        var storage = new MemoryStorage { Raw = null };

        Assert.IsFalse(storage.TryLoad());
        Assert.IsFalse(storage.IsWriteBlocked);
        Assert.IsNotNull(storage.GetProjectData());
        Assert.IsNotNull(storage.GetGameSettings());
    }

    [Test]
    public void ExistingSaveIsLoadedAndRewritten()
    {
        IgnoreLoggedErrors();

        var storage = new MemoryStorage { Raw = SaveWith("player-1") };

        Assert.IsTrue(storage.TryLoad());
        Assert.IsFalse(storage.IsWriteBlocked);

        time.Now = time.Now.AddMinutes(5);
        storage.Save();

        Assert.AreEqual(1, storage.Writes);
        Assert.AreEqual(time.Now, storage.LastUpdateDate);

        // Записанное читается обратно тем же сохранением.
        Assert.IsTrue(SaveDataVersioning.TryReadRaw(storage.Raw, out PRSaveData data));
        Assert.AreEqual("player-1", data.SaveId);
    }

    [Test]
    public void UnreadableSaveIsNeverOverwritten()
    {
        IgnoreLoggedErrors();

        var storage = new MemoryStorage { Raw = Garbage };

        Assert.IsFalse(storage.TryLoad(), "Нечитаемое сохранение для игры — как первый запуск.");
        Assert.IsTrue(storage.IsWriteBlocked);

        // Ровно то, что делает GameManager при первом запуске: заполнить данные и записать.
        storage.UpdateProjectData(new ProjectData());
        storage.UpdateGameSettings(new GameSettings(), true);
        storage.Save();

        Assert.AreEqual(0, storage.Writes);
        Assert.AreEqual(Garbage, storage.Raw, "Строка игрока должна остаться как была.");
    }

    [Test]
    public void UnreadableSaveStillGivesTheGameData()
    {
        IgnoreLoggedErrors();

        var storage = new MemoryStorage { Raw = Garbage };
        storage.TryLoad();

        Assert.IsNotNull(storage.GetProjectData());
        Assert.IsNotNull(storage.GetGameSettings());

        bool ready = false;
        storage.ReadySignal.SubscribeOnReady(() => ready = true);
        Assert.IsTrue(ready, "Сигнал готовности поднят и при нечитаемом сохранении.");
    }

    /// <remarks>
    /// Так выглядит сохранение после обновления, в котором у поля сменили тип. Чтение такую ошибку пропускает
    /// (<c>PRJsonUtils.GetJsonYandexSettings</c>): поле остаётся по умолчанию, остальное загружается. Сохранение
    /// поэтому читаемое, и запись не блокируется.
    /// </remarks>
    [Test]
    public void SaveWithWrongFieldTypeLoadsWithoutThatField()
    {
        IgnoreLoggedErrors();

        var storage = new MemoryStorage { Raw = "{\"SaveId\":\"a\",\"ProjectData\":123}" };

        storage.TryLoad();

        if (storage.IsWriteBlocked)
            Assert.Ignore("Настройки проекта читают только зашифрованные сохранения: открытый JSON здесь нечитаем.");

        Assert.IsNotNull(storage.GetProjectData());

        storage.Save();

        Assert.AreEqual(1, storage.Writes);
    }

    [Test]
    public void TruncatedSaveIsUnreadable()
    {
        IgnoreLoggedErrors();

        // Оборванная запись: начало настоящего сохранения без конца.
        string full = SaveWith("player-1");
        var storage = new MemoryStorage { Raw = full.Substring(0, full.Length / 2) };
        string before = storage.Raw;

        Assert.IsFalse(storage.TryLoad());
        Assert.IsTrue(storage.IsWriteBlocked);

        storage.Save();

        Assert.AreEqual(before, storage.Raw);
    }

    [Test]
    public void SaveWithEmptyPartsGetsDefaults()
    {
        IgnoreLoggedErrors();

        var storage = new MemoryStorage { Raw = "{\"SaveId\":\"a\",\"GameSettings\":null,\"ProjectData\":null}" };

        storage.TryLoad();

        if (storage.IsWriteBlocked)
            Assert.Ignore("Настройки проекта читают только зашифрованные сохранения: открытый JSON здесь нечитаем.");

        Assert.IsNotNull(storage.GetProjectData());
        Assert.IsNotNull(storage.GetGameSettings());
    }

    [Test]
    public void ReloadAfterUnreadableSaveUnblocksWriting()
    {
        IgnoreLoggedErrors();

        var storage = new MemoryStorage { Raw = Garbage };
        storage.TryLoad();

        storage.Raw = string.Empty;
        storage.TryLoad();

        Assert.IsFalse(storage.IsWriteBlocked);
    }

    [Test]
    public void RejectedWriteDoesNotThrow()
    {
        IgnoreLoggedErrors();

        var storage = new MemoryStorage { Raw = string.Empty, Accepts = false };
        storage.TryLoad();

        Assert.DoesNotThrow(storage.Save);
        Assert.AreEqual(0, storage.Writes);
    }

    [Test]
    public void StorageUnavailableOnLoadIsNeverWrittenLater()
    {
        IgnoreLoggedErrors();

        string playerSave = SaveWith("player-1");
        var storage = new MemoryStorage { Raw = playerSave, Offline = true };

        Assert.IsFalse(storage.TryLoad());
        Assert.IsTrue(storage.IsWriteBlocked);

        // Площадка ответила с опозданием: запись этого запуска легла бы поверх настоящего сохранения.
        storage.Offline = false;
        storage.Save();

        Assert.AreEqual(0, storage.Writes);
        Assert.AreEqual(playerSave, storage.Raw);
        Assert.IsFalse(storage.LastSaveWritten);
    }

    [Test]
    public void LastSaveWrittenFollowsTheStorage()
    {
        IgnoreLoggedErrors();

        var storage = new MemoryStorage { Raw = string.Empty };
        storage.TryLoad();
        Assert.IsFalse(storage.LastSaveWritten, "До первой записи.");

        storage.Save();
        Assert.IsTrue(storage.LastSaveWritten);

        storage.Accepts = false;
        storage.Save();
        Assert.IsFalse(storage.LastSaveWritten, "Хранилище запись не приняло.");

        var blocked = new MemoryStorage { Raw = Garbage };
        blocked.TryLoad();
        blocked.Save();
        Assert.IsFalse(blocked.LastSaveWritten, "Запись выключена.");
    }

    [Test]
    public void ReadTellsMissingFromUnreadable()
    {
        IgnoreLoggedErrors();

        Assert.IsFalse(SaveDataVersioning.TryReadRaw(string.Empty, out _, out bool emptyUnreadable));
        Assert.IsFalse(emptyUnreadable);

        Assert.IsFalse(SaveDataVersioning.TryReadRaw(Garbage, out PRSaveData data, out bool garbageUnreadable));
        Assert.IsTrue(garbageUnreadable);
        Assert.IsNotNull(data, "Данные для игры есть всегда.");
    }

    /// <summary>
    /// Хранилище по умолчанию: нечитаемая строка в PlayerPrefs переживает запуск и запись.
    /// </summary>
    /// <remarks>
    /// На коде до общей базы тест красный: строка заменялась новым сохранением. Ключ PlayerPrefs возвращается в TearDown.
    /// </remarks>
    [Test]
    public void PlayerPrefsKeepsUnreadableSave()
    {
        IgnoreLoggedErrors();

        PlayerPrefs.SetString(PlayerPrefsKey, Garbage);

        var storage = new PlayerPrefsSaveLoadManager();

        Assert.IsFalse(storage.TryLoad());

        storage.UpdateProjectData(new ProjectData());
        storage.UpdateGameSettings(new GameSettings());
        storage.Save();

        Assert.AreEqual(Garbage, PlayerPrefs.GetString(PlayerPrefsKey, string.Empty));
    }
}
#endif
