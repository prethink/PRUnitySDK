using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class GameEventEntity : IEntity
{
    #region Поля и свойства

    private string name;

    #endregion

    #region IEntity

    public long Id => -1;

    public string Name => name;

    public Type EntityType => typeof(GameEventEntity);

    public bool OnScene => true;

    public bool InPool => false;

    public EntityLifeTime LifeTime => EntityLifeTime.Infinity;

    public DamageType DamageType => DamageType.Generic;

    public GameObject gameObject => GameEventsFactory.GetOrCreateGameObject(name);

    public IEntityInfo Info => new EntityInfoImplementer(Guid.Empty, nameof(GameEventEntity), PRUnitySDK.Database.Sprites.Entities.GameEventEntity); 

    public void DestroyEntity() { }

    public void DestroyEntity(EntityDestroyOptions options) { }

    public void GenerateId(Func<int> register) { }

    public long GetCurrentLevel()
    {
        return 99999;
    }

    public DamageData GetDamageData()
    {
        return new DamageData()
        {
            Damage = 9999999,
        };
    }

    public void EntityInitialize()
    {

    }


    #endregion

    #region Конструкторы

    public GameEventEntity(string name)
    {
        this.name = name;
    }

    #endregion
}

public static class GameEventEntityFactory
{
    public static GameEventEntity CreateEventGame()
    {
        return new GameEventEntity("Game");
    }

    public static GameEventEntity CreateEventSpawn()
    {
        return new GameEventEntity("Spawn");
    }

    public static GameEventEntity CreateEventSuicide()
    {
        return new GameEventEntity("Suicide");
    }
}

public static class GameEventsFactory
{
    private static Dictionary<string, GameObject> gameObjects = new();
    private static GameObject root;

    /// <summary>
    /// Корневой объект для всех игровых событий.
    /// </summary>
    private static GameObject Root
    {
        get
        {
            if (root == null)
            {
                root = new GameObject("[GameEvents]");
                Object.DontDestroyOnLoad(root);
            }
            return root;
        }
    }

    /// <summary>
    /// Получает существующий или создаёт новый GameObject для события.
    /// </summary>
    /// <param name="name">Название объекта.</param>
    /// <returns>GameObject события.</returns>
    public static GameObject GetOrCreateGameObject(string name)
    {
        if (gameObjects.TryGetValue(name, out var existing))
        {
            return existing;
        }

        // Проверка, не создан ли объект в корне
        var childTransform = Root.transform.Find(name);
        if (childTransform != null)
        {
            gameObjects[name] = childTransform.gameObject;
            return childTransform.gameObject;
        }

        // Создание нового объекта
        var go = new GameObject(name);
        go.transform.SetParent(Root.transform);
        gameObjects[name] = go;
        return go;
    }

    /// <summary>
    /// Удаляет объект из фабрики и сцены.
    /// </summary>
    public static void DestroyGameObject(string name)
    {
        if (gameObjects.TryGetValue(name, out var go))
        {
            Object.Destroy(go);
            gameObjects.Remove(name);
        }
    }

    /// <summary>
    /// Удаляет все объекты фабрики.
    /// </summary>
    public static void DestroyAll()
    {
        foreach (var go in gameObjects.Values)
        {
            Object.Destroy(go);
        }
        gameObjects.Clear();
    }
}