using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Детализация сущности: насколько подробно ей жить, решает то, видно ли её и далеко ли она от камеры.
/// </summary>
/// <remarks>
/// Уровень (<see cref="Level"/>) считает <see cref="EntityLodService"/>. Сам компонент вдали выключает тени
/// мешам под собой, остальное упрощают компоненты сущности (<see cref="IEntityLodListener"/>): аниматор
/// тикает реже, невидимый бот ходит без проверок ступенек и стен.
/// <para>
/// Вешается на то, что движется: у бота — на тело (<c>PlayerController</c>), у NPC и питомца — на корень.
/// NPC, боты и питомцы добавляют его себе сами (<see cref="Ensure"/>); руками ставить нужно только для
/// своих поведений или чтобы задать <see cref="HiddenSimulation"/> в префабе.
/// </para>
/// </remarks>
[DisallowMultipleComponent]
public class EntityLod : PRMonoBehaviour
{
    #region Поля и свойства

    [Tooltip("Как живёт сущность, когда её не видно и она дальше ближней зоны камеры. ProjectDefault — " +
             "из настроек проекта (PRSDKSettings → EntityLod).")]
    [SerializeField] private HiddenSimulation hiddenSimulation = HiddenSimulation.ProjectDefault;

    [Tooltip("Отбрасывать тени только вблизи камеры. Тень дальней сущности почти не видна, а рисуется ещё одним проходом.")]
    [SerializeField] private bool shadowsOnlyNear = true;

    [Tooltip("Радиус сферы, по которой решается, видна ли сущность, в единицах объекта: растёт вместе с его масштабом. " +
             "0 — по мешам: при каждом включении и смене надетого.")]
    [SerializeField, Min(0f)] private float radius;

    [Tooltip("Центр сферы относительно объекта; действует вместе с заданным радиусом.")]
    [SerializeField] private Vector3 center;

    /// <summary>
    /// Сфера по мешам (<see cref="FitToRenderers"/>): заданные в инспекторе поля она не трогает, поэтому подгонка
    /// повторяется и после смены модели, а не только при первом включении.
    /// </summary>
    private Vector3 fittedCenter;

    /// <summary>
    /// Радиус по мешам в единицах объекта: увеличили NPC — сфера растёт вместе с ним без новой подгонки.
    /// </summary>
    private float fittedRadius = 1f;

    /// <summary>
    /// Текущий уровень детализации.
    /// </summary>
    public EntityLodLevel Level { get; private set; } = EntityLodLevel.Full;

    /// <summary>
    /// Поведение упрощено: сущности не видно, она далеко, и упрощать её разрешено.
    /// </summary>
    public bool IsSimplified => Level == EntityLodLevel.Hidden && ResolveHiddenSimulation() == HiddenSimulation.Simplified;

    /// <summary>
    /// Поведение невидимой сущности, заданное ей; <see cref="HiddenSimulation.ProjectDefault"/> — из настроек проекта.
    /// </summary>
    public HiddenSimulation HiddenSimulation => hiddenSimulation;

    /// <summary>
    /// Сфера видимости в мире.
    /// </summary>
    public BoundingSphere WorldSphere => radius > 0f
        ? new BoundingSphere(transform.TransformPoint(center), radius * MaxScale())
        : new BoundingSphere(transform.TransformPoint(fittedCenter), fittedRadius * MaxScale());

    /// <summary>
    /// Наибольший масштаб объекта по осям: сфера должна накрыть его и при растяжении вдоль одной оси.
    /// </summary>
    private float MaxScale()
    {
        Vector3 scale = transform.lossyScale;
        float max = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        return max > 0.0001f ? max : 1f;
    }

    /// <summary>
    /// Место в списке службы; −1 — не зарегистрирован.
    /// </summary>
    internal int ServiceIndex { get; set; } = -1;

    /// <summary>
    /// Кадр, с которого служба верит своим данным о сущности.
    /// </summary>
    /// <remarks>
    /// Видимость <see cref="CullingGroup"/> узнаёт только в отрисовке кадра. До неё у новой сущности
    /// и у той, что переехала на чужое место в списке, данные пустые или чужие: без ожидания она на кадр
    /// становилась бы невидимой.
    /// </remarks>
    internal int SettleFrame { get; set; }

    /// <summary>
    /// Центр сферы в прошлом кадре: служба замечает телепорт и не верит видимости, посчитанной по старому месту.
    /// </summary>
    internal Vector3 LastCenter { get; set; }

    internal bool HasLastCenter { get; set; }

    private readonly List<IEntityLodListener> listeners = new();

    /// <summary>
    /// На чью детализацию подписан слушатель: отписка не ищет её заново (объект могли переложить в другую сущность).
    /// </summary>
    private static readonly Dictionary<IEntityLodListener, EntityLod> subscriptions = new();

    private readonly Dictionary<Renderer, ShadowCastingMode> shadowModes = new();

    private bool shadowsHidden;

    #endregion

    #region MonoBehaviour

    protected override void OnEnable()
    {
        base.OnEnable();

        if (radius <= 0f)
            FitToRenderers();

        HasLastCenter = false;
        RefreshListeners();

        EntityLodService service = EntityLodService.Instance;
        if (service != null)
            service.Register(this);
    }

    /// <summary>
    /// Детализация уничтожена: её записи в общей таблице подписок снимаются, в том числе у выключенных слушателей.
    /// </summary>
    protected override void UnRegisterEventsOnDestroy()
    {
        foreach (IEntityLodListener listener in listeners)
        {
            if (listener != null && subscriptions.TryGetValue(listener, out EntityLod owner) && owner == this)
                subscriptions.Remove(listener);
        }

        listeners.Clear();
        base.UnRegisterEventsOnDestroy();
    }

    protected override void OnDisable()
    {
        if (EntityLodService.HasInstance)
            EntityLodService.Instance.Unregister(this);

        SetLevel(EntityLodLevel.Full);
        base.OnDisable();
    }

    #endregion

    #region Методы

    /// <summary>
    /// Детализация на объекте; если её нет — добавляет.
    /// </summary>
    /// <param name="target">То, что движется: тело бота, корень NPC.</param>
    public static EntityLod Ensure(GameObject target)
    {
        if (target == null)
            return null;

        return target.TryGetComponent(out EntityLod lod) ? lod : target.AddComponent<EntityLod>();
    }

    /// <summary>
    /// Задаёт, как сущность живёт, пока её не видно.
    /// </summary>
    /// <remarks>
    /// Для игры и сцены: одним и тем же ботам в одной игре нужна полная жизнь, в другой — упрощённая.
    /// </remarks>
    public void SetHiddenSimulation(HiddenSimulation value)
    {
        if (hiddenSimulation == value)
            return;

        hiddenSimulation = value;
        Notify();
    }

    /// <summary>
    /// Поведение невидимой сущности с учётом настроек проекта: только <see cref="HiddenSimulation.Full"/>
    /// или <see cref="HiddenSimulation.Simplified"/>.
    /// </summary>
    public HiddenSimulation ResolveHiddenSimulation()
    {
        if (hiddenSimulation != HiddenSimulation.ProjectDefault)
            return hiddenSimulation;

        EntityLodSettings settings = PRUnitySDK.Settings != null ? PRUnitySDK.Settings.EntityLod : null;
        return settings != null ? settings.ResolveHiddenSimulation() : HiddenSimulation.Full;
    }

    /// <summary>
    /// Заново находит компоненты, которые упрощают себя по уровню.
    /// </summary>
    /// <remarks>
    /// Зовётся при включении: подбирает слушателей, включившихся раньше самой детализации (её добавляют сущности
    /// при создании — <see cref="Ensure"/>). Появившиеся позже подписываются сами (<see cref="Subscribe"/>), звать
    /// это для них не нужно.
    /// </remarks>
    public void RefreshListeners()
    {
        listeners.Clear();
        listenerBuffer.Clear();
        GetComponentsInChildren(true, listenerBuffer);

        foreach (IEntityLodListener listener in listenerBuffer)
        {
            // У слушателя во вложенной сущности своя, более близкая детализация: он её, а не этой.
            if (listener is Component component && component.GetComponentInParent<EntityLod>(true) != this)
                continue;

            if (subscriptions.TryGetValue(listener, out EntityLod previous) && previous != this && previous != null)
                previous.listeners.Remove(listener);

            listeners.Add(listener);
            subscriptions[listener] = this;
        }

        listenerBuffer.Clear();
        Notify();
    }

    private static readonly List<IEntityLodListener> listenerBuffer = new();

    /// <summary>
    /// Подписывает компонент на детализацию его сущности и сразу сообщает ему текущий уровень.
    /// </summary>
    /// <remarks>
    /// Зовёт <see cref="PRMonoBehaviour"/> при включении любого <see cref="IEntityLodListener"/>, поэтому слушатель,
    /// появившийся под сущностью позже детализации (новая модель, скин, вещь в руке, объект из пула), получает и
    /// текущий уровень, и все следующие. Раньше слушатели собирались только при включении детализации: аниматор,
    /// созданный после, узнавал уровень один раз и, попав на <see cref="EntityLodLevel.Hidden"/>, так и стоял.
    /// Детализации над компонентом нет — ничего: появившись, она подберёт его сама (<see cref="RefreshListeners"/>).
    /// </remarks>
    /// <param name="owner">Компонент-слушатель: детализация ищется у него и выше.</param>
    /// <param name="listener">Он же как слушатель.</param>
    public static void Subscribe(Component owner, IEntityLodListener listener)
    {
        if (owner == null || owner is EntityLod)
            return;

        EntityLod lod = owner.GetComponentInParent<EntityLod>(true);
        if (lod != null)
            lod.AddListener(listener);
    }

    /// <summary>
    /// Компонент переложили в другую иерархию: подписка переходит к детализации его новой сущности.
    /// </summary>
    /// <remarks>
    /// Зовёт <see cref="PRMonoBehaviour"/> из <c>OnTransformParentChanged</c>: перенос через <c>SetParent</c> не
    /// выключает объект, и без этого слушатель остался бы подписан на прежнюю сущность.
    /// </remarks>
    public static void Resubscribe(Component owner, IEntityLodListener listener)
    {
        if (owner == null || owner is EntityLod)
            return;

        EntityLod lod = owner.GetComponentInParent<EntityLod>(true);
        subscriptions.TryGetValue(listener, out EntityLod current);
        if (lod == current && current != null)
            return;

        Unsubscribe(listener);
        if (lod != null)
            lod.AddListener(listener);
    }

    /// <summary>
    /// Снимает подписку, сделанную <see cref="Subscribe"/> или <see cref="RefreshListeners"/>.
    /// </summary>
    public static void Unsubscribe(IEntityLodListener listener)
    {
        if (listener == null || !subscriptions.TryGetValue(listener, out EntityLod lod))
            return;

        subscriptions.Remove(listener);
        if (lod != null)
            lod.listeners.Remove(listener);
    }

    /// <summary>
    /// Добавляет слушателя (повторно — не дублирует) и сразу сообщает ему текущий уровень.
    /// </summary>
    public void AddListener(IEntityLodListener listener)
    {
        if (listener == null)
            return;

        if (subscriptions.TryGetValue(listener, out EntityLod previous) && previous != this && previous != null)
            previous.listeners.Remove(listener);

        if (!listeners.Contains(listener))
            listeners.Add(listener);

        subscriptions[listener] = this;
        listener.OnEntityLodChanged(this);
    }

    /// <summary>
    /// Меши сущности сменились (надели вещь, сменили модель): сфера подгоняется заново, а пока тени выключены —
    /// выключаются и у новых мешей.
    /// </summary>
    public void RefreshRenderers()
    {
        if (radius <= 0f)
            FitToRenderers();

        if (!shadowsHidden)
            return;

        foreach (Renderer meshRenderer in GetComponentsInChildren<Renderer>(true))
        {
            if (meshRenderer is not (MeshRenderer or SkinnedMeshRenderer)
                || meshRenderer.shadowCastingMode == ShadowCastingMode.Off || shadowModes.ContainsKey(meshRenderer))
                continue;

            shadowModes[meshRenderer] = meshRenderer.shadowCastingMode;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    /// <summary>
    /// Подгоняет сферу видимости под меши сущности. Заданные в инспекторе радиус и центр не меняет.
    /// </summary>
    public void FitToRenderers()
    {
        bool found = false;
        Bounds bounds = default;

        foreach (Renderer meshRenderer in GetComponentsInChildren<Renderer>(true))
        {
            if (meshRenderer is not (MeshRenderer or SkinnedMeshRenderer))
                continue;

            if (!found)
            {
                bounds = meshRenderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(meshRenderer.bounds);
            }
        }

        if (!found)
        {
            fittedCenter = Vector3.zero;
            fittedRadius = 1f;
            return;
        }

        // Границы мешей — в мире, радиус хранится в единицах объекта.
        fittedCenter = transform.InverseTransformPoint(bounds.center);
        fittedRadius = Mathf.Max(0.5f, bounds.extents.magnitude) / MaxScale();
    }

    /// <summary>
    /// Ставит уровень: служба — по видимости и расстоянию, сам компонент — при выключении.
    /// </summary>
    internal void SetLevel(EntityLodLevel level)
    {
        if (Level == level)
            return;

        Level = level;
        ApplyShadows();
        Notify();
    }

    private void Notify()
    {
        for (int index = listeners.Count - 1; index >= 0; index--)
        {
            IEntityLodListener listener = listeners[index];

            if (listener is Object unityObject && unityObject == null)
            {
                listeners.RemoveAt(index);
                continue;
            }

            listener.OnEntityLodChanged(this);
        }
    }

    /// <summary>
    /// Тени только вблизи: дальше мешам под сущностью тени выключаются, вблизи возвращаются прежние.
    /// </summary>
    private void ApplyShadows()
    {
        bool hide = shadowsOnlyNear && Level != EntityLodLevel.Full;

        if (hide == shadowsHidden)
            return;

        shadowsHidden = hide;

        if (!hide)
        {
            foreach (KeyValuePair<Renderer, ShadowCastingMode> pair in shadowModes)
            {
                if (pair.Key != null)
                    pair.Key.shadowCastingMode = pair.Value;
            }

            shadowModes.Clear();
            return;
        }

        foreach (Renderer meshRenderer in GetComponentsInChildren<Renderer>(true))
        {
            if (meshRenderer is not (MeshRenderer or SkinnedMeshRenderer)
                || meshRenderer.shadowCastingMode == ShadowCastingMode.Off)
                continue;

            shadowModes[meshRenderer] = meshRenderer.shadowCastingMode;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    #endregion
}
