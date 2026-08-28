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
    /// Данные одного зарегистрированного пула: очередь свободных объектов и исходные префабы.
    /// </summary>
    private class PoolEntry
    {
        public PoolKey Key;
        public readonly Queue<PoolObject> Queue = new();
        public List<GameObject> Prefabs;
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
            Debug.LogError($"[ObjectPoolManager] Не удалось зарегистрировать пул '{type}/{category}': список префабов пуст.");
            return;
        }

        if (count < 1)
            count = 1;

        var key = new PoolKey(type, category);

        if (pools.TryGetValue(key, out var entry))
        {
            // Пул уже существует — при необходимости досоздаём недостающие объекты.
            // Раньше эта проверка была недостижима из-за раннего return в начале метода.
            var missing = count - entry.Queue.Count;
            if (missing > 0)
                StartPoolInstantiation(entry, missing);
            return;
        }

        entry = new PoolEntry { Key = key, Prefabs = objects };
        pools[key] = entry;
        StartPoolInstantiation(entry, count);
    }

    #endregion

    #region Отображение объектов

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    public PoolObject ShowObject(string category, Transform transform, Vector3 scaler)
        => ShowObject(DefaultTypeKey, category, transform.position, transform.rotation, scaler, transform);

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    public PoolObject ShowObject(Type type, string category, Transform transform, Vector3 scaler)
        => ShowObject(type.ToString(), category, transform.position, transform.rotation, scaler, transform);

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    public PoolObject ShowObject(Type type, string category, Transform transform)
        => ShowObject(type.ToString(), category, transform.position, transform.rotation, Vector3.one, transform);

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
        => ShowObject(TypeKeyOf(component), category, transform.position, transform.rotation, scaler, transform);

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    public PoolObject ShowObject(Component component, string category, Transform transform)
        => ShowObject(TypeKeyOf(component), category, transform.position, transform.rotation, Vector3.one, null);

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
        => ShowObject(DefaultTypeKey, category, transform.position, transform.rotation, Vector3.one, transform);

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
    /// <param name="position">Локальная позиция.</param>
    /// <param name="rotation">Локальное вращение.</param>
    /// <param name="scaler">Локальный размер.</param>
    /// <param name="parent">Родительский transform (может быть null).</param>
    /// <returns>Объект пула, либо null, если пул с таким ключом не зарегистрирован.</returns>
    public PoolObject ShowObject(string type, string category, Vector3 position, Quaternion rotation, Vector3 scaler, Transform parent)
    {
        var key = new PoolKey(type, category);
        if (!pools.TryGetValue(key, out var entry))
        {
            Debug.LogError($"[ObjectPoolManager] Пул '{key}' не зарегистрирован. Сначала вызовите RegisterPoolObject.");
            return null;
        }

        // Если очередь пуста или объект был уничтожен извне (например, при смене сцены) — досоздаём один экземпляр.
        while (entry.Queue.Count > 0 && entry.Queue.Peek().InstanceGameObject == null)
            entry.Queue.Dequeue();

        if (entry.Queue.Count == 0)
            CreateInstance(entry, RandomPrefab(entry));

        var poolObject = entry.Queue.Dequeue();

        if (parent != null)
            poolObject.InstanceGameObject.transform.SetParent(parent);

        var t = poolObject.InstanceGameObject.transform;
        t.localPosition = position;
        t.localRotation = rotation;
        t.localScale = scaler;

        poolObject.InstanceGameObject.SetActive(true);
        poolObject.InstanceGameObject.GetComponent<IPoolable>()?.InitializationPoolObject();

        if (poolObject.Lifetime > TimeSpan.Zero)
            StartCoroutineTracking(poolObject.Guid, BackToQueue(poolObject));

        objectOnScene.Add(poolObject);
        return poolObject;
    }

    public T ShowEntity<T>(T prefab, Vector3 position)
        where T : EntityBase
        => ShowEntity(prefab, position, Quaternion.identity, null);

    public T ShowEntity<T>(T prefab, Vector3 position, Quaternion rotation, Transform transform)
        where T : EntityBase
    {
        RegisterPoolObject("Entity", prefab.GetPoolKey(), prefab.gameObject);
        var poolObject = ShowObject("Entity", prefab.GetPoolKey(), position, rotation, prefab.transform.localScale, transform);
        return poolObject.InstanceGameObject.GetComponent<T>();
    }

    public T ShowEntity<T>(T prefab, Transform transform)
        where T : EntityBase
        => ShowEntity(prefab, Vector3.zero, Quaternion.identity, transform);

    public T ShowObject<T>(T prefab, Vector3 position, Quaternion rotation, Transform transform, out PoolObject poolObject)
        where T : Component
    {
        RegisterPoolObject(nameof(GameObject), prefab.GetType().ToString(), prefab.gameObject);
        poolObject = ShowObject(nameof(GameObject), prefab.GetType().ToString(), position, rotation, prefab.transform.localScale, transform);
        return poolObject.InstanceGameObject.GetComponent<T>();
    }

    public T ShowObject<T>(T prefab, Vector3 position, Transform transform, out PoolObject poolObject)
        where T : Component
    {
        RegisterPoolObject(nameof(GameObject), prefab.GetType().ToString(), prefab.gameObject);
        poolObject = ShowObject(nameof(GameObject), prefab.GetType().ToString(), position, Quaternion.identity, prefab.transform.localScale, transform);
        return poolObject.InstanceGameObject.GetComponent<T>();
    }

    public T ShowObject<T>(T prefab, Transform transform, out PoolObject poolObject)
        where T : Component
    {
        RegisterPoolObject(nameof(GameObject), prefab.GetType().ToString(), prefab.gameObject);
        poolObject = ShowObject(nameof(GameObject), prefab.GetType().ToString(), Vector3.zero, Quaternion.identity, prefab.transform.localScale, transform);
        return poolObject.InstanceGameObject.GetComponent<T>();
    }

    public T ShowObject<T>(T prefab, Vector3 position, out PoolObject poolObject)
        where T : Component
    {
        RegisterPoolObject(nameof(GameObject), prefab.GetType().ToString(), prefab.gameObject);
        poolObject = ShowObject(typeof(GameObject), prefab.GetType().ToString(), position);
        return poolObject.InstanceGameObject.GetComponent<T>();
    }

    public GameObject ShowObject(GameObject prefab, Vector3 position, out PoolObject poolObject)
    {
        RegisterPoolObject(typeof(GameObject), prefab.name, prefab);
        poolObject = ShowObject(typeof(GameObject), prefab.name, position);
        return poolObject.InstanceGameObject;
    }

    public GameObject ShowObject(GameObject prefab, Vector3 position, Vector3 scale, out PoolObject poolObject)
    {
        RegisterPoolObject(typeof(GameObject), prefab.name, prefab);
        poolObject = ShowObject(typeof(GameObject), prefab.name, position, scale);
        return poolObject.InstanceGameObject;
    }

    public GameObject ShowObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform transform, out PoolObject poolObject)
    {
        RegisterPoolObject(nameof(GameObject), prefab.name, prefab);
        poolObject = ShowObject(nameof(GameObject), prefab.name, position, rotation, prefab.transform.localScale, transform);
        return poolObject.InstanceGameObject;
    }

    #endregion

    #region Внутренняя логика

    /// <summary>
    /// Строковый ключ типа по умолчанию — используется, когда вызывающий код не привязан к конкретному компоненту.
    /// </summary>
    private static string DefaultTypeKey { get; } = typeof(MonoBehaviour).ToString();

    private static string TypeKeyOf(Component component) => component.GetType().ToString();

    private static GameObject RandomPrefab(PoolEntry entry) =>
        entry.Prefabs[UnityEngine.Random.Range(0, entry.Prefabs.Count)];

    /// <summary>
    /// Корутина создания объектов в пуле.
    /// </summary>
    /// <param name="entry">Пул, в который добавляются объекты.</param>
    /// <param name="count">Количество объектов для создания.</param>
    private IEnumerator InstantiateObjects(PoolEntry entry, int count)
    {
        // Раньше здесь была отдельная проверка objects.Count == 0 без учёта null,
        // что приводило к NullReferenceException, если objects == null.
        if (count < 1 || entry?.Prefabs == null || entry.Prefabs.Count == 0)
        {
            Debug.LogError("[ObjectPoolManager] Невозможно создать объекты пула: некорректные входные данные.");
            yield break;
        }

        for (var i = 0; i < count; i++)
        {
            foreach (var prefab in entry.Prefabs)
            {
                CreateInstance(entry, prefab);
                yield return null;
            }
        }
    }

    private void StartPoolInstantiation(PoolEntry entry, int count)
    {
        StopPoolInstantiation(entry.Key);
        poolCreationCoroutines[entry.Key] = StartCoroutine(InstantiateObjectsTracked(entry, count));
    }

    private IEnumerator InstantiateObjectsTracked(PoolEntry entry, int count)
    {
        yield return InstantiateObjects(entry, count);
        poolCreationCoroutines.Remove(entry.Key);
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

        poolObject.InstanceGameObject.transform.SetParent(
            poolObject.PoolObjectType == PoolObjectType.Particles ? particles : gameObjects);

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
        pools.Remove(key);
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
        OnObjectDestroy(poolObject);
        runningCoroutines.Remove(poolObject.Guid);
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

        if (poolObject.InstanceGameObject != null)
            poolObject.InstanceGameObject.SetActive(false);

        var key = new PoolKey(poolObject.Type, poolObject.Category);
        if (pools.TryGetValue(key, out var entry))
        {
            if (!fullDestroy)
                entry.Queue.Enqueue(poolObject);

            objectOnScene.Remove(poolObject);
        }
        else
        {
            Debug.LogError($"[ObjectPoolManager] Не найден пул с ключом '{key}'.");
        }
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

    #region Monobehaviour

    private void Start()
    {
        //this.SetParentSystem();

        //foreach (var container in resourceContainer.PoolManagerContainer.GetAllData)
        //{
        //    foreach (var pool in container.Value)
        //        RegisterPoolObject(container.GetKey(), pool.Name, pool.Prefabs, pool.Count);
        //}
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
