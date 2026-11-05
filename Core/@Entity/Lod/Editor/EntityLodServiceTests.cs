#if PRSDK_TESTS
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Уровни детализации по настоящей отрисовке камеры: вблизи — полный, вдали в кадре — сниженный,
/// вдали за спиной — скрытый, в том числе при нулевой ближней зоне.
/// </summary>
/// <remarks>
/// <para>
/// Видимость <see cref="CullingGroup"/> узнаёт только в отрисовке, поэтому тест рисует свою камеру
/// (<see cref="Camera.Render"/> работает и в редакторе) и затем раздаёт уровни — как кадр игры: отрисовка, потом
/// <see cref="EntityLodService"/>. Служба и детализация созданы в редакторе, их OnEnable не вызывается: сущности
/// регистрируются вручную, а ожидание первого кадра (<c>SettleFrame</c>) снимается — кадры в тесте не идут.
/// </para>
/// <para>
/// Ловит регресс 05.11.2026: с одной полосой расстояний <see cref="CullingGroup"/> считал всё дальше ближней зоны
/// невидимым, и NPC в кадре вдали замирали.
/// </para>
/// </remarks>
public class EntityLodServiceTests
{
    private const BindingFlags Internal = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private GameObject root;
    private Camera camera;
    private RenderTexture target;
    private EntityLodService service;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("EntityLodServiceTests") { hideFlags = HideFlags.HideAndDontSave };

        var cameraObject = new GameObject("Camera") { hideFlags = HideFlags.HideAndDontSave };
        cameraObject.transform.SetParent(root.transform, false);
        // Далеко от карты открытой сцены: её стены не закроют сферы.
        cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 5000f, 0f), Quaternion.identity);
        camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.farClipPlane = 500f;
        camera.useOcclusionCulling = false;
        target = new RenderTexture(64, 64, 16);
        camera.targetTexture = target;

        service = root.AddComponent<EntityLodService>();
    }

    [TearDown]
    public void TearDown()
    {
        var group = (CullingGroup)typeof(EntityLodService).GetField("group", Internal).GetValue(service);
        group?.Dispose();
        camera.targetTexture = null;
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(target);
    }

    [Test]
    public void Levels_ByDistanceAndView()
    {
        EntityLod near = Entity(new Vector3(0f, 0f, 8f));
        EntityLod farInView = Entity(new Vector3(0f, 0f, 40f));
        EntityLod farInViewEdge = Entity(new Vector3(8f, 0f, 30f));
        EntityLod farBehind = Entity(new Vector3(0f, 0f, -40f));
        EntityLod nearBehind = Entity(new Vector3(0f, 0f, -8f));

        RenderAndEvaluate(15f);

        Assert.AreEqual(EntityLodLevel.Full, near.Level, "вблизи в кадре");
        Assert.AreEqual(EntityLodLevel.Reduced, farInView.Level, "в кадре на 40 м");
        Assert.AreEqual(EntityLodLevel.Reduced, farInViewEdge.Level, "в кадре сбоку на 30 м");
        Assert.AreEqual(EntityLodLevel.Hidden, farBehind.Level, "за спиной на 40 м");
        Assert.AreEqual(EntityLodLevel.Full, nearBehind.Level, "вблизи за спиной");
    }

    [Test]
    public void ZeroNearDistance_StillSplitsInViewAndBehind()
    {
        EntityLod inView = Entity(new Vector3(0f, 0f, 30f));
        EntityLod behind = Entity(new Vector3(0f, 0f, -30f));

        RenderAndEvaluate(0f);

        Assert.AreEqual(EntityLodLevel.Reduced, inView.Level, "в кадре при нулевой ближней зоне");
        Assert.AreEqual(EntityLodLevel.Hidden, behind.Level, "за спиной при нулевой ближней зоне");
    }

    /// <summary>
    /// Сущность радиусом 1 м в точке относительно камеры.
    /// </summary>
    private EntityLod Entity(Vector3 offset)
    {
        var go = new GameObject("Entity") { hideFlags = HideFlags.HideAndDontSave };
        go.transform.SetParent(root.transform, false);
        go.transform.position = camera.transform.position + offset;
        EntityLod lod = go.AddComponent<EntityLod>();
        typeof(EntityLodService).GetMethod("Register", Internal).Invoke(service, new object[] { lod });
        return lod;
    }

    /// <summary>
    /// Кадр игры: служба кладёт сферы и привязывает камеру, камера рисует, служба раздаёт уровни по отрисовке.
    /// </summary>
    private void RenderAndEvaluate(float nearDistance)
    {
        MethodInfo evaluate = typeof(EntityLodService).GetMethod("Evaluate", Internal);
        evaluate.Invoke(service, new object[] { camera, nearDistance });

        PropertyInfo settle = typeof(EntityLod).GetProperty("SettleFrame", Internal);
        foreach (EntityLod lod in root.GetComponentsInChildren<EntityLod>())
            settle.SetValue(lod, 0);

        camera.Render();
        evaluate.Invoke(service, new object[] { camera, nearDistance });
    }
}
#endif
