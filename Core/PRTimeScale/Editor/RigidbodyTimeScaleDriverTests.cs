#if PRSDK_TESTS
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Драйвер тела: заморозка и разморозка дают ту же скорость, что прямая смена масштаба, а выключенный драйвер
/// смен масштаба не замечает.
/// </summary>
/// <remarks>
/// В редакторе Awake и OnEnable драйвера не вызываются, поэтому масштаб применяется вызовом
/// <see cref="RigidbodyTimeScaleDriverBase.ApplyScaleChange"/>, как это делает событие.
/// </remarks>
public class RigidbodyTimeScaleDriverTests
{
    private PRTimeScale previous;
    private PRTimeScale timeScale;
    private GameObject gameObject;
    private Rigidbody body;
    private RigidbodyTimeScaleDriver driver;

    [SetUp]
    public void SetUp()
    {
        previous = PRTimeScale.Instance;
        timeScale = new PRTimeScale();
        timeScale.SingletonInitialize();
        PRTimeScale.Override(timeScale);

        gameObject = new GameObject("RigidbodyTimeScaleDriverTests");
        body = gameObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        driver = gameObject.AddComponent<RigidbodyTimeScaleDriver>();
    }

    [TearDown]
    public void TearDown()
    {
        // Снять с PRUpdateRunner: OnEnable из теста ставит драйвер в его статические списки.
        Call("OnDisable");
        Object.DestroyImmediate(gameObject);
        EventBus.Unsubscribe(timeScale);
        PRTimeScale.Override(previous);
    }

    [TestCase(false, 10f)]
    [TestCase(true, 2.5f)]
    public void FreezeAfterSlowdown_ThawsLikeDirectScaleChange(bool scaleVelocityOnChange, float expected)
    {
        Setup(scaleVelocityOnChange);
        body.velocity = Vector3.forward * 10f;

        var slow = timeScale.AddModifier(0.5f, TimeLayer.Enemy);
        driver.ApplyScaleChange();

        var freeze = timeScale.AddModifier(0f, TimeLayer.Enemy);
        driver.ApplyScaleChange();
        Assert.IsTrue(driver.IsFrozen);
        Assert.AreEqual(0f, body.velocity.magnitude, 1e-4f);

        timeScale.RemoveModifier(freeze);
        timeScale.RemoveModifier(slow);
        timeScale.AddModifier(0.25f, TimeLayer.Enemy);
        driver.ApplyScaleChange();

        // Как прямая смена 0.5 → 0.25: без пересчёта скорость прежняя, с пересчётом вдвое меньше той, что была при 0.5.
        Assert.IsFalse(driver.IsFrozen);
        Assert.AreEqual(expected, body.velocity.z, 1e-4f);
    }

    [Test]
    public void PushWhileFrozen_WithoutRescale_AddsInNewTempo()
    {
        Setup(scaleVelocityOnChange: false);
        body.velocity = Vector3.forward * 10f;

        var freeze = timeScale.AddModifier(0f, TimeLayer.Enemy);
        driver.ApplyScaleChange();
        driver.AddScaledForce(Vector3.up * 4f, ForceMode.VelocityChange);

        timeScale.RemoveModifier(freeze);
        timeScale.AddModifier(0.5f, TimeLayer.Enemy);
        driver.ApplyScaleChange();

        // Скорость, с которой замерло, возвращается как есть; толчок за время заморозки — в новом темпе.
        Assert.AreEqual(10f, body.velocity.z, 1e-4f);
        Assert.AreEqual(2f, body.velocity.y, 1e-4f);
    }

    [Test]
    public void SetScaledVelocityWhileFrozen_ReplacesKeptVelocity()
    {
        Setup(scaleVelocityOnChange: false);
        body.velocity = Vector3.forward * 10f;

        var freeze = timeScale.AddModifier(0f, TimeLayer.Enemy);
        driver.ApplyScaleChange();
        driver.SetScaledVelocity(Vector3.up * 4f);

        timeScale.RemoveModifier(freeze);
        driver.ApplyScaleChange();

        Assert.AreEqual(Vector3.up * 4f, body.velocity);
    }

    [Test]
    public void DisabledDriver_IgnoresScaleChangeEvent()
    {
        Setup(scaleVelocityOnChange: true);
        driver.enabled = false;
        body.velocity = Vector3.forward * 10f;

        timeScale.AddModifier(0f, TimeLayer.Enemy);
        driver.OnPRTimeScaleChange(TimeLayer.Enemy);

        // Тело в пуле не фиксируется и скорость ему не пишется: масштаб драйвер возьмёт при включении.
        Assert.IsFalse(driver.IsFrozen);
        Assert.AreEqual(RigidbodyConstraints.None, body.constraints);
        Assert.AreEqual(10f, body.velocity.z, 1e-4f);
    }

    [TestCase(false, 7f)]
    [TestCase(true, 3.5f)]
    public void PoolReuse_SpawnedWhileFrozen_GetsSpawnVelocityAfterThaw(bool scaleVelocityOnChange, float expected)
    {
        Setup(scaleVelocityOnChange);
        Call("OnEnable");
        body.velocity = Vector3.forward * 10f;
        var freeze = timeScale.AddModifier(0f, TimeLayer.Enemy);
        driver.ApplyScaleChange();

        // Ушёл в пул посреди заморозки: тело отпущено, скорость прошлой жизни забыта.
        driver.enabled = false;
        Call("OnDisable");
        Assert.AreEqual(RigidbodyConstraints.None, body.constraints);
        driver.OnPRTimeScaleChange(TimeLayer.Enemy);

        // Код появления задал скорость и включил объект — заморозка ещё идёт.
        body.velocity = Vector3.forward * 7f;
        driver.enabled = true;
        Call("OnEnable");
        Assert.IsTrue(driver.IsFrozen);
        Assert.AreEqual(RigidbodyConstraints.FreezeAll, body.constraints);

        Call("PRFixedUpdate");
        Assert.AreEqual(0f, body.velocity.magnitude, 1e-4f);

        timeScale.RemoveModifier(freeze);
        timeScale.AddModifier(0.5f, TimeLayer.Enemy);
        driver.ApplyScaleChange();

        // Без пересчёта скорость появления возвращается как есть, с пересчётом — в новом темпе.
        Assert.AreEqual(RigidbodyConstraints.None, body.constraints);
        Assert.AreEqual(expected, body.velocity.z, 1e-4f);
    }

    [Test]
    public void PoolReuse_ThawedWhileInPool_KeepsSpawnVelocity()
    {
        Setup(scaleVelocityOnChange: true);
        Call("OnEnable");
        var freeze = timeScale.AddModifier(0f, TimeLayer.Enemy);
        driver.ApplyScaleChange();

        driver.enabled = false;
        Call("OnDisable");

        // Заморозку сняли, пока объект в пуле: выключенный драйвер этого не видит.
        timeScale.RemoveModifier(freeze);
        timeScale.AddModifier(0.5f, TimeLayer.Enemy);
        driver.OnPRTimeScaleChange(TimeLayer.Enemy);

        body.velocity = Vector3.forward * 7f;
        driver.enabled = true;
        Call("OnEnable");

        // Включённый берёт текущий темп как новый: скорость появления не затёрта и не пересчитана.
        Assert.IsFalse(driver.IsFrozen);
        Assert.AreEqual(RigidbodyConstraints.None, body.constraints);
        Assert.AreEqual(7f, body.velocity.z, 1e-4f);
    }

    /// <summary>
    /// В редакторе Unity не зовёт OnEnable, OnDisable и PRFixedUpdate у обычных скриптов — тест зовёт их сам.
    /// </summary>
    private void Call(string method)
    {
        typeof(RigidbodyTimeScaleDriverBase)
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(driver, null);
    }

    private void Setup(bool scaleVelocityOnChange)
    {
        var serialized = new SerializedObject(driver);
        serialized.FindProperty("body").objectReferenceValue = body;
        serialized.FindProperty("scaleVelocityOnChange").boolValue = scaleVelocityOnChange;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        driver.SetTimeLayers(TimeLayer.Enemy);
    }
}
#endif
