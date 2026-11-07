using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Система пула объектов на сцене.
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    #region Вложенные типы

    /// <summary>
    /// Составной ключ пула: тип + категория. Используется вместо вложенных словарей,
    /// чтобы не дублировать поиск по двум уровням в каждом методе.
    /// </summary>
    private readonly struct PoolKey : IEquatable<PoolKey>
    {
        public readonly string Type;
        public readonly string Category;

        public PoolKey(string type, string category)
        {
            Type = type;
            Category = category;
        }

        public bool Equals(PoolKey other) =>
            string.Equals(Type, other.Type, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Category, other.Category, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => obj is PoolKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(
                Type?.ToUpperInvariant(),
                Category?.ToUpperInvariant());

        public override string ToString() => $"{Type}/{Category}";
    }

    /// <summary>
    /// Пул одного префаба: тип ключа, читаемая категория и идентификатор самого префаба.
    /// </summary>
    /// <remarks>
    /// Идентификатор — отдельным полем, а не частью строки: так найденный пул ищется без
    /// сборки строки на каждую выдачу. Строковая категория остаётся у <see cref="PoolKey"/>
    /// для отчёта пулов и <see cref="ClearPool(string, string)"/>.
    /// </remarks>
    private readonly struct PrefabPoolId : IEquatable<PrefabPoolId>
    {
        public readonly string Type;
        public readonly string ReadableCategory;
        public readonly int PrefabId;

        public PrefabPoolId(string type, string readableCategory, int prefabId)
        {
            Type = type;
            ReadableCategory = readableCategory;
            PrefabId = prefabId;
        }

        public bool Equals(PrefabPoolId other) =>
            PrefabId == other.PrefabId &&
            string.Equals(Type, other.Type, StringComparison.Ordinal) &&
            string.Equals(ReadableCategory, other.ReadableCategory, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is PrefabPoolId other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Type, ReadableCategory, PrefabId);
    }

    /// <summary>
    /// Данные одного зарегистрированного пула: очередь свободных объектов и исходные префабы.
    /// </summary>
    private class PoolEntry
    {
        public PoolKey Key;
        public readonly Queue<PoolObject> Queue = new();
        public List<GameObject> Prefabs;

        /// <summary>
        /// Сколько экземпляров ещё создать корутиной наполнения.
        /// </summary>
        public int Pending;

        /// <summary>
        /// Какой префаб из <see cref="Prefabs"/> создавать следующим: наполнение идёт по кругу.
        /// </summary>
        public int NextPrefab;
    }

    #endregion

    #region Поля и свойства

    /// <summary>
    /// Группа частиц.
    /// </summary>
    [SerializeField] private Transform particles;

    /// <summary>
    /// Группа игровых объектов.
    /// </summary>
    [SerializeField] private Transform gameObjects;

    /// <summary>
    /// Все зарегистрированные пулы, ключ — тип + категория.
    /// </summary>
    private readonly Dictionary<PoolKey, PoolEntry> pools = new();

    /// <summary>
    /// Пулы, заведённые по префабу, — быстрый путь выдачи. Источник истины — <see cref="pools"/>:
    /// запись здесь действительна, только пока тот же пул лежит и там.
    /// </summary>
    private readonly Dictionary<PrefabPoolId, PoolEntry> prefabPools = new();

    /// <summary>
    /// Объекты, вытянутые из очереди и находящиеся на сцене.
    /// </summary>
    private readonly List<PoolObject> objectOnScene = new();

    /// <summary>
    /// Запущенные корутины возврата объектов по истечении времени жизни.
    /// </summary>
    private readonly Dictionary<Guid, Coroutine> runningCoroutines = new();

    /// <summary>
    /// Корутины наполнения зарегистрированных пулов.
    /// </summary>
    private readonly Dictionary<PoolKey, Coroutine> poolCreationCoroutines = new();

    /// <summary>
    /// Формирует отчёт по всем пулам: сколько объектов создано, сколько активно, сколько в резерве.
    /// </summary>
    public List<PoolSystemTableData> GenerateReport()
    {
        // Считаем активные объекты один раз, а не заново для каждого пула,
        // как это было раньше (Count() внутри двойного foreach).
        var activeCounts = new Dictionary<PoolKey, int>();
        foreach (var obj in objectOnScene)
        {
            var key = new PoolKey(obj.Type, obj.Category);
            activeCounts.TryGetValue(key, out var current);
            activeCounts[key] = current + 1;
        }

        var result = new List<PoolSystemTableData>(pools.Count);
        foreach (var (key, entry) in pools)
        {
            activeCounts.TryGetValue(key, out var showCount);
            var hideCount = entry.Queue.Count;

            result.Add(new PoolSystemTableData
            {
                Type = key.Type,
                Category = key.Category,
                TotalCount = showCount + hideCount,
                ShowCount = showCount,
                HideCount = hideCount
            });
        }

        return result;
    }

    #endregion

    #region Регистрация пулов

    /// <summary>
    /// Регистрирует новый пул объектов.
    /// </summary>
    /// <param name="type">Компонент, для которого регистрируется пул объектов.</param>
    /// <param name="category">Категория пула.</param>
    /// <param name="obj">Префаб объекта.</param>
    /// <param name="count">Количество объектов для создания (по умолчанию 1).</param>
    public void RegisterPoolObject(string type, string category, GameObject obj, int count = 1)
        => RegisterPoolObject(type, category, new List<GameObject> { obj }, count);

    /// <summary>
    /// Регистрирует новый пул объектов под типом MonoBehaviour.
    /// </summary>
    /// <param name="category">Категория пула.</param>
    /// <param name="obj">Префаб объекта.</param>
    /// <param name="count">Количество объектов для создания (по умолчанию 1).</param>
    public void RegisterPoolObject(string category, GameObject obj, int count = 1)
        => RegisterPoolObject(DefaultTypeKey, category, new List<GameObject> { obj }, count);

    /// <summary>
    /// Регистрирует новый пул объектов под типом MonoBehaviour.
    /// </summary>
    /// <param name="category">Категория пула.</param>
    /// <param name="objs">Префабы объектов.</param>
    /// <param name="count">Количество объектов для создания (по умолчанию 1).</param>
    public void RegisterPoolObject(string category, List<GameObject> objs, int count = 1)
        => RegisterPoolObject(DefaultTypeKey, category, objs, count);

    /// <summary>
    /// Регистрирует новый пул объектов.
    /// </summary>
    /// <param name="component">Компонент, чей тип используется как ключ пула.</param>
    /// <param name="category">Категория пула.</param>
    /// <param name="obj">Префаб объекта.</param>
    /// <param name="count">Количество объектов для создания (по умолчанию 1).</param>
    public void RegisterPoolObject(Component component, string category, GameObject obj, int count = 1)
        => RegisterPoolObject(TypeKeyOf(component), category, new List<GameObject> { obj }, count);

    /// <summary>
    /// Регистрирует новый пул объектов.
    /// </summary>
    /// <param name="component">Компонент, чей тип используется как ключ пула.</param>
    /// <param name="category">Категория пула.</param>
    /// <param name="objects">Префабы объектов.</param>
    /// <param name="count">Количество объектов для создания (по умолчанию 1).</param>
    public void RegisterPoolObject(Component component, string category, List<GameObject> objects, int count = 1)
        => RegisterPoolObject(TypeKeyOf(component), category, objects, count);

    /// <summary>
    /// Регистрирует новый пул объектов.
    /// </summary>
    /// <param name="type">Тип, используемый как ключ пула.</param>
    /// <param name="category">Категория пула.</param>
    /// <param name="obj">Префаб объекта.</param>
    /// <param name="count">Количество объектов для создания (по умолчанию 1).</param>
    public void RegisterPoolObject(Type type, string category, GameObject obj, int count = 1)
        => RegisterPoolObject(type.ToString(), category, new List<GameObject> { obj }, count);

    /// <summary>
    /// Регистрирует новый пул объектов. Основная реализация — все остальные перегрузки сводятся к ней.
    /// </summary>
    /// <param name="type">Тип, используемый как ключ пула.</param>
    /// <param name="category">Категория пула.</param>
    /// <param name="objects">Префабы объектов (случайно выбираются при создании экземпляра).</param>
    /// <param name="count">Желаемое количество объектов в пуле (по умолчанию 1).</param>
    public void RegisterPoolObject(string type, string category, List<GameObject> objects, int count = 1)
    {
        if (objects == null || objects.Count == 0)
        {
            PRLog.WriteError(this, $"[ObjectPoolManager] Не удалось зарегистрировать пул '{type}/{category}': список префабов пуст.");
            return;
        }

        if (count < 1)
            count = 1;

        var key = new PoolKey(type, category);

        if (pools.TryGetValue(key, out var entry))
        {
            // Пул уже существует — досоздаём только недостающее. Считаются все живые экземпляры
            // (свободные и выданные) и ещё не созданные: по одной свободной очереди повторный
            // прогрев плодил лишние объекты, пока часть была выдана.
            var missing = count * entry.Prefabs.Count - CountAlive(entry) - entry.Pending;
            if (missing > 0)
                RequestInstances(entry, missing);
            return;
        }

        entry = new PoolEntry { Key = key, Prefabs = objects };
        pools[key] = entry;
        RequestInstances(entry, count * objects.Count);
    }

    #endregion

    #region Отображение объектов

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    public PoolObject ShowObject(string category, Transform transform, Vector3 scaler)
        => ShowObject(DefaultTypeKey, category, Vector3.zero, Quaternion.identity, scaler, transform);

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    public PoolObject ShowObject(Type type, string category, Transform transform, Vector3 scaler)
        => ShowObject(type.ToString(), category, Vector3.zero, Quaternion.identity, scaler, transform);

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    public PoolObject ShowObject(Type type, string category, Transform transform)
        => ShowObject(type.ToString(), category, Vector3.zero, Quaternion.identity, Vector3.one, transform);

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    public PoolObject ShowObject(Type type, string category, Vector3 position)
        => ShowObject(type.ToString(), category, position, Quaternion.identity, Vector3.one, null);

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    public PoolObject ShowObject(Type type, string category, Vector3 position, Vector3 scaler)
        => ShowObject(type.ToString(), category, position, Quaternion.identity, scaler, null);

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    public PoolObject ShowObject(Component component, string category, Transform transform, Vector3 scaler)
        => ShowObject(TypeKeyOf(component), category, Vector3.zero, Quaternion.identity, scaler, transform);

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    /// <remarks>
    /// Как и остальные перегрузки с <see cref="Transform"/>: объект становится его дочерним в его точке.
    /// Раньше эта одна ставила объект в мировые координаты без родителя — поведение зависело от того,
    /// передан тип или компонент. Нужна точка без родителя — перегрузка с позицией и поворотом.
    /// </remarks>
    public PoolObject ShowObject(Component component, string category, Transform transform)
        => ShowObject(TypeKeyOf(component), category, Vector3.zero, Quaternion.identity, Vector3.one, transform);

    /// <summary>
    /// Отображает объект в мировой точке без родителя.
    /// </summary>
    public PoolObject ShowObject(Component component, string category, Vector3 position, Quaternion rotation)
        => ShowObject(TypeKeyOf(component), category, position, rotation, Vector3.one, null);

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    public PoolObject ShowObject(Component component, string category, Vector3 position)
        => ShowObject(TypeKeyOf(component), category, position, Quaternion.identity, Vector3.one, null);

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    public PoolObject ShowObject(Component component, string category, Vector3 position, Vector3 scaler)
        => ShowObject(TypeKeyOf(component), category, position, Quaternion.identity, scaler, null);

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    public PoolObject ShowObject(string category, Transform transform)
        => ShowObject(DefaultTypeKey, category, Vector3.zero, Quaternion.identity, Vector3.one, transform);

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    public PoolObject ShowObject(string category, Vector3 position)
        => ShowObject(DefaultTypeKey, category, position, Quaternion.identity, Vector3.one, null);

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    public PoolObject ShowObject(string category, Vector3 position, Vector3 scaler)
        => ShowObject(DefaultTypeKey, category, position, Quaternion.identity, scaler, null);

    /// <summary>
    /// Отображает объект на сцене в позиции (0,0,0) без родителя.
    /// </summary>
    public PoolObject ShowObject(string category)
        => ShowObject(DefaultTypeKey, category, Vector3.zero, Quaternion.identity, Vector3.one, null);

    /// <summary>
    /// Отображает объект на сцене. Основная реализация — все остальные перегрузки сводятся к ней.
    /// </summary>
    /// <param name="type">Тип, используемый как ключ пула.</param>
    /// <param name="category">Категория объекта.</param>
    /// <param name="position">Позиция: мировая без родителя, локальная — с родителем.</param>
    /// <param name="rotation">Поворот: мировой без родителя, локальный — с родителем.</param>
    /// <param name="scaler">Локальный размер.</param>
    /// <param name="parent">Родительский transform (может быть null).</param>
    /// <returns>Объект пула, либо null, если пул с таким ключом не зарегистрирован.</returns>
    public PoolObject ShowObject(string type, string category, Vector3 position, Quaternion rotation, Vector3 scaler, Transform parent)
    {
        var key = new PoolKey(type, category);
        if (!pools.TryGetValue(key, out var entry))
        {
            PRLog.WriteError(this, $"[ObjectPoolManager] Пул '{key}' не зарегистрирован. Сначала вызовите RegisterPoolObject.");
            return null;
        }

        return ShowFromEntry(entry, position, rotation, scaler, parent);
    }

    /// <summary>
    /// Выдаёт объект из уже найденного пула.
    /// </summary>
    /// <remarks>
    /// Родитель ставится при каждой выдаче, в том числе пустой: объект из пула помнит
    /// родителя прошлой выдачи, и без сброса выданный «без родителя» остался бы дочерним
    /// у прежнего — с его трансформацией и уничтожением вместе с ним.
    /// </remarks>
    private PoolObject ShowFromEntry(PoolEntry entry, Vector3 position, Quaternion rotation, Vector3 scaler, Transform parent)
    {
        // Если очередь пуста или объект был уничтожен извне (например, при смене сцены) — досоздаём один экземпляр.
        while (entry.Queue.Count > 0 && entry.Queue.Peek().InstanceGameObject == null)
            entry.Queue.Dequeue();

        if (entry.Queue.Count == 0)
        {
            // Пока идёт наполнение, созданный по требованию — его очередной шаг: в счёт заказа и тем же
            // префабом по кругу, иначе прогрев «по N каждого» перекосился бы в пользу случайного.
            GameObject prefab;
            if (entry.Pending > 0)
            {
                entry.Pending--;
                prefab = NextPrefabInTurn(entry);
            }
            else
            {
                prefab = RandomPrefab(entry);
            }

            CreateInstance(entry, prefab);
        }

        var poolObject = entry.Queue.Dequeue();

        poolObject.InstanceGameObject.transform.SetParent(parent != null ? parent : ContainerOf(poolObject));

        // Без родителя позиция мировая: контейнер пула не обязан стоять в начале координат (менеджер
        // создаётся из префаба со сдвинутым корнем), и локальная позиция в нём увела бы объект в сторону.
        // С родителем — локальная относительно него.
        var t = poolObject.InstanceGameObject.transform;
        if (parent != null)
        {
            t.localPosition = position;
            t.localRotation = rotation;
        }
        else
        {
            t.SetPositionAndRotation(position, rotation);
        }

        t.localScale = scaler;

        objectOnScene.Add(poolObject);
        poolObject.InstanceGameObject.SetActive(true);
        poolObject.InstanceGameObject.GetComponent<IPoolable>()?.InitializationPoolObject();

        if (objectOnScene.Contains(poolObject) && poolObject.Lifetime > TimeSpan.Zero)
            StartCoroutineTracking(poolObject.Guid, BackToQueue(poolObject));

        return poolObject;
    }

    public T ShowEntity<T>(T prefab, Vector3 position)
        where T : EntityBase
        => ShowEntity(prefab, position, Quaternion.identity, null);

    /// <summary>
    /// Выдаёт сущность из пула в заданное место.
    /// </summary>
    /// <remarks>
    /// Пул ставит в точку выданный объект, а сущностью может быть вложенный в него объект
    /// со своим смещением. Поэтому после выдачи сущность досылается на место сама: просили
    /// поставить в точку её, а не корень, в котором она лежит.
    /// </remarks>
    /// <param name="prefab">Префаб сущности.</param>
    /// <param name="position">Куда поставить сущность.</param>
    /// <param name="rotation">Каким поворотом поставить сущность.</param>
    /// <param name="transform">Родитель либо <c>null</c>.</param>
    /// <returns>Сущность из пула.</returns>
    public T ShowEntity<T>(T prefab, Vector3 position, Quaternion rotation, Transform transform)
        where T : EntityBase
    {
        var poolObject = ShowPrefab(EntityTypeKey, prefab.GetPoolKey(), prefab.gameObject, position, rotation, prefab.transform.localScale, transform);
        var entity = poolObject.InstanceGameObject.GetComponent<T>();
        if (entity == null)
            return null;

        var placed = poolObject.InstanceGameObject.transform;
        entity.SetPositionAndRotation(placed.position, placed.rotation);

        return entity;
    }

    public T ShowEntity<T>(T prefab, Transform transform)
        where T : EntityBase
        => ShowEntity(prefab, Vector3.zero, Quaternion.identity, transform);

    public T ShowObject<T>(T prefab, Vector3 position, Quaternion rotation, Transform transform, out PoolObject poolObject)
        where T : Component
    {
        poolObject = ShowPrefab(PrefabTypeKey, prefab.name, prefab.gameObject, position, rotation, prefab.transform.localScale, transform);
        return poolObject.InstanceGameObject.GetComponent<T>();
    }

    public T ShowObject<T>(T prefab, Vector3 position, Transform transform, out PoolObject poolObject)
        where T : Component
    {
        poolObject = ShowPrefab(PrefabTypeKey, prefab.name, prefab.gameObject, position, Quaternion.identity, prefab.transform.localScale, transform);
        return poolObject.InstanceGameObject.GetComponent<T>();
    }

    public T ShowObject<T>(T prefab, Transform transform, out PoolObject poolObject)
        where T : Component
    {
        poolObject = ShowPrefab(PrefabTypeKey, prefab.name, prefab.gameObject, Vector3.zero, Quaternion.identity, prefab.transform.localScale, transform);
        return poolObject.InstanceGameObject.GetComponent<T>();
    }

    public T ShowObject<T>(T prefab, Vector3 position, out PoolObject poolObject)
        where T : Component
    {
        poolObject = ShowPrefab(PrefabTypeKey, prefab.name, prefab.gameObject, position, Quaternion.identity, Vector3.one, null);
        return poolObject.InstanceGameObject.GetComponent<T>();
    }

    public GameObject ShowObject(GameObject prefab, Vector3 position, out PoolObject poolObject)
    {
        poolObject = ShowPrefab(PrefabTypeKey, prefab.name, prefab, position, Quaternion.identity, Vector3.one, null);
        return poolObject.InstanceGameObject;
    }

    public GameObject ShowObject(GameObject prefab, Vector3 position, Vector3 scale, out PoolObject poolObject)
    {
        poolObject = ShowPrefab(PrefabTypeKey, prefab.name, prefab, position, Quaternion.identity, scale, null);
        return poolObject.InstanceGameObject;
    }

    public GameObject ShowObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform transform, out PoolObject poolObject)
    {
        poolObject = ShowPrefab(PrefabTypeKey, prefab.name, prefab, position, rotation, prefab.transform.localScale, transform);
        return poolObject.InstanceGameObject;
    }

    /// <summary>
    /// Выдаёт экземпляр именно этого префаба, заводя ему пул при первом обращении.
    /// </summary>
    /// <remarks>
    /// Категория пула привязана к самому префабу (<see cref="PrefabCategory"/>), а не только к его
    /// имени, типу компонента или <see cref="EntityBase.GetPoolKey"/>: всё это у разных префабов
    /// совпадает. У префаба сущности нет метаданных — они ставятся в <c>Awake</c>, а ассет не
    /// просыпается, — и ключ у всех <c>Unknown</c>. С общим пулом второй префаб молча получал
    /// экземпляры первого: под одним ключом регистрируется только первый набор префабов.
    /// </remarks>
    /// <param name="type">Тип ключа пула.</param>
    /// <param name="readableCategory">Читаемая часть категории: её видно в отладочном отчёте пулов.</param>
    /// <param name="prefab">Префаб, экземпляр которого нужен.</param>
    /// <param name="position">Позиция: мировая без родителя, локальная — с родителем.</param>
    /// <param name="rotation">Поворот: мировой без родителя, локальный — с родителем.</param>
    /// <param name="scale">Локальный размер.</param>
    /// <param name="parent">Родитель либо <c>null</c>.</param>
    private PoolObject ShowPrefab(string type, string readableCategory, GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale, Transform parent)
    {
        PoolEntry entry = GetOrRegisterPrefabPool(type, readableCategory, prefab, 1);
        return ShowFromEntry(entry, position, rotation, scale, parent);
    }

    /// <summary>
    /// Заранее создаёт экземпляры сущности для <see cref="ShowEntity{T}(T, Vector3, Quaternion, Transform)"/>.
    /// </summary>
    /// <remarks>
    /// Регистрировать пул сущности через <see cref="RegisterPoolObject(string, string, GameObject, int)"/>
    /// бесполезно: <c>ShowEntity</c> ищет пул по ключу самого префаба и такой пул не увидит.
    /// Экземпляры создаются по одному за кадр.
    /// </remarks>
    /// <param name="prefab">Префаб сущности.</param>
    /// <param name="count">Сколько экземпляров держать в пуле.</param>
    public void RegisterEntityPool<T>(T prefab, int count)
        where T : EntityBase
        => GetOrRegisterPrefabPool(EntityTypeKey, prefab.GetPoolKey(), prefab.gameObject, count);

    /// <summary>
    /// Заранее создаёт экземпляры объекта для перегрузок <c>ShowObject</c>, принимающих префаб.
    /// </summary>
    /// <remarks>
    /// Один пул на префаб, как бы его ни выдавали: самим <see cref="GameObject"/> или его компонентом.
    /// Экземпляры создаются по одному за кадр.
    /// </remarks>
    /// <param name="prefab">Префаб объекта.</param>
    /// <param name="count">Сколько экземпляров держать в пуле.</param>
    public void RegisterPrefabPool(GameObject prefab, int count)
        => GetOrRegisterPrefabPool(PrefabTypeKey, prefab.name, prefab, count);

    /// <summary>
    /// Находит пул префаба или заводит его; при заказе больше одного экземпляра — дополняет.
    /// </summary>
    /// <remarks>
    /// Выдача идёт сюда на каждое появление объекта, поэтому найденный пул берётся из
    /// <see cref="prefabPools"/> без новых строк и списков. Повторная регистрация на каждой
    /// выдаче к тому же сбивала прогрев: при пустой очереди она останавливала корутину
    /// наполнения и заводила новую на один экземпляр.
    /// </remarks>
    private PoolEntry GetOrRegisterPrefabPool(string type, string readableCategory, GameObject prefab, int count)
    {
        var id = new PrefabPoolId(type, readableCategory, prefab.GetInstanceID());

        // Пул могли снять через ClearPool: тогда ссылка в кэше устарела и пул заводится заново.
        if (count <= 1 &&
            prefabPools.TryGetValue(id, out PoolEntry cached) &&
            pools.TryGetValue(cached.Key, out PoolEntry live) &&
            ReferenceEquals(cached, live))
            return cached;

        string category = PrefabCategory(readableCategory, prefab);
        RegisterPoolObject(type, category, prefab, count);

        PoolEntry entry = pools[new PoolKey(type, category)];
        prefabPools[id] = entry;
        return entry;
    }

    #endregion

    #region Внутренняя логика

    /// <summary>
    /// Строковый ключ типа по умолчанию — используется, когда вызывающий код не привязан к конкретному компоненту.
    /// </summary>
    private static string DefaultTypeKey { get; } = typeof(MonoBehaviour).ToString();

    /// <summary>
    /// Тип ключа для пулов сущностей, заведённых по префабу.
    /// </summary>
    private const string EntityTypeKey = "Entity";

    /// <summary>
    /// Тип ключа для пулов обычных объектов, заведённых по префабу.
    /// </summary>
    private const string PrefabTypeKey = nameof(GameObject);

    /// <summary>
    /// Категория пула одного префаба: читаемое имя плюс идентификатор самого префаба.
    /// </summary>
    /// <remarks>
    /// Идентификатор объекта не меняется и за сеанс редактора или игры не выдаётся повторно,
    /// поэтому ключ не укажет на другой префаб даже после уничтожения прежнего.
    /// Читаемую часть оставляем: переопределения <see cref="EntityBase.GetPoolKey"/> разводят
    /// по пулам экземпляры одного префаба с разным содержимым (контейнеры с разными предметами).
    /// </remarks>
    private static string PrefabCategory(string readableCategory, GameObject prefab) =>
        $"{readableCategory}#{prefab.GetInstanceID()}";

    /// <summary>
    /// Контейнер, в котором объект пула лежит без своего родителя.
    /// </summary>
    private Transform ContainerOf(PoolObject poolObject) =>
        poolObject.PoolObjectType == PoolObjectType.Particles ? particles : gameObjects;

    private static string TypeKeyOf(Component component) => component.GetType().ToString();

    private static GameObject RandomPrefab(PoolEntry entry) =>
        entry.Prefabs[UnityEngine.Random.Range(0, entry.Prefabs.Count)];

    /// <summary>
    /// Следующий префаб заказанного наполнения: по кругу, чтобы каждого вышло поровну.
    /// </summary>
    private static GameObject NextPrefabInTurn(PoolEntry entry) =>
        entry.Prefabs[entry.NextPrefab++ % entry.Prefabs.Count];

    /// <summary>
    /// Корутина создания объектов в пуле.
    /// </summary>
    /// <param name="entry">Пул, в который добавляются объекты.</param>
    /// <param name="count">Количество объектов для создания.</param>
    private IEnumerator FillPool(PoolEntry entry)
    {
        while (entry.Pending > 0)
        {
            entry.Pending--;
            CreateInstance(entry, NextPrefabInTurn(entry));
            yield return null;
        }

        poolCreationCoroutines.Remove(entry.Key);
    }

    /// <summary>
    /// Заказывает ещё экземпляры пула; создаются по одному за кадр.
    /// </summary>
    /// <remarks>
    /// Идущее наполнение не перезапускается, а получает добавку: раньше новый заказ
    /// останавливал корутину, и недоделанная часть прежнего пропадала.
    /// </remarks>
    private void RequestInstances(PoolEntry entry, int count)
    {
        entry.Pending += count;

        if (!poolCreationCoroutines.ContainsKey(entry.Key))
            poolCreationCoroutines[entry.Key] = StartCoroutine(FillPool(entry));
    }

    /// <summary>
    /// Живые экземпляры пула: свободные и выданные.
    /// </summary>
    private int CountAlive(PoolEntry entry)
    {
        int alive = 0;

        foreach (var poolObject in entry.Queue)
        {
            if (poolObject.InstanceGameObject != null)
                alive++;
        }

        foreach (var poolObject in objectOnScene)
        {
            if (poolObject.InstanceGameObject != null && new PoolKey(poolObject.Type, poolObject.Category).Equals(entry.Key))
                alive++;
        }

        return alive;
    }

    private void StopPoolInstantiation(PoolKey key)
    {
        if (!poolCreationCoroutines.TryGetValue(key, out var coroutine))
            return;

        StopCoroutine(coroutine);
        poolCreationCoroutines.Remove(key);
    }

    /// <summary>
    /// Создаёт экземпляр объекта и кладёт его в очередь пула.
    /// </summary>
    /// <param name="entry">Пул, в который добавляется созданный объект (хранит ключ типа/категории).</param>
    /// <param name="prefab">Префаб, из которого создаётся экземпляр.</param>
    private void CreateInstance(PoolEntry entry, GameObject prefab)
    {
        var instance = PRUnitySDK.Instantiate(prefab);
        var poolObject = new PoolObject(entry.Key.Type, entry.Key.Category, instance);

        if (instance.TryGetComponent<IPoolable>(out var poolable))
            poolable.RegisterPoolObject(poolObject);

        poolObject.InstanceGameObject.transform.SetParent(ContainerOf(poolObject));

        instance.SetActive(false);
        entry.Queue.Enqueue(poolObject);
    }

    /// <summary>
    /// Очистка данных при смене сцены.
    /// </summary>
    private void OnSceneEnd(string currentScene, string nextScene) => ClearData();

    /// <summary>
    /// Полностью очищает все пулы, останавливает корутины и уничтожает объекты.
    /// </summary>
    public void ClearData()
    {
        StopAllRunningCoroutines();

        foreach (var key in pools.Keys.ToList())
            ClearPool(key);

        foreach (var poolObject in objectOnScene.ToList())
            poolObject.Dispose();

        poolCreationCoroutines.Clear();
        pools.Clear();
        prefabPools.Clear();
        objectOnScene.Clear();
    }

    /// <summary>
    /// Удаляет регистрацию одного пула и уничтожает все его активные и свободные объекты.
    /// </summary>
    /// <param name="type">Тип ключа пула.</param>
    /// <param name="category">Категория ключа пула.</param>
    /// <returns>True, если пул существовал и был удалён.</returns>
    public bool ClearPool(string type, string category)
    {
        return ClearPool(new PoolKey(type, category));
    }

    private bool ClearPool(PoolKey key)
    {
        if (!pools.TryGetValue(key, out var entry))
            return false;

        StopPoolInstantiation(key);

        foreach (var poolObject in entry.Queue)
            poolObject.Dispose();

        foreach (var poolObject in objectOnScene
                     .Where(item => new PoolKey(item.Type, item.Category).Equals(key))
                     .ToList())
        {
            StopRunningCoroutine(poolObject.Guid);
            objectOnScene.Remove(poolObject);
            poolObject.Dispose();
        }

        entry.Queue.Clear();
        entry.Pending = 0;
        pools.Remove(key);

        foreach (var prefabPool in prefabPools.Where(pair => ReferenceEquals(pair.Value, entry)).ToList())
            prefabPools.Remove(prefabPool.Key);

        return true;
    }

    private void StopAllRunningCoroutines()
    {
        foreach (var coroutine in runningCoroutines.Values)
            StopCoroutine(coroutine);

        runningCoroutines.Clear();
    }

    private void StopRunningCoroutine(Guid guid)
    {
        if (!runningCoroutines.TryGetValue(guid, out var coroutine))
            return;

        StopCoroutine(coroutine);
        runningCoroutines.Remove(guid);
    }

    private Coroutine StartCoroutineTracking(Guid guid, IEnumerator routine)
    {
        var coroutine = StartCoroutine(routine);
        runningCoroutines[guid] = coroutine;
        return coroutine;
    }

    /// <summary>
    /// Корутина возврата объекта в пул по истечении времени жизни.
    /// </summary>
    private IEnumerator BackToQueue(PoolObject poolObject)
    {
        yield return new WaitForSeconds((float)poolObject.Lifetime.TotalSeconds);
        runningCoroutines.Remove(poolObject.Guid);
        OnObjectDestroy(poolObject);
    }

    /// <summary>
    /// Уничтожение объекта пула — возвращает его обратно в очередь (либо уничтожает полностью).
    /// </summary>
    /// <param name="poolObject">Объект пула для возврата.</param>
    /// <param name="fullDestroy">Если true — объект не возвращается в очередь (используется при полном удалении).</param>
    public void OnObjectDestroy(PoolObject poolObject, bool fullDestroy = false)
    {
        if (poolObject == null)
            return;

        StopRunningCoroutine(poolObject.Guid);

        bool wasOnScene = objectOnScene.Remove(poolObject);
        var key = new PoolKey(poolObject.Type, poolObject.Category);
        pools.TryGetValue(key, out var entry);

        if (fullDestroy)
        {
            if (entry != null)
            {
                int count = entry.Queue.Count;
                for (int i = 0; i < count; i++)
                {
                    var queued = entry.Queue.Dequeue();
                    if (!ReferenceEquals(queued, poolObject))
                        entry.Queue.Enqueue(queued);
                }
            }

            // Пул объект больше не учитывает — уничтожает его он же. Иначе выданный корень
            // оставался бы висеть: сущность, вложенная в него, уничтожает только себя.
            poolObject.Dispose();
            return;
        }

        if (entry == null)
        {
            PRLog.WriteError(this, $"[ObjectPoolManager] Не найден пул с ключом '{key}'.");
            return;
        }

        if (!wasOnScene)
            return;

        // Свободный объект лежит в контейнере пула, а не у родителя выдачи: иначе он двигался бы
        // вместе с чужим объектом и погиб бы при его уничтожении, пока ждёт следующей выдачи.
        // Возврат зовут явно (DestroyEntity, таймер жизни), не из OnDestroy/OnDisable, где
        // менять иерархию Unity не даёт.
        if (poolObject.InstanceGameObject != null)
        {
            poolObject.InstanceGameObject.SetActive(false);
            poolObject.InstanceGameObject.transform.SetParent(ContainerOf(poolObject), false);
        }

        entry.Queue.Enqueue(poolObject);
    }

    /// <summary>
    /// Уничтожение объекта по ссылке на GameObject.
    /// </summary>
    public void OnObjectDestroy(GameObject obj)
    {
        var poolObject = objectOnScene.FirstOrDefault(x => x.InstanceGameObject == obj);
        if (poolObject != null)
            OnObjectDestroy(poolObject);
    }

    #endregion

}

public class ObjectPoolManagerFactory : SingletonMonoBehaviourFactoryBase<ObjectPoolManager>
{
    public override string ResourcePath => $"{PRUnitySDK.ResourcePaths.PrefabsPath}/ObjectPoolManager";
}

public class PoolSystemTableData
{
    public string Type;
    public string Category;
    public long TotalCount;
    public long ShowCount;
    public long HideCount;
}
