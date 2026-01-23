using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Система пула объектов на сцене.
/// </summary>
public class ObjectPoolSystem : MonoBehaviour
{
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
    /// Пул объектов сцены.
    /// </summary>
    private Dictionary<string, Dictionary<string, Queue<PoolObject>>> pool = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Данные префабов.
    /// </summary>
    private Dictionary<string, Dictionary<string, List<GameObject>>> prefabData = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Объекты вытянутые из очереди и находящиеся на сцене.
    /// </summary>
    private List<PoolObject> objectOnScene = new List<PoolObject>();

    /// <summary>
    /// Запущенные корутины.
    /// </summary>
    private Dictionary<Guid, Coroutine> runningCoroutines = new();

    public List<PoolSystemTableData> GenerateReport()
    {
        var result = new List<PoolSystemTableData>();
        foreach (var root in pool)
        {
            foreach (var category in root.Value)
            {
                var objectOnSceneCount = objectOnScene.Count(x => x.Type == root.Key && x.Category == category.Key);
                var hideCount = category.Value.Count;
                var data = new PoolSystemTableData() 
                { 
                    Type = root.Key, 
                    Category = category.Key, 
                    TotalCount = objectOnSceneCount + hideCount,
                    ShowCount = objectOnSceneCount, 
                    HideCount = hideCount
                };
                result.Add(data);
            }
        }
        return result;
    }

    #endregion

    #region Методы

    /// <summary>
    /// Регистрирует новый пул объектов.
    /// </summary>
    /// <param name="type">Компонент, для которого регистрируется пул объектов.</param>
    /// <param name="category">Категория пула.</param>
    /// <param name="obj">Префаб объекта.</param>
    /// <param name="count">Количество объектов для создания (по умолчанию 1).</param>
    public void RegisterPoolObject(string type, string category, GameObject obj, int count = 1)
    {
        RegisterPoolObject(type, category, new List<GameObject>() { obj }, count);
    }

    /// <summary>
    /// Регистрирует новый пул объектов.
    /// </summary>
    /// <param name="category">Категория пула.</param>
    /// <param name="obj">Префаб объекта.</param>
    /// <param name="count">Количество объектов для создания (по умолчанию 1).</param>
    public void RegisterPoolObject(string category, GameObject obj, int count = 1)
    {
        var type = typeof(MonoBehaviour).ToString();
        RegisterPoolObject(type, category, new List<GameObject>() { obj }, count);
    }

    /// <summary>
    /// Регистрирует новый пул объектов.
    /// </summary>
    /// <param name="category">Категория пула.</param>
    /// <param name="obj">Префаб объекта.</param>
    /// <param name="count">Количество объектов для создания (по умолчанию 1).</param>
    public void RegisterPoolObject(string category, List<GameObject> objs, int count = 1)
    {
        var type = typeof(MonoBehaviour).ToString();
        RegisterPoolObject(type, category, objs.ToList(), count);
    }

    /// <summary>
    /// Регистрирует новый пул объектов.
    /// </summary>
    /// <param name="component">Компонент.</param>
    /// <param name="category">Категория пула.</param>
    /// <param name="obj">Префаб объекта.</param>
    /// <param name="count">Количество объектов для создания (по умолчанию 1).</param>
    public void RegisterPoolObject(Component component, string category, GameObject obj, int count = 1)
    {
        var type = component.GetType().ToString();
        RegisterPoolObject(type, category, new List<GameObject>() { obj }, count);
    }

    /// <summary>
    /// Регистрирует новый пул объектов.
    /// </summary>
    /// <param name="component">Компонент.</param>
    /// <param name="category">Категория пула.</param>
    /// <param name="objects">Префабы объектов.</param>
    /// <param name="count">Количество объектов для создания (по умолчанию 1).</param>
    public void RegisterPoolObject(Component component, string category, List<GameObject> objects, int count = 1)
    {
        var type = component.GetType().ToString();
        RegisterPoolObject(type, category, objects, count);
    }

    /// <summary>
    /// Регистрирует новый пул объектов.
    /// </summary>
    /// <param name="type">Компонент, для которого регистрируется пул объектов.</param>
    /// <param name="category">Категория пула.</param>
    /// <param name="obj">Префаб объекта.</param>
    /// <param name="count">Количество объектов для создания (по умолчанию 1).</param>
    public void RegisterPoolObject(Type type, string category, GameObject obj, int count = 1)
    {
        var stringType = type.ToString();
        RegisterPoolObject(stringType, category, new List<GameObject>() { obj }, count);
    }

    /// <summary>
    /// Регистрирует новый пул объектов.
    /// </summary>
    /// <param name="type">Тип.</param>
    /// <param name="category">Категория пула.</param>
    /// <param name="objects">Префабы объектов.</param>
    /// <param name="count">Количество объектов для создания (по умолчанию 1).</param>
    public void RegisterPoolObject(string type, string category, List<GameObject> objects, int count = 1)
    {
        if (pool.ContainsKey(type) && pool[type].ContainsKey(category))
            return;

        var queue = new Queue<PoolObject>();
        if (pool.ContainsKey(type))
        {
            if (pool[type] == null)
            {
                pool[type] = CreateActionValue(category, out queue);
                prefabData[type.ToString()] = CreatePrefabActionValue(category, objects);
                StartCoroutine(InstantiateObjects(type, category, queue, objects, count));
            }
            else if (!pool[type].ContainsKey(category))
            {
                CreateActionValue(category, out queue);
                StartCoroutine(InstantiateObjects(type, category, queue, objects, count));
                pool[type.ToString()].Add(category, queue);
                prefabData[type.ToString()].Add(category, objects);

            }
            else
            {
                var countInPool = pool[type.ToString()][category].Count;
                if (countInPool <= count)
                    StartCoroutine(InstantiateObjects(type, category, pool[type.ToString()][category], objects, count - countInPool));
            }
        }
        else
        {
            pool[type.ToString()] = CreateActionValue(category, out queue);
            prefabData[type.ToString()] = CreatePrefabActionValue(category, objects);
            StartCoroutine(InstantiateObjects(type, category, queue, objects, count));
        }
    }

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    /// <param name="category">Категория объекта.</param>
    /// <param name="transform">Трансформ, задающий позицию и вращение объекта.</param>
    /// <param name="scaler">Размер объекта.</param>
    /// <returns>Игровой объект.</returns>
    public PoolObject ShowObject(string category, Transform transform, Vector3 scaler)
    {
        var type = typeof(MonoBehaviour).ToString();
        return ShowObject(type, category, transform.position, transform.rotation, scaler, transform);
    }

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    /// <param name="type">Тип, который требуется для отображения объекта.</param>
    /// <param name="category">Категория объекта.</param>
    /// <param name="transform">Трансформ, задающий позицию и вращение объекта.</param>
    /// <param name="scaler">Размер объекта.</param>
    /// <returns>Игровой объект.</returns>
    public PoolObject ShowObject(Type type, string category, Transform transform, Vector3 scaler)
    {
        return ShowObject(type.ToString(), category, transform.position, transform.rotation, scaler, transform);
    }

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    /// <param name="type">Тип, который требуется для отображения объекта.</param>
    /// <param name="category">Категория объекта.</param>
    /// <param name="transform">Трансформ, задающий позицию и вращение объекта.</param>
    /// <returns>Игровой объект.</returns>
    public PoolObject ShowObject(Type type, string category, Transform transform)
    {
        return ShowObject(type.ToString(), category, transform.position, transform.rotation, Vector3.one, transform);
    }

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    /// <param name="type">Тип, который требуется для отображения объекта.</param>
    /// <param name="category">Категория объекта.</param>
    /// <param name="position">Позиция.</param>
    /// <returns>Игровой объект.</returns>
    public PoolObject ShowObject(Type type, string category, Vector3 position)
    {
        return ShowObject(type.ToString(), category, position, Quaternion.identity, Vector3.one, null);
    }

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    /// <param name="type">Тип, который требуется для отображения объекта.</param>
    /// <param name="category">Категория объекта.</param>
    /// <param name="position">Позиция.</param>
    /// <param name="scaler">Размер объекта.</param>
    /// <returns>Игровой объект.</returns>
    public PoolObject ShowObject(Type type, string category, Vector3 position, Vector3 scaler)
    {
        return ShowObject(type.ToString(), category, position, Quaternion.identity, scaler, null);
    }

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    /// <param name="component">Компонент.</param>
    /// <param name="category">Категория объекта.</param>
    /// <param name="transform">Трансформ, задающий позицию и вращение объекта.</param>
    /// <param name="scaler">Размер объекта.</param>
    /// <returns>Игровой объект.</returns>
    public PoolObject ShowObject(Component component, string category, Transform transform, Vector3 scaler)
    {
        var type = component.GetType().ToString();
        return ShowObject(type, category, transform.position, transform.rotation, scaler, transform);
    }

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    /// <param name="component">Компонент.</param>
    /// <param name="category">Категория объекта.</param>
    /// <param name="transform">Трансформ, задающий позицию и вращение объекта.</param>
    /// <returns>Игровой объект.</returns>
    public PoolObject ShowObject(Component component, string category, Transform transform)
    {
        var type = component.GetType().ToString();
        return ShowObject(type, category, transform.position, transform.rotation, Vector3.one, null);
    }

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    /// <param name="component">Компонент.</param>
    /// <param name="category">Категория объекта.</param>
    /// <param name="position">Позиция.</param>
    /// <returns>Игровой объект.</returns>
    public PoolObject ShowObject(Component component, string category, Vector3 position)
    {
        var type = component.GetType().ToString();
        return ShowObject(type, category, position, Quaternion.identity, Vector3.one, null);
    }

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    /// <param name="component">Компонент.</param>
    /// <param name="category">Категория объекта.</param>
    /// <param name="position">Позиция.</param>
    /// <param name="scaler">Размер объекта.</param>
    /// <returns>Игровой объект.</returns>
    public PoolObject ShowObject(Component component, string category, Vector3 position, Vector3 scaler)
    {
        var type = component.GetType().ToString();
        return ShowObject(type, category, position, Quaternion.identity, scaler, null);
    }

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    /// <param name="category">Категория объекта.</param>
    /// <param name="transform">Трансформ, задающий позицию и вращение объекта.</param>
    /// <returns>Игровой объект.</returns>
    public PoolObject ShowObject(string category, Transform transform)
    {
        var type = typeof(MonoBehaviour).ToString();
        return ShowObject(type, category, transform.position, transform.rotation, Vector3.one, transform);
    }

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    /// <param name="category">Категория объекта.</param>
    /// <param name="position">Позиция.</param>
    /// <returns>Игровой объект.</returns>
    public PoolObject ShowObject(string category, Vector3 position)
    {
        var type = typeof(MonoBehaviour).ToString();
        return ShowObject(type, category, position, Quaternion.identity, Vector3.one, null);
    }

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    /// <param name="category">Категория объекта.</param>
    /// <param name="position">Позиция.</param>
    /// <param name="scaler">Размер объекта.</param>
    /// <returns>Игровой объект.</returns>
    public PoolObject ShowObject(string category, Vector3 position, Vector3 scaler)
    {
        var type = typeof(MonoBehaviour);
        return ShowObject(type.ToString(), category, position, Quaternion.identity, scaler, null);
    }

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    /// <param name="category">Категория объекта.</param>
    /// <returns>Игровой объект.</returns>
    public PoolObject ShowObject(string category)
    {
        var type = typeof(MonoBehaviour).ToString();
        return ShowObject(type, category, Vector3.zero, Quaternion.identity, Vector3.one, null);
    }

    /// <summary>
    /// Отображает объект на сцене.
    /// </summary>
    /// <param name="type">Тип, который требуется для отображения объекта.</param>
    /// <param name="category">Категория объекта.</param>
    /// <param name="position">Позиция.</param>
    /// <param name="rotation">Вращение.</param>
    /// <param name="scaler">Размер объекта.</param>
    /// <returns>Игровой объект.</returns>
    public PoolObject ShowObject(string type, string category, Vector3 position, Quaternion rotation, Vector3 scaler, Transform parent)
    {
        if (pool.ContainsKey(type.ToString()) && pool[type.ToString()].ContainsKey(category))
        {
            var currentQueue = pool[type.ToString()][category];
            if (currentQueue.Count > 0)
            {
                var poolObject = currentQueue.Dequeue();
                if(poolObject.InstanceGameObject == null)
                {
                    var prefabs = prefabData[type.ToString()][category];
                    CreateInstance(type, category, currentQueue, prefabs[UnityEngine.Random.Range(0, prefabs.Count)]);
                    return ShowObject(type, category, position, rotation, scaler, parent);
                }

                poolObject.InstanceGameObject.transform.position = position;
                poolObject.InstanceGameObject.transform.rotation = rotation;
                poolObject.InstanceGameObject.transform.localScale = scaler;
                if(parent != null)
                    poolObject.InstanceGameObject.transform.SetParent(parent.transform);

                poolObject.InstanceGameObject.SetActive(true);
                poolObject.InstanceGameObject.GetComponent<IPoolable>()?.InitializationPoolObject();

                if (poolObject.Lifetime > TimeSpan.Zero)
                    StartCoroutineTracking(poolObject.Guid, BackToQueue(poolObject));
                objectOnScene.Add(poolObject);
                return poolObject;
            }
            else
            {
                var prefabs = prefabData[type.ToString()][category];
                CreateInstance(type, category, currentQueue, prefabs[UnityEngine.Random.Range(0, prefabs.Count)]);
                return ShowObject(type, category, position, rotation, scaler, parent);
            }
        }

        return null;
    }

    public T ShowEntity<T>(T prefab, Vector3 position)
        where T : EntityBase
    {
        return ShowEntity<T>(prefab, position, Quaternion.identity, null);
    }

    public T ShowEntity<T>(T prefab, Vector3 position, Quaternion rotation, Transform transform) 
        where T : EntityBase
    {
        RegisterPoolObject("Entity", prefab.GetPoolKey(), prefab.gameObject);
        var poolObject = ShowObject("Entity", prefab.GetPoolKey(), position, rotation, prefab.transform.localScale, transform);

        return poolObject.InstanceGameObject.GetComponent<T>();
    }

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

    public T ShowObject<T>(T prefab, Vector3 position, out PoolObject poolObject)
        where T : Component
    {
        RegisterPoolObject(nameof(GameObject), prefab.GetType().ToString(), prefab.gameObject);
        poolObject = ShowObject(typeof(GameObject), prefab.GetType().ToString(), position);

        return poolObject.InstanceGameObject.GetComponent<T>();
    }

    public GameObject ShowObject(GameObject prefab, Vector3 position, out PoolObject poolObject)
    {
        RegisterPoolObject(typeof(GameObject), prefab.name, prefab.gameObject);
        poolObject = ShowObject(typeof(GameObject), prefab.name, position);

        return poolObject.InstanceGameObject;
    }

    public GameObject ShowObject(GameObject prefab, Vector3 position, Vector3 scale, out PoolObject poolObject)
    {
        RegisterPoolObject(typeof(GameObject), prefab.name, prefab.gameObject);
        poolObject = ShowObject(typeof(GameObject), prefab.name, position, scale);

        return poolObject.InstanceGameObject;
    }

    public GameObject ShowObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform transform, out PoolObject poolObject)
    {
        RegisterPoolObject(nameof(GameObject), prefab.name, prefab.gameObject);
        poolObject = ShowObject(nameof(GameObject), prefab.name, position, rotation, prefab.transform.localScale, transform);

        return poolObject.InstanceGameObject;
    }

    public T ShowEntity<T>(T prefab, Transform transform)
        where T : EntityBase
    {
        return ShowEntity<T>(prefab, Vector3.zero, Quaternion.identity, transform); 
    }

    /// <summary>
    /// Корутину для создания объектов в пуле.
    /// </summary>
    /// <param name="type">Тип объекта.</param>
    /// <param name="category">Категория объекта.</param>
    /// <param name="queue">Очередь объектов.</param>
    /// <param name="objects">Префабы объектов.</param>
    /// <param name="count">Количество объектов для создания.</param>
    private IEnumerator InstantiateObjects(string type, string category, Queue<PoolObject> queue, List<GameObject> objects, int count)
    {
        if (count < 1 || queue == null || objects == null || objects.Count == 0)
        {
            string errorMsg = "";
            if (count < 1) errorMsg += "Count < 1. ";
            if (queue == null) errorMsg += "Queue<GameObject> is null. ";
            if (objects == null) errorMsg += "GameObject is null. ";
            if (objects.Count == 0) errorMsg += "GameObject is null. ";
            Debug.LogError(errorMsg);
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < objects.Count; j++)
            {
                CreateInstance(type, category, queue, objects[j]);
                yield return null;
            }
        }
    }

    /// <summary>
    /// Создание экземпляра объекта.
    /// </summary>
    /// <param name="category">Категория объекта.</param>
    /// <param name="name">Имя объекта.</param>
    /// <param name="queue">Очередь объектов.</param>
    /// <param name="obj">Префаб объекта.</param>
    private void CreateInstance(string category, string name, Queue<PoolObject> queue, GameObject obj)
    {
        //var instance = diContainer.InstantiatePrefab(obj);
        //var poolObject = new PoolObject(category, name, instance);
        //if (instance.TryGetComponent<IPoolable>(out var basePoolObject))
        //    basePoolObject.RegisterPoolObject(poolObject);

        //poolObject.InstanceGameObject.transform.SetParent(poolObject.PoolObjectType == PoolObjectType.Particles ? particles : gameObjects);
        //instance.SetActive(false);
        //queue.Enqueue(poolObject);
    }

    /// <summary>
    /// Создание значения действия для очереди.
    /// </summary>
    /// <param name="actionValue">Имя действия.</param>
    /// <param name="queue">Очередь объектов.</param>
    /// <returns>Словарь с очередью объектов.</returns>
    private Dictionary<string, Queue<PoolObject>> CreateActionValue(string actionValue, out Queue<PoolObject> queue)
    {
        queue = new Queue<PoolObject>();

        return new Dictionary<string, Queue<PoolObject>>(StringComparer.OrdinalIgnoreCase)
        {
            { actionValue, queue }
        };
    }

    /// <summary>
    /// Создание значения префаба.
    /// </summary>
    /// <param name="actionValue">Имя действия.</param>
    /// <param name="prefab">Префаб объекта.</param>
    /// <returns>Словарь с префабом.</returns>
    private Dictionary<string, List<GameObject>> CreatePrefabActionValue(string actionValue, List<GameObject> prefabs)
    {
        return new Dictionary<string, List<GameObject>>(StringComparer.OrdinalIgnoreCase)
        {
            { actionValue, prefabs.ToList() }
        };
    }

    /// <summary>
    /// Очистка данных при смене сцены.
    /// </summary>
    /// <param name="currentScene">Текущая сцена.</param>
    /// <param name="nextScene">Следующая сцена.</param>
    private void OnSceneEnd(string currentScene, string nextScene)
    {
        ClearData();
    }

    public void ClearData()
    {
        StopAllRunningCoroutines();
        foreach (var item in pool)
        {
            foreach (var query in item.Value)
            {
                foreach (var instance in query.Value)
                {
                    instance.Dispose();
                }
            }
        }
        pool.Clear();
        prefabData.Clear();
        objectOnScene.Clear();
    }

    /// <summary>
    /// Останавливает все запущенные корутины.
    /// </summary>
    private void StopAllRunningCoroutines()
    {
        foreach (var coroutine in runningCoroutines)
            StopCoroutine(coroutine.Value); // Останавливаем каждую корутину
        runningCoroutines.Clear(); // Очищаем список корутин
    }

    /// <summary>
    /// Запускает корутину и добавляет её в список запущенных корутин.
    /// </summary>
    /// <param name="routine">Корутину для запуска.</param>
    /// <param name="guid">Уникальный идентификатор.</param>
    /// <returns>Корутину.</returns>
    private Coroutine StartCoroutineTracking(Guid guid, IEnumerator routine)
    {
        var coroutine = StartCoroutine(routine);
        runningCoroutines.Add(guid, coroutine); // Добавляем корутину в список
        return coroutine;
    }

    /// <summary>
    /// Корутину для возврата объекта в пул по истечении времени жизни.
    /// </summary>
    /// <param name="poolObject">Пул-объект, который нужно вернуть.</param>
    /// <returns>Корутину.</returns>
    private IEnumerator BackToQueue(PoolObject poolObject)
    {
        yield return new WaitForSeconds((float)poolObject.Lifetime.TotalSeconds);
        OnObjectDestroy(poolObject);
        RemoveCoroutineFromList(poolObject.Guid);
    }

    /// <summary>
    /// Удаляет корутину из списка запущенных корутин.
    /// </summary>
    /// <param name="coroutine">Ссылка на корутину, которую нужно удалить.</param>
    private void RemoveCoroutineFromList(Guid guid)
    {
        if(runningCoroutines.TryGetValue(guid, out var coroutine))
            runningCoroutines.Remove(guid);
    }

    /// <summary>
    /// Уничтожение объекта пула.
    /// </summary>
    /// <param name="poolObject">Пул-объект, который нужно убрать в очередь.</param>
    public void OnObjectDestroy(PoolObject poolObject, bool fullDestroy = false)
    {
        if (poolObject != null)
        {
            if(poolObject?.InstanceGameObject != null)
                poolObject?.InstanceGameObject.SetActive(false);

            if (pool.ContainsKey(poolObject.Type.ToString()) && pool[poolObject.Type.ToString()].ContainsKey(poolObject.Category))
            {
                if(!fullDestroy)
                    pool[poolObject.Type.ToString()][poolObject.Category].Enqueue(poolObject);

                objectOnScene.Remove(poolObject);
            }
            else
            {
                Debug.LogError($"Not found poll container with type {poolObject.Type} and category {poolObject.Category}");
            }
        }
    }

    /// <summary>
    /// Уничтожение объекта по ссылке на GameObject.
    /// </summary>
    /// <param name="obj">GameObject, который нужно убрать обратно в очередь.</param>
    public void OnObjectDestroy(GameObject obj)
    {
        var poolObject = objectOnScene.FirstOrDefault(x => x.InstanceGameObject.Equals(obj));
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

public class PoolSystemTableData
{
    public string Type;
    public string Category;
    public long TotalCount;
    public long ShowCount;
    public long HideCount;
}
