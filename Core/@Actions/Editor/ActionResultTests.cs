#if PRSDK_TESTS
using System;
using System.Collections.Generic;
using NUnit.Framework;

public class ActionResultTests
{
    [Test]
    public void DefaultResult_IsFailedWithoutReason()
    {
        ActionResult result = default;
        Assert.IsTrue(result.IsFailed);
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNull(result.FailureReason);
        Assert.IsNull(result.FailureReasonArgs);
        Assert.IsTrue(ActionResult.Success.IsSuccess);
        Assert.IsNull(ActionResult.Success.FailureReason);
    }

    [Test]
    public void Executor_PreservesRefusalAndDoesNotExecute()
    {
        int checks = 0;
        int executions = 0;
        Func<string[]> args = () => new[] { "5" };
        ActionResult refusal = ActionResult.Fail(ActionLabels.InvalidAmount, args);
        ActionResult result = new ActionExecuter().Execute(
            () => { checks++; return refusal; },
            () => { executions++; return ActionResult.Success; });
        Assert.AreEqual(1, checks);
        Assert.AreEqual(0, executions);
        Assert.IsTrue(result.IsFailed);
        Assert.AreSame(refusal.FailureReason, result.FailureReason);
        Assert.AreSame(args, result.FailureReasonArgs);
    }

    [Test]
    public void Executor_PreservesExecutionFailure()
    {
        int executions = 0;
        ActionResult result = new ActionExecuter().Execute(
            () => ActionResult.Success,
            () => { executions++; return ActionResult.Fail(ActionLabels.Unavailable); });
        Assert.AreEqual(1, executions);
        Assert.IsTrue(result.IsFailed);
        Assert.AreSame(ActionLabels.Unavailable, result.FailureReason);
    }

    [Test]
    public void Executor_PropagatesExceptionsAndRejectsNullDelegates()
    {
        var executor = new ActionExecuter();
        Assert.Throws<InvalidOperationException>(() => executor.Execute(
            () => ActionResult.Success, () => throw new InvalidOperationException()));
        Assert.Throws<ArgumentNullException>(() => executor.Execute(null, () => ActionResult.Success));
        Assert.Throws<ArgumentNullException>(() => executor.Execute(() => ActionResult.Success, null));
    }

    [Test]
    public void InlineExecution_UsesVirtualCheckOnEveryCall()
    {
        var action = new ProbeAction();
        Assert.IsTrue(action.CanExecute().IsSuccess);
        action.Availability = ActionResult.Fail(ActionLabels.MissingResource);
        ActionResult result = action.Execute();
        Assert.IsTrue(result.IsFailed);
        Assert.AreSame(ActionLabels.MissingResource, result.FailureReason);
        Assert.AreEqual(0, action.Executions);
        action.Availability = ActionResult.Success;
        Assert.IsTrue(action.Execute().IsSuccess);
        Assert.AreEqual(1, action.Executions);
    }

    [Test]
    public void Sequence_StopsAndKeepsFailureAfterPartialExecution()
    {
        var first = new ProbeAction();
        var failed = new ProbeAction { Outcome = ActionResult.Fail(ActionLabels.InvalidUrl) };
        var last = new ProbeAction();
        int count = 0;
        ActionResult result = ActionSequence.Execute(new[] { first, failed, last }, true, ref count);
        Assert.IsTrue(result.IsFailed);
        Assert.AreSame(ActionLabels.InvalidUrl, result.FailureReason);
        Assert.AreEqual(1, count);
        Assert.AreEqual(1, first.Executions);
        Assert.AreEqual(1, failed.Executions);
        Assert.AreEqual(0, last.Executions);
    }

    [Test]
    public void Sequence_ContinuesAndReportsPartialSuccess()
    {
        var failed = new ProbeAction { Availability = ActionResult.Fail(ActionLabels.InvalidUrl) };
        var success = new ProbeAction();
        int count = 0;
        ActionResult result = ActionSequence.Execute(new ProbeAction[] { failed, null, success }, false, ref count);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, count);
        Assert.AreEqual(0, failed.Executions);
        Assert.AreEqual(1, success.Executions);
        Assert.IsTrue(ActionSequence.CanExecuteAny(new[] { failed, success }));
    }

    [Test]
    public void Sequence_AllFailPreservesFirstReasonAndPreviousCount()
    {
        var first = new ProbeAction { Availability = ActionResult.Fail(ActionLabels.InvalidUrl) };
        var second = new ProbeAction { Availability = ActionResult.Fail(ActionLabels.MissingAction) };
        int count = 2;
        ActionResult result = ActionSequence.Execute(new[] { first, second }, false, ref count);
        Assert.IsTrue(result.IsFailed);
        Assert.AreSame(ActionLabels.InvalidUrl, result.FailureReason);
        Assert.AreEqual(2, count);
        Assert.IsFalse(ActionSequence.CanExecuteAny(new[] { first, second }));
    }

    [Test]
    public void FailureArguments_AreLazyAndCanBeRefreshed()
    {
        int value = 1;
        int reads = 0;
        Func<string[]> args = () => { reads++; return new[] { value.ToString() }; };
        ActionResult result = new ActionExecuter().Execute(
            () => ActionResult.Fail(ActionLabels.InvalidAmount, args),
            () => ActionResult.Success);
        Assert.AreEqual(0, reads);
        Assert.AreEqual("1", result.FailureReasonArgs()[0]);
        value = 2;
        Assert.AreEqual("2", result.FailureReasonArgs()[0]);
        Assert.AreEqual(2, reads);
    }

    [Test]
    public void Labels_HaveUniqueKeysAndThreeLanguages()
    {
        var keys = new HashSet<string>();
        foreach (var field in typeof(ActionLabels).GetFields())
        {
            var provider = (ILocalizationProvider)field.GetValue(null);
            Assert.IsTrue(keys.Add(provider.LocalizationKey));
            foreach (LangType language in new[] { LangType.English, LangType.Russian, LangType.Turkey })
            {
                Assert.IsTrue(provider.LocalizationValues.TryGetValue(language, out string text));
                Assert.IsFalse(string.IsNullOrWhiteSpace(text));
            }
        }
    }

    [Test]
    public void Container_PreservesNestedExecutionFailure()
    {
        var container = UnityEngine.ScriptableObject.CreateInstance<ActionResultTestContainer>();
        try
        {
            var nested = new ProbeAction { Outcome = ActionResult.Fail(ActionLabels.InvalidUrl) };
            typeof(InlineActionContainer).GetField("action",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(container, nested);
            ActionResult result = container.Execute();
            Assert.IsTrue(result.IsFailed);
            Assert.AreSame(ActionLabels.InvalidUrl, result.FailureReason);
            Assert.AreEqual(1, nested.Executions);
        }
        finally { UnityEngine.Object.DestroyImmediate(container); }
    }

    [Test]
    public void Pipeline_ReportsNestedRefusalAndClearsCountOnRejectedCheck()
    {
        var pipeline = UnityEngine.ScriptableObject.CreateInstance<ActionResultTestPipeline>();
        try
        {
            var nested = new ProbeAction { Outcome = ActionResult.Fail(ActionLabels.InvalidUrl) };
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(InlineActionPipeline).GetField("actions", flags).SetValue(pipeline, new List<IAction> { nested });
            ActionResult refused = pipeline.Execute();
            Assert.IsTrue(refused.IsFailed);
            Assert.AreSame(ActionLabels.InvalidUrl, refused.FailureReason);
            Assert.AreEqual(0, pipeline.LastExecutedCount);

            nested.Outcome = ActionResult.Success;
            Assert.IsTrue(pipeline.Execute().IsSuccess);
            Assert.AreEqual(1, pipeline.LastExecutedCount);
            pipeline.Availability = ActionResult.Fail(ActionLabels.GameNotReady);
            Assert.IsTrue(pipeline.Execute().IsFailed);
            Assert.AreEqual(0, pipeline.LastExecutedCount);
        }
        finally { UnityEngine.Object.DestroyImmediate(pipeline); }
    }

    private class ProbeAction : InlineActionBase
    {
        public ActionResult Availability = ActionResult.Success;
        public ActionResult Outcome = ActionResult.Success;
        public int Executions;
        public override ActionResult CanExecute() => Availability;
        protected override ActionResult Action() { Executions++; return Outcome; }
    }
}
#endif
