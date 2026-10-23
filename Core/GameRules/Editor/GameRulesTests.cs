#if PRSDK_TESTS
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Правила характеристик из настроек проекта: порядок, пропуск пустых, сохранение в ассете.
/// </summary>
public sealed class GameRulesTests
{
    private static readonly Enumeration Stat = Enumeration.GetOrCreate("GameRulesTests_Stat");

    // Реестр статический: после теста возвращаем правила активного проекта.
    [TearDown]
    public void TearDown() => GameRules.Initialize();

    [Test]
    public void Initialize_AppliesLowerPriorityFirst()
    {
        GameRules.Initialize(new StatRuleBase[]
        {
            new MaxValueRule(Stat, 10f, priority: 20),
            new MinValueRule(Stat, 12f, priority: 10),
        });

        Assert.AreEqual(10f, GameRules.ApplyStatRules(Stat, 5f));
        Assert.IsInstanceOf<MinValueRule>(GameRules.GetRules(Stat)[0]);
    }

    [Test]
    public void Initialize_EqualPriority_KeepsListOrder()
    {
        GameRules.Initialize(new StatRuleBase[]
        {
            new MaxValueRule(Stat, 10f),
            new MinValueRule(Stat, 12f),
        });

        Assert.AreEqual(12f, GameRules.ApplyStatRules(Stat, 5f));
    }

    [Test]
    public void Initialize_SkipsEmptyAndStatlessRules()
    {
        LogAssertNoFail(() => GameRules.Initialize(new StatRuleBase[]
        {
            null,
            new MinValueRule(),
            new MinValueRule(Stat, 1f),
        }));

        Assert.AreEqual(1, GameRules.RuleCount);
        Assert.AreEqual(1f, GameRules.ApplyStatRules(Stat, 0f));
    }

    [Test]
    public void SettingsAsset_KeepsRulesThroughSerialization()
    {
        var settings = ScriptableObject.CreateInstance<PRSDKSettings>();
        try
        {
            var serialized = new SerializedObject(settings);
            SerializedProperty list = serialized
                .FindProperty($"<{nameof(PRSDKSettings.StatRules)}>k__BackingField")
                .FindPropertyRelative(GameRulesSettings.RulesFieldName);
            list.arraySize = 2;
            list.GetArrayElementAtIndex(0).managedReferenceValue = new MinValueRule(Stat, 0.5f);
            list.GetArrayElementAtIndex(1).managedReferenceValue = new MaxValueRule(Stat, 3f, priority: 5);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Копия через сериализацию: так же читает ассет билд.
            var copy = Object.Instantiate(settings);
            try
            {
                GameRules.Initialize(copy.StatRules.Rules);
                Assert.AreEqual(2, GameRules.RuleCount);
                Assert.AreEqual(0.5f, GameRules.ApplyStatRules(Stat, 0f));
                Assert.AreEqual(3f, GameRules.ApplyStatRules(Stat, 9f));
                Assert.AreEqual(5, GameRules.GetRules(Stat)[0].Priority);
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }
        finally
        {
            Object.DestroyImmediate(settings);
        }
    }

    private static void LogAssertNoFail(System.Action action)
    {
        // Пропуск пустых правил пишет предупреждения — это ожидаемо, а не ошибка теста.
        bool ignore = UnityEngine.TestTools.LogAssert.ignoreFailingMessages;
        UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
        try
        {
            action();
        }
        finally
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = ignore;
        }
    }
}
#endif
