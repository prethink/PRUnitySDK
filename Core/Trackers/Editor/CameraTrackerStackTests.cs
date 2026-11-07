#if PRSDK_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Стек камер: закрытая камера уходит из стека целиком и не возвращается при восстановлении.
/// </summary>
public class CameraTrackerStackTests
{
    private readonly List<Object> created = new();

    private CameraTracker tracker;
    private CameraTracker previous;

    [SetUp]
    public void SetUp()
    {
        // Свой экземпляр, а не прежний: проверка не трогает стек живой игры. Контроллер обращается
        // к CameraTracker.Instance, поэтому экземпляр подставляется на время проверки.
        previous = CameraTracker.Instance;
        tracker = new CameraTracker();
        CameraTracker.Override(tracker);
    }

    [TearDown]
    public void TearDown()
    {
        CameraTracker.Override(previous);

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

    [Test]
    public void DestroyingCurrentController_RestoresPlayerCamerasAndClearsCurrent()
    {
        CameraControllerTestDouble focus = Create("Focus");
        var camera = new GameObject("CameraTrackerStackTests_Camera").AddComponent<Camera>();
        created.Add(camera.gameObject);

        // Крупный план включён: он текущий, камеры игрока спрятаны.
        tracker.Push(focus);
        MakeCurrent(focus, camera);
        tracker.HidePlayerCameras();

        focus.DestroyForTest();

        Assert.IsFalse(tracker.IsMainCameraActive, "После уничтожения текущей камеры должны вернуться камеры игрока.");
        Assert.IsTrue(tracker.IsEmptyStack());
        Assert.IsNull(tracker.Current, "Трекер не должен считать текущей камеру уничтоженного контроллера.");
        Assert.IsFalse(focus.IsCurrent);
    }

    [Test]
    public void DestroyingNotCurrentController_LeavesStackAndCameras()
    {
        CameraControllerTestDouble below = Create("Below");
        CameraControllerTestDouble top = Create("Top");

        tracker.Push(below);
        tracker.Push(top);
        tracker.HidePlayerCameras();

        below.DestroyForTest();

        Assert.AreSame(top, tracker.Peek());
        Assert.AreEqual(1, tracker.Count);
        Assert.IsTrue(tracker.IsMainCameraActive, "Уничтожение не текущей камеры ничего не переключает.");
    }

    [Test]
    public void DestroyingCurrentAboveAnother_RestoresTheOne()
    {
        Camera camera = SetSceneCamera();
        CameraControllerTestDouble below = Create("Below");
        CameraControllerTestDouble above = Create("Above");

        // A → B: B текущий поверх A.
        tracker.Push(below);
        tracker.Push(above);
        MakeCurrent(above, camera);

        above.DestroyForTest();

        Assert.AreSame(below, tracker.Peek());
        Assert.AreEqual(1, tracker.Count);
        Assert.IsTrue(below.IsCurrent, "После уничтожения верхней текущей должна стать нижняя.");
        Assert.IsFalse(above.IsCurrent);
        Assert.AreSame(camera, tracker.Current);
        Assert.IsTrue(tracker.IsMainCameraActive);
    }

    [Test]
    public void DestroyingCurrentWithoutRestore_ClearsCurrentOnly()
    {
        Camera camera = SetSceneCamera();
        CameraControllerTestDouble below = Create("Below");
        CameraControllerTestDouble above = Create("Above");

        tracker.Push(below);
        tracker.Push(above);
        MakeCurrent(above, camera);
        tracker.HidePlayerCameras();

        // Выход из игры или выгрузка сцены: переключаться не на кого, но и держать уходящего нельзя.
        above.RestoreOnDestroyOverride = false;
        above.DestroyForTest();

        Assert.IsNull(tracker.Current, "Трекер не должен держать камеру уходящего контроллера.");
        Assert.IsFalse(below.IsCurrent, "Без восстановления нижняя не включается.");
        Assert.AreSame(below, tracker.Peek());
        Assert.IsTrue(tracker.IsMainCameraActive, "Без восстановления камеры не переключаются.");
    }

    [Test]
    public void DestroyingController_WithoutTracker_DoesNotCreateOne()
    {
        CameraControllerTestDouble controller = Create("Lonely");
        CameraTracker.Override(null);

        controller.DestroyForTest();

        Assert.IsFalse(CameraTracker.TryGetInstance(out _), "Уничтожение не должно поднимать трекер заново.");
    }

    /// <summary>
    /// Камера сцены для трекера: восстановление включает именно её.
    /// </summary>
    private Camera SetSceneCamera()
    {
        var camera = new GameObject("CameraTrackerStackTests_SceneCamera").AddComponent<Camera>();
        created.Add(camera.gameObject);

        // SetMainCamera внутренний, как и SetCurrent: тестовая сборка его не видит.
        typeof(CameraTracker).GetProperty(nameof(CameraTracker.MainCamera))
            .GetSetMethod(nonPublic: true)
            .Invoke(tracker, new object[] { camera });

        return camera;
    }

    /// <summary>
    /// Делает контроллер текущим так же, как <c>SetMain</c>, но без камеры сцены и событий.
    /// </summary>
    private void MakeCurrent(CameraControllerBase controller, Camera camera)
    {
        // SetCurrent трекера внутренний: тестовая сборка его не видит.
        typeof(CameraTracker)
            .GetMethod("SetCurrent", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(tracker, new object[] { controller, camera });
    }

    private CameraControllerTestDouble Create(string name)
    {
        var gameObject = new GameObject("CameraTrackerStackTests_" + name);
        gameObject.SetActive(false);
        created.Add(gameObject);

        return gameObject.AddComponent<CameraControllerTestDouble>();
    }
}
#endif
