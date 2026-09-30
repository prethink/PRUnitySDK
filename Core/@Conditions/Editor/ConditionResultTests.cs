#if PRSDK_TESTS
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class ConditionResultTests
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test]
    public void DefaultResult_IsFailedAndEmptyConfigurationsRemainSuccessful()
    {
        Assert.IsTrue(default(ConditionResult).IsFailed);
        Assert.IsNull(default(ConditionResult).FailureDescription);
        Assert.IsTrue(((ICondition)new AllCondition()).Evaluate().IsSuccess);
        Assert.IsTrue(((ICondition)new AnyCondition()).Evaluate().IsSuccess);
        Assert.IsTrue(((ICondition)new NotCondition()).Evaluate().IsSuccess);
        Assert.IsTrue(((ICondition)new AssetCondition()).Evaluate().IsSuccess);
        Assert.IsTrue(((ICondition)new ResourceInlineCondition()).Evaluate().IsSuccess);
    }

    [Test]
    public void All_StopsAtFirstFailureAndKeepsReasonAndContext()
    {
        ConditionContextBase context = ConditionContextEmpty.Instance;
        Func<string[]> args = () => new[] { "5" };
        var first = new ProbeCondition { Result = ConditionResult.Fail(ConditionLabels.Unavailable, args) };
        var second = new ProbeCondition();
        var all = new AllCondition();
        Set(all, "conditions", new List<ICondition> { null, first, second });
        ConditionResult result = all.Evaluate(context);
        Assert.IsTrue(result.IsFailed);
        Assert.AreSame(ConditionLabels.Unavailable, result.FailureReason);
        Assert.AreSame(args, result.FailureReasonArgs);
        Assert.AreSame(context, first.Context);
        Assert.AreEqual(1, first.Calls);
        Assert.AreEqual(0, second.Calls);
    }

    [Test]
    public void Any_StopsOnSuccessAndPreservesFirstReasonWhenAllFail()
    {
        var first = new ProbeCondition { Result = ConditionResult.Fail(ConditionLabels.Requirement(ConditionComparison.Greater)) };
        var success = new ProbeCondition();
        var last = new ProbeCondition();
        var any = new AnyCondition();
        Set(any, "conditions", new List<ICondition> { null, first, success, last });
        Assert.IsTrue(any.Evaluate(ConditionContextEmpty.Instance).IsSuccess);
        Assert.AreEqual(0, last.Calls);
        success.Result = ConditionResult.Fail(ConditionLabels.Unavailable);
        last.Result = ConditionResult.Fail();
        ConditionResult failed = any.Evaluate(ConditionContextEmpty.Instance);
        Assert.IsTrue(failed.IsFailed);
        Assert.AreSame(first.Result.FailureReason, failed.FailureReason);
        Set(any, "conditions", new List<ICondition> { null });
        Assert.IsTrue(any.Evaluate(ConditionContextEmpty.Instance).IsSuccess);
    }

    [Test]
    public void Not_DoesNotReuseInnerRequirementWhenInnerIsSatisfied()
    {
        var inner = new ProbeCondition();
        var inverse = new NotCondition();
        Set(inverse, "condition", inner);
        ConditionResult failure = inverse.Evaluate(ConditionContextEmpty.Instance);
        Assert.IsTrue(failure.IsFailed);
        Assert.AreSame(ConditionLabels.Unavailable, failure.FailureReason);
        inner.Result = ConditionResult.Fail();
        Assert.IsTrue(inverse.Evaluate(ConditionContextEmpty.Instance).IsSuccess);
    }

    [Test]
    public void Description_AndNumberArgumentsAreResolvedOnlyWhenRead()
    {
        int descriptions = 0, values = 0, amount = 5;
        var condition = new ProbeCondition();
        ConditionDescriptions.Register<ProbeCondition>(_ =>
        {
            descriptions++;
            return new ConditionDescription(ConditionLabels.Unavailable,
                () => { values++; return new[] { amount.ToString() }; });
        });
        try
        {
            condition.Result = ConditionResult.Fail(condition, ConditionContextEmpty.Instance);
            ConditionResult result = condition.Evaluate(ConditionContextEmpty.Instance);
            Assert.AreEqual(0, descriptions);
            Assert.AreEqual(0, values);
            ConditionDescription description = result.FailureDescription;
            Assert.AreEqual(1, descriptions);
            Assert.AreEqual("5", description.Args()[0]);
            amount = 6;
            Assert.AreEqual("6", description.Args()[0]);
            Assert.AreEqual(2, values);
        }
        finally { ConditionDescriptions.Unregister<ProbeCondition>(); }
    }

    [Test]
    public void Registry_PreservesTypedFailureAndSupportsBoolRules()
    {
        var registry = new ConditionRegistry();
        var key = new Enumeration("condition_result_test");
        ConditionContextBase received = null;
        var reason = ConditionLabels.Requirement(ConditionComparison.Less);
        registry.Register(key, context =>
        {
            received = context;
            return ConditionResult.Fail(reason);
        });
        ConditionResult result = registry.Evaluate(key, null);
        Assert.IsTrue(result.IsFailed);
        Assert.AreSame(reason, result.FailureReason);
        Assert.AreSame(ConditionContextEmpty.Instance, received);
        registry.Register(key, () => false);
        Assert.IsTrue(registry.Evaluate(key, null).IsFailed);
        registry.Register(key, () => true);
        Assert.IsTrue(registry.Evaluate(key, null).IsSuccess);
        Assert.IsTrue(registry.Evaluate(null, null).IsSuccess);
        registry.Unregister(key);
        Assert.IsFalse(registry.IsRegistered(key));
    }

    [TestCase(ConditionComparison.GreaterOrEqual, false)]
    [TestCase(ConditionComparison.Greater, false)]
    [TestCase(ConditionComparison.Equal, false)]
    [TestCase(ConditionComparison.NotEqual, true)]
    [TestCase(ConditionComparison.LessOrEqual, true)]
    [TestCase(ConditionComparison.Less, true)]
    public void Resource_AssetInlineAndStaticChecksAgree(ConditionComparison comparison, bool expected)
    {
        var resource = ScriptableObject.CreateInstance<ResourceItemDefinition>();
        var asset = ScriptableObject.CreateInstance<ResourceCondition>();
        try
        {
            var inline = new ResourceInlineCondition();
            foreach (object condition in new object[] { asset, inline })
            {
                Set(condition, "resource", resource);
                Set(condition, "comparison", comparison);
                Set(condition, "amount", 1L);
                ConditionResult result = ((ICondition)condition).Evaluate();
                Assert.AreEqual(expected, result.IsSuccess);
                if (!expected)
                {
                    ConditionDescription description = result.FailureDescription;
                    Assert.AreSame(ConditionLabels.Requirement(comparison), description.Text);
                    Assert.AreEqual(NumberConverter.FormatNumber(1L), description.Args()[0]);
                }
            }
            Assert.AreEqual(expected, ResourceCondition.Evaluate(resource, comparison, 1).IsSuccess);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(asset);
            UnityEngine.Object.DestroyImmediate(resource);
        }
    }

    [Test]
    public void AssetAndCollection_PreserveNestedFailure()
    {
        var resource = ScriptableObject.CreateInstance<ResourceItemDefinition>();
        var asset = ScriptableObject.CreateInstance<ResourceCondition>();
        var collection = ScriptableObject.CreateInstance<ConditionCollection>();
        try
        {
            Set(asset, "resource", resource);
            Set(asset, "amount", 10L);
            var wrapper = new AssetCondition();
            Set(wrapper, "condition", asset);
            Set(collection, "conditions", new List<ConditionBase> { null, asset });
            foreach (ICondition condition in new ICondition[] { wrapper, collection })
            {
                ConditionResult result = condition.Evaluate();
                Assert.IsTrue(result.IsFailed);
                Assert.AreSame(ConditionLabels.Requirement(ConditionComparison.GreaterOrEqual), result.FailureReason);
            }
            Set(collection, "match", ConditionMatch.Any);
            Assert.IsTrue(collection.Evaluate().IsFailed);
            Set(collection, "conditions", new List<ConditionBase> { null });
            Assert.IsTrue(collection.Evaluate().IsSuccess);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(collection);
            UnityEngine.Object.DestroyImmediate(asset);
            UnityEngine.Object.DestroyImmediate(resource);
        }
    }

    [Test]
    public void RequirementLabels_AreCachedAndTranslated()
    {
        foreach (ConditionComparison comparison in Enum.GetValues(typeof(ConditionComparison)))
        {
            ILocalizationProvider provider = ConditionLabels.Requirement(comparison);
            Assert.AreSame(provider, ConditionLabels.Requirement(comparison));
            foreach (LangType language in new[] { LangType.English, LangType.Russian, LangType.Turkey })
                Assert.IsFalse(string.IsNullOrWhiteSpace(provider.LocalizationValues[language]));
        }
    }

    private static void Set(object target, string field, object value)
    {
        target.GetType().GetField(field, Fields).SetValue(target, value);
    }

    private class ProbeCondition : ICondition
    {
        public ConditionResult Result = ConditionResult.Success;
        public int Calls;
        public ConditionContextBase Context;
        public ConditionResult Evaluate(ConditionContextBase context)
        {
            Calls++;
            Context = context;
            return Result;
        }
    }
}
#endif
