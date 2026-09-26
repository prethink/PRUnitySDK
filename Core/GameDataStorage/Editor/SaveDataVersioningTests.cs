#if PRSDK_TESTS
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Версия сохранения: преобразование по шагам, новое сохранение и сохранения новее сборки.
/// </summary>
public class SaveDataVersioningTests
{
    /// <summary>
    /// Шаг, который дописывает в сохранение, с какой версии он сработал.
    /// </summary>
    private sealed class MarkMigration : ISaveMigration
    {
        public MarkMigration(int fromVersion)
        {
            FromVersion = fromVersion;
        }

        public int FromVersion { get; }

        public void Migrate(JObject save)
        {
            var marks = (JArray)(save["Marks"] ??= new JArray());
            marks.Add(FromVersion);
        }
    }

    private sealed class FailingMigration : ISaveMigration
    {
        public int FromVersion => 0;

        public void Migrate(JObject save) => throw new InvalidOperationException("broken");
    }

    private static Func<IReadOnlyDictionary<int, ISaveMigration>> Steps(params ISaveMigration[] steps)
    {
        var result = new Dictionary<int, ISaveMigration>();

        foreach (ISaveMigration step in steps)
            result.Add(step.FromVersion, step);

        return () => result;
    }

    [Test]
    public void SaveWithoutVersionIsVersionZero()
    {
        Assert.AreEqual(0, SaveDataVersioning.ReadVersion(JObject.Parse("{\"SaveId\":\"a\"}")));
    }

    [Test]
    public void SameVersionIsLeftUntouched()
    {
        JObject save = JObject.Parse("{\"Version\":2}");

        bool kept = SaveDataVersioning.Upgrade(save, 2, SaveVersionMismatchAction.Convert, Steps(new MarkMigration(1)));

        Assert.IsTrue(kept);
        Assert.IsNull(save["Marks"]);
    }

    [Test]
    public void ConvertRunsStepsInOrderAndSetsVersion()
    {
        JObject save = JObject.Parse("{}");

        bool kept = SaveDataVersioning.Upgrade(save, 3, SaveVersionMismatchAction.Convert,
            Steps(new MarkMigration(2), new MarkMigration(0)));

        Assert.IsTrue(kept);
        CollectionAssert.AreEqual(new[] { 0, 2 }, save["Marks"].ToObject<int[]>());
        Assert.AreEqual(3, SaveDataVersioning.ReadVersion(save));
    }

    [Test]
    public void ConvertWithoutStepsOnlyRaisesVersion()
    {
        JObject save = JObject.Parse("{\"Version\":1}");

        Assert.IsTrue(SaveDataVersioning.Upgrade(save, 4, SaveVersionMismatchAction.Convert, Steps()));
        Assert.AreEqual(4, SaveDataVersioning.ReadVersion(save));
    }

    [Test]
    public void FailedStepDiscardsSave()
    {
        LogAssert.Expect(LogType.Error, new Regex("FailingMigration"));

        Assert.IsFalse(SaveDataVersioning.Upgrade(JObject.Parse("{}"), 1, SaveVersionMismatchAction.Convert, Steps(new FailingMigration())));
    }

    [Test]
    public void KeepLoadsOlderSaveWithoutSteps()
    {
        JObject save = JObject.Parse("{\"Version\":1}");

        bool kept = SaveDataVersioning.Upgrade(save, 3, SaveVersionMismatchAction.Keep,
            Steps(new MarkMigration(1), new MarkMigration(2)));

        Assert.IsTrue(kept);
        Assert.IsNull(save["Marks"]);
        Assert.AreEqual(3, SaveDataVersioning.ReadVersion(save));
    }

    [Test]
    public void StartNewDiscardsOlderSave()
    {
        Assert.IsFalse(SaveDataVersioning.Upgrade(JObject.Parse("{\"Version\":1}"), 2, SaveVersionMismatchAction.StartNew, Steps()));
    }

    [Test]
    public void NewerSaveIsKeptEvenWithStartNew()
    {
        JObject save = JObject.Parse("{\"Version\":5}");

        Assert.IsTrue(SaveDataVersioning.Upgrade(save, 2, SaveVersionMismatchAction.StartNew, Steps()));
        Assert.AreEqual(5, SaveDataVersioning.ReadVersion(save));
    }

    [Test]
    public void ParseKeepsDatesAsWrittenAndDecimalsExact()
    {
        JObject save = SaveDataVersioning.Parse("{\"SaveDate\":\"2026-09-26T16:14:04.401+07:00\",\"Score\":79228162514264337593543950.335}");

        Assert.AreEqual(JTokenType.String, save["SaveDate"].Type);
        Assert.AreEqual("2026-09-26T16:14:04.401+07:00", save["SaveDate"].Value<string>());
        Assert.AreEqual(79228162514264337593543950.335m, save["Score"].Value<decimal>());
    }
}
#endif
