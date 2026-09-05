using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Хранит состояние объекта сцены между запусками игры.
/// </summary>
/// <remarks>
/// Решает, в каком виде объект появится: если о нём есть запись в сохранении, применяется
/// она, иначе берутся значения из инспектора.
/// <para>
/// Из коробки хранится только активность. Остальное добавляют наследники через
/// <see cref="CaptureState"/> и <see cref="ApplyState"/>, складывая значения
/// в <see cref="SceneObjectState"/> по ключам <see cref="EnumerationType{T}"/>.
/// </para>
/// <para>
/// Выключенный объект не получает <c>Awake</c> и сам себя не включит. Если объект может
/// оказаться выключенным, вешайте компонент на родителя или соседа и указывайте объект
/// в поле цели.
/// </para>
/// </remarks>
public class SaveableObjectState : PRMonoBehaviour
{
    [Header("Объект")]
    [Tooltip("Чьё состояние храним. Пусто — объект самого компонента.")]
    [SerializeField] protected GameObject target;

    [Header("Ключ")]
    [Tooltip("Откуда берётся ключ сохранения.")]
    [SerializeField] protected SaveableIdSource idSource = SaveableIdSource.OwnId;

    [Tooltip("Ключ этого объекта. Заполняется сам при добавлении компонента.")]
    [SerializeField] protected string ownId;

    /// <summary>
    /// Объект, которому ключ был выдан.
    /// </summary>
    /// <remarks>
    /// Хранится, чтобы отличить копию от оригинала. Отдельного вызова «вас скопировали»
    /// в Unity нет, зато у копии другой <c>GlobalObjectId</c>: он у объекта свой
    /// и при копировании не наследуется. Расхождение с записанным здесь и означает,
    /// что перед нами копия.
    /// </remarks>
    [SerializeField, HideInInspector] protected string idOwner;

    [Tooltip("Взять группу из набора, а не из типа сущности.")]
    [SerializeField] protected bool overrideEntityGroup;

    [Tooltip("Группа для подсчёта прогресса. Пусто — объект в подсчёт по группе не попадает.")]
    [SerializeField] protected EnumerationReference<ObjectStateGroupEnumerations> group = new();

    [Header("Значения по умолчанию")]
    [Tooltip("Хранить активность объекта. Выключите, если наследник хранит только своё.")]
    [SerializeField] protected bool saveActiveState = true;

    [Tooltip("Каким объект появляется, когда записи о нём ещё нет.")]
    [SerializeField] protected bool defaultIsActive = true;

    /// <summary>
    /// Кто уже занял ключ. Только для проверки на совпадения в текущем запуске.
    /// </summary>
    private static readonly Dictionary<string, SaveableObjectState> UsedIds = new();

#if UNITY_EDITOR
    [System.NonSerialized] private bool isIdCheckQueued;
#endif

    private Enumeration resolvedGroup;
    private string resolvedId;
    private bool isStateApplied;
    private bool isSubscribed;

    /// <summary>
    /// Объект, чьё состояние хранится.
    /// </summary>
    public GameObject Target => target != null ? target : gameObject;

    /// <summary>
    /// Ключ, под которым состояние лежит в сохранении.
    /// </summary>
    public string StateId => resolvedId;

    /// <summary>
    /// Группа, в которой объект считают вместе с остальными.
    /// </summary>
    /// <remarks>
    /// У сущности группа по умолчанию — её <c>EntityType</c>: классификация у неё уже
    /// есть, и заводить рядом вторую только ради подсчёта незачем. Когда сущности одного
    /// типа нужно считать порознь, это переключают галкой, и группа берётся из набора.
    /// Объект, сущностью не являющийся, всегда берёт группу из набора.
    /// </remarks>
    /// <remarks>
    /// Считается один раз и запоминается: группу спрашивают в цикле по всем состояниям
    /// уровня — при подсчёте прогресса и при снимке отладчика, — а поиск сущности идёт
    /// по родителям через <c>GetComponentInParent</c>. На россыпи подбираемых предметов
    /// это сотни обходов иерархии на один запрос.
    /// </remarks>
    public Enumeration Group => resolvedGroup ??= ResolveGroup();

    private Enumeration ResolveGroup()
    {
        if (!overrideEntityGroup && TryGetEntityType(out Enumeration entityType))
            return entityType;

        // Статическая форма: она сама разбирается и с пустым значением,
        // и с отсутствующей ссылкой, отдавая значение по умолчанию набора.
        return EnumerationReference<ObjectStateGroupEnumerations>.ToEnumeration(group);
    }

    /// <summary>
    /// Объект — сущность, и его тип можно взять как группу.
    /// </summary>
    public bool HasEntityType => TryGetEntityType(out _);

    /// <summary>
    /// Тип сущности, если объект ею является.
    /// </summary>
    /// <remarks>
    /// Ищем в родителях: ссылка на сущность обычно висит выше по иерархии,
    /// а не на том же объекте, что и состояние.
    /// </remarks>
    private bool TryGetEntityType(out Enumeration entityType)
    {
        entityType = null;

        GameObject targetObject = Target;

        if (targetObject == null)
            return false;

        EntityLinkBase link = targetObject.GetComponentInParent<EntityLinkBase>();

        if (link == null || link.Entity == null)
            return false;

        entityType = link.Entity.EntityType;
        return entityType != null;
    }

    #region Жизненный цикл

    protected override void InitializationComponents()
    {
        // Ключ считаем до базового вызова: он поднимает RegisterEventsOnCreated,
        // а тот ставит состояние на учёт в трекере — с пустым ключом оно не попало бы
        // в поиск по ключу.
        resolvedId = ResolveId();
        RegisterId();

        base.InitializationComponents();

        // Значения по умолчанию ставим сразу, не дожидаясь данных: иначе объект
        // успел бы мелькнуть в том виде, в каком лежит на сцене.
        ApplyDefaults();

        // Подписка идёт после умолчаний, чтобы сохранённое перекрывало их, а не наоборот:
        // готовый сигнал вызывает подписку сразу же.
        TrySubscribe();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Запасной путь для случая, когда в <c>Awake</c> менеджера ещё не было. Обычно
    /// подписка уже сделана, и здесь ничего не происходит.
    /// </remarks>
    protected override void Start()
    {
        base.Start();

        if (!TrySubscribe())
            PRLog.WriteWarning(this, $"Состояние [{resolvedId}] не восстановлено: GameManager недоступен.");
    }

    /// <summary>
    /// Просит сообщить, когда данные проекта будут прочитаны.
    /// </summary>
    /// <remarks>
    /// Подписываемся в <c>Awake</c>, а не в <c>Start</c>, из-за одного неочевидного
    /// случая: объект, который по умолчанию выключен, выключает сам себя — и <c>Start</c>
    /// у него уже не вызовется. Подписаться он обязан до этого, иначе сохранённое
    /// состояние не применится никогда. <c>SetActive(false)</c> не прерывает начатый
    /// <c>Awake</c>, а сигнал готовности — обычный обратный вызов, не сообщение Unity,
    /// поэтому выключенный объект его получит и сможет включиться обратно.
    /// </remarks>
    private bool TrySubscribe()
    {
        if (isSubscribed)
            return true;

        if (!GameManager.HasInstance)
            return false;

        isSubscribed = true;
        GameManager.Instance.ReadySignal.SubscribeOnReady(ApplySavedState);

        return true;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Ключ к этому моменту уже вычислен в <c>InitializationComponents</c>: регистрация
    /// идёт после Awake, и трекер получает состояние сразу с ключом.
    /// </remarks>
    protected override void RegisterEventsOnCreated()
    {
        base.RegisterEventsOnCreated();
        PRUnitySDK.Trackers.ObjectStates.Add(this);
    }

    protected override void UnRegisterEventsOnDestroy()
    {
        PRUnitySDK.Trackers.ObjectStates.Remove(this);


        // Ключ освобождаем до базового вызова: тот снимет объект с учёта сохранения,
        // и с этого момента компонент считается ушедшим со сцены.
        if (!string.IsNullOrEmpty(resolvedId) &&
            UsedIds.TryGetValue(resolvedId, out SaveableObjectState owner) &&
            owner == this)
        {
            UsedIds.Remove(resolvedId);
        }

        base.UnRegisterEventsOnDestroy();
    }

    /// <summary>
    /// Значения при добавлении компонента.
    /// </summary>
    /// <remarks>
    /// Цель проставляем сразу: почти всегда хранят состояние того объекта, на который
    /// компонент и повесили, а пустое поле в инспекторе выглядит как забытая настройка.
    /// </remarks>
    protected virtual void Reset()
    {
        target = gameObject;
        ownId = CreateId();
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        // Дальше — только правка данных в редакторе. В игре менять эти поля нельзя:
        // сгенерированный в рантайме ключ никуда не сохранится, и при следующем
        // запуске окажется другим.
        if (Application.isPlaying)
            return;

        // Правки в инспекторе во время игры должны быть видны: группа считается один раз
        // и без сброса показывала бы прежнее значение до перезапуска сцены.
        resolvedGroup = null;

        if (target == null)
            target = gameObject;

        if (idSource != SaveableIdSource.OwnId)
            return;

        // Ключ, выданный до перехода на формат с дефисами, приводим к нему. Значение
        // остаётся тем же самым идентификатором, меняется только запись.
        if (!string.IsNullOrEmpty(ownId) && Guid.TryParse(ownId, out Guid parsed))
            ownId = parsed.ToString("D");

#if UNITY_EDITOR
        // Откладываем: во время загрузки сцены объект ещё не получил постоянного
        // адреса, и спрашивать его рано.
        // Одна постановка в очередь на компонент: OnValidate приходит всем объектам
        // при загрузке сцены, а EnsureOwnId зовёт GetGlobalObjectIdSlow — сотни
        // дублей этого вызова заметно тормозят открытие сцены.
        if (isIdCheckQueued)
            return;

        isIdCheckQueued = true;
        UnityEditor.EditorApplication.delayCall += EnsureOwnId;
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Выдаёт ключ, если его нет, и перевыдаёт копии.
    /// </summary>
    /// <remarks>
    /// Уведомления о копировании в Unity нет, поэтому рядом с ключом хранится
    /// <c>GlobalObjectId</c> объекта, которому ключ выдан. У копии он свой, по расхождению
    /// копия и определяется. Иначе расставленная копированием россыпь ехала бы с одним
    /// ключом на всех.
    /// <para>
    /// Перенос объекта в другую сцену от копирования неотличим, поэтому ключ сменится
    /// и там, а сохранённое состояние потеряется.
    /// </para>
    /// </remarks>
    private void EnsureOwnId()
    {
        isIdCheckQueued = false;

        if (this == null || Application.isPlaying || idSource != SaveableIdSource.OwnId)
            return;

        string address = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(this).ToString();

        // У объекта, ещё не записанного в сцену, адреса нет — ждём сохранения.
        if (string.IsNullOrEmpty(address) || address.Contains("-0-0"))
            return;

        if (string.IsNullOrEmpty(ownId))
        {
            Assign(CreateId(), address);
            return;
        }

        // Ключ выдан до появления этой проверки: адрес просто запоминаем,
        // ключ не трогаем, иначе разом слетели бы все уже расставленные объекты.
        if (string.IsNullOrEmpty(idOwner))
        {
            Assign(ownId, address);
            return;
        }

        if (idOwner == address)
            return;

        Assign(CreateId(), address);
        PRLog.WriteDebug(this, $"Объект [{name}] скопирован: выдан новый ключ {ownId}.");
    }

    private void Assign(string id, string address)
    {
        ownId = id;
        idOwner = address;

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    /// <summary>
    /// Новый ключ.
    /// </summary>
    public static string CreateId()
    {
        return Guid.NewGuid().ToString("D");
    }

    #endregion

    #region Сохранение и загрузка

    /// <inheritdoc />
    /// <remarks>
    /// Запись появляется, только если состояние отличается от заданного в инспекторе.
    /// Объект, которого игрок не касался, не стоит в сохранении ничего — это важно там,
    /// где таких объектов сотни: россыпь подбираемых предметов начинает весить ровно
    /// столько, сколько игрок успел собрать. Совпавшая с умолчанием запись удаляется,
    /// иначе собранное и потом сброшенное состояние оставалось бы висеть.
    /// </remarks>
    public override bool TrySaveData()
    {
        if (string.IsNullOrEmpty(resolvedId))
            return false;

        if (!ObjectStateTracker.TryGetProjectData(out ProjectData projectData))
            return false;

        if (!projectData.SceneObjects.TryGetValue(resolvedId, out SceneObjectState state) || state == null)
            state = new SceneObjectState();

        CaptureState(state);

        if (IsDefaultState(state))
        {
            projectData.SceneObjects.Remove(resolvedId);
            return true;
        }

        projectData.SceneObjects[resolvedId] = state;
        return true;
    }

    /// <summary>
    /// Запись не отличается от того, что и так задано в инспекторе.
    /// </summary>
    /// <remarks>
    /// Наследник, положивший своё значение, делает запись нужной автоматически.
    /// Если у него есть собственные умолчания, которые тоже не стоит хранить,
    /// он переопределяет этот метод.
    /// </remarks>
    protected virtual bool IsDefaultState(SceneObjectState state)
    {
        if (state.HasValues)
            return false;

        return !saveActiveState || state.IsActive == defaultIsActive;
    }

    /// <summary>
    /// Применяет сохранённое состояние, если оно есть.
    /// </summary>
    /// <remarks>
    /// Записи может не быть — объект видят впервые. Это не ошибка: значения
    /// по умолчанию уже стоят с <c>Awake</c>, и трогать их не нужно.
    /// </remarks>
    private void ApplySavedState()
    {
        if (this == null || isStateApplied)
            return;

        isStateApplied = true;

        if (string.IsNullOrEmpty(resolvedId) || !ObjectStateTracker.TryGetProjectData(out ProjectData projectData))
            return;

        if (!projectData.SceneObjects.TryGetValue(resolvedId, out SceneObjectState state) || state == null)
            return;

        ApplyState(state);
    }

    /// <summary>
    /// Объект сейчас показан.
    /// </summary>
    public bool IsOpened => Target.activeSelf;

    /// <summary>
    /// Показывает объект и запоминает это.
    /// </summary>
    /// <remarks>
    /// Без параметров, поэтому вешается прямо на <c>UnityEvent</c> кнопки или триггера.
    /// </remarks>
    public void Open()
    {
        SetActiveState(true);
    }

    /// <summary>
    /// Прячет объект и запоминает это.
    /// </summary>
    public void Hide()
    {
        SetActiveState(false);
    }

    /// <summary>
    /// Меняет активность объекта и запоминает её.
    /// </summary>
    /// <param name="isActive">Каким объект должен стать.</param>
    /// <param name="save">Записать сохранение на диск сразу.</param>
    /// <remarks>
    /// Снимок можно было бы и не делать — активность всё равно попадёт в ближайший
    /// сбор сохранения. Но между сменой и сбором игра может закрыться, и тогда игрок
    /// увидит, что открытое им закрылось обратно. Поэтому пишем сразу.
    /// </remarks>
    public void SetActiveState(bool isActive, bool save = true)
    {
        Target.SetActive(isActive);

        if (!saveActiveState)
        {
            PRLog.WriteWarning(this,
                $"Активность объекта [{Target.name}] изменена, но не сохранится: хранение активности выключено.");

            return;
        }

        // а тот по кулдауну выходит, вообще не собирая состояние. Без своего вызова
        // изменение не попало бы даже в данные в памяти и потерялось бы при закрытии игры.
        // изменение не попало бы даже в данные в памяти и потерялось бы при закрытии игры.
        if (!TrySaveData() || !save)
            return;

        if (GameManager.HasInstance)
            GameManager.Instance.SaveProjectData();
    }

    /// <summary>
    /// Переносит состояние объекта в запись сохранения.
    /// </summary>
    protected virtual void CaptureState(SceneObjectState state)
    {
        if (saveActiveState)
            state.IsActive = Target.activeSelf;
    }

    /// <summary>
    /// Применяет к объекту сохранённое состояние.
    /// </summary>
    protected virtual void ApplyState(SceneObjectState state)
    {
        if (saveActiveState)
            Target.SetActive(state.IsActive);
    }

    /// <summary>
    /// Приводит объект к виду, заданному в инспекторе.
    /// </summary>
    protected virtual void ApplyDefaults()
    {
        if (saveActiveState)
            Target.SetActive(defaultIsActive);
    }

    #endregion

    #region Ключ

    /// <summary>
    /// Вычисляет ключ сохранения.
    /// </summary>
    private string ResolveId()
    {
        if (idSource == SaveableIdSource.OwnId)
        {
            if (string.IsNullOrEmpty(ownId))
                PRLog.WriteWarning(this, "Свой идентификатор не задан: состояние объекта сохраняться не будет.");

            return ownId;
        }

        IIdentifiable identifiable = Target.GetComponent<IIdentifiable>();

        if (identifiable == null)
        {
            PRLog.WriteWarning(this,
                $"На объекте [{Target.name}] нет компонента с идентификатором: состояние сохраняться не будет.");

            return string.Empty;
        }

        if (string.IsNullOrEmpty(identifiable.Id))
            PRLog.WriteWarning(this, $"У объекта [{Target.name}] пустой идентификатор: состояние сохраняться не будет.");

        return identifiable.Id;
    }

    /// <summary>
    /// Занимает ключ и предупреждает, если он уже за кем-то.
    /// </summary>
    /// <remarks>
    /// Два объекта с одним ключом пишут в одну запись и перетирают друг друга:
    /// побеждает сохранившийся последним, а игрок видит, что объект сам вернулся
    /// в прежнее состояние. Такое ловить тяжело, поэтому предупреждаем сразу
    /// и называем обоих.
    /// </remarks>
    private void RegisterId()
    {
        if (string.IsNullOrEmpty(resolvedId))
            return;

        if (UsedIds.TryGetValue(resolvedId, out SaveableObjectState owner) && owner != null && owner != this)
        {
            PRLog.WriteError(this,
                $"Ключ [{resolvedId}] уже занят объектом [{owner.name}]: состояния будут перетирать друг друга.");

            return;
        }

        UsedIds[resolvedId] = this;
    }

    #endregion
}
