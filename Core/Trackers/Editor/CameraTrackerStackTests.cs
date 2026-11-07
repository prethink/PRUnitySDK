#if PRSDK_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Стек камер: закрытая камера уходит из стека целиком и не возвращается при восстановлении.
/// </summary>
public class CameraTrackerStackTests
{
    private readonly List<Object> created = new();

    private CameraTracker tracker;

    [SetUp]
    public void SetUp()
    {
        // Свой экземпляр, а не CameraTracker.Instance: проверка не трогает стек живой игры.
        tracker = new CameraTracker();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object item in created)
            Object.DestroyImmediate(item);

        created.Clear();
    }

    [Test]
    public void RemoveFromStack_CameraPushedTwiceAroundAnother_LeavesOnlyTheOther()
    {
        CameraControllerBase first = Create("First");
        CameraControllerBase second = Create("Second");

        // A → B → снова A: например, крупный план открыли, сверху показали другую камеру и вернулись к нему.
        tracker.Push(first);
        tracker.Push(second);
        tracker.Push(first);

        tracker.RemoveFromStack(first);

        Assert.AreSame(second, tracker.Peek());
        Assert.AreEqual(1, tracker.Count, "Закрытая камера не должна остаться в стеке под другой.");
    }

    [Test]
    public void Push_SameCameraAgain_DoesNotGrowStack()
    {
        CameraControllerBase first = Create("First");
        CameraControllerBase second = Create("Second");

        for (int i = 0; i < 3; i++)
        {
            tracker.Push(first);
            tracker.Push(second);
        }

        Assert.AreEqual(2, tracker.Count);
        Assert.AreSame(second, tracker.Peek());
    }

    [Test]
    public void Push_CameraAlreadyBelow_MovesItToTop()
    {
        CameraControllerBase first = Create("First");
        CameraControllerBase second = Create("Second");

        tracker.Push(first);
        tracker.Push(second);
        tracker.Push(first);
        tracker.RemoveFromStack(first);
        tracker.RemoveFromStack(second);

        Assert.IsTrue(tracker.IsEmptyStack());
    }

    private CameraControllerBase Create(string name)
    {
        var gameObject = new GameObject("CameraTrackerStackTests_" + name);
        gameObject.SetActive(false);
        created.Add(gameObject);

        return gameObject.AddComponent<CameraControllerTestDouble>();
    }
}
#endif
