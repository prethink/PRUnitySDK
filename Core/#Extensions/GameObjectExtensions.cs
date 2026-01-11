using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class GameObjectExtensions 
{
    public static bool TryGetComponentInChildren<T>(this GameObject gameObject, out T result, bool includeInactive = false)
    {
        result = gameObject.GetComponentInChildren<T>(includeInactive);
        return result != null;
    }

    public static T GetComponentInChildren<T>(this GameObject gameObject, bool includeInactive = false)
    {
        return gameObject.GetComponentInChildren<T>(includeInactive);
    }

    public static T GetComponentInSelfOrChildren<T>(this GameObject gameObject, bool includeInactive = false) where T : Component
    {
        // Сначала пытаемся найти компонент на самом объекте
        T component = gameObject.GetComponent<T>();
        if (component != null)
            return component;

        // Если не нашли — ищем среди дочерних
        return gameObject.GetComponentInChildren<T>(includeInactive);
    }

    public static List<T> GetComponentsInSelfOrChildren<T>(this GameObject gameObject, bool includeInactive = false) where T : Component
    {
        var result = new List<T>();

        // Добавляем компоненты на самом объекте
        result.AddRange(gameObject.GetComponents<T>());

        // Добавляем компоненты в дочерних объектах
        result.AddRange(gameObject.GetComponentsInChildren<T>(includeInactive));

        return result;
    }


    public static T ParentGetComponentInChildren<T>(this GameObject gameObject)
    {
        var parent = gameObject.transform.parent != null ? gameObject.transform.parent : gameObject.transform;
        return parent.gameObject.GetComponentInChildren<T>();
    }

    public static T[] ParentGetComponentsInChildren<T>(this GameObject gameObject)
    {
        var parent = gameObject.transform.parent != null ? gameObject.transform.parent : gameObject.transform;
        return parent.gameObject.GetComponentsInChildren<T>();
    }

    public static T ParentGetComponent<T>(this GameObject gameObject)
    {
        var parent = gameObject.transform.parent != null ? gameObject.transform.parent : gameObject.transform;
        return parent.gameObject.GetComponent<T>();
    }

    public static void RefreshLayoutGroupsImmediateAndRecursive(this GameObject root)
    {
        foreach (var layoutGroup in root.GetComponentsInChildren<RectTransform>())
        {
            if(layoutGroup.gameObject.activeSelf)
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup);
        }
    }


    ///// <summary>
    ///// Попытаться найти компонент в иерархии объектов (для Component).
    ///// </summary>
    ///// <typeparam name="T">Тип компонента.</typeparam>
    ///// <param name="obj">Компонент на котором происходит поиск.</param>
    ///// <param name="component">Искомый компонент.</param>
    ///// <returns>Компонент или null.</returns>
    //public static bool TryGetComponentRecursive<T>(this GameObject obj, out T component)
    //    where T : Component
    //{
    //    component = obj.GetComponent<T>();

    //    if (component == null)
    //        obj.TryFindComponentInObjectHierarchy(obj.transform, out component);

    //    return component != null;
    //}

    /// <summary>
    /// Получить компонент в иерархии объектов (для GameObject).
    /// </summary>
    /// <typeparam name="T">Тип компонента.</typeparam>
    /// <param name="obj">GameObject на котором происходит поиск.</param>
    /// <returns>Компонент или null.</returns>
    public static T GetComponentInObjectHierarchy<T>(this GameObject obj)
        where T : Component
    {
        T component = default(T);
        obj.TryFindComponentInObjectHierarchy<T>(out component);
        return component;
    }

    /// <summary>
    /// Попытаться найти компонент в иерархии объектов (для Component).
    /// </summary>
    /// <typeparam name="T">Тип компонента.</typeparam>
    /// <param name="obj">Компонент на котором происходит поиск.</param>
    /// <param name="component">Искомый компонент.</param>
    /// <returns>Компонент или null.</returns>
    public static bool TryFindComponentInObjectHierarchy<T>(this GameObject obj, out T component)
        where T : Component
    {
        return obj.TryFindComponentInObjectChildren<T>(obj.transform.root, out component);
    }

    ///// <summary>
    ///// Попытаться найти компонент в иерархии объектов (для GameObject).
    ///// </summary>
    ///// <typeparam name="T">Тип компонента.</typeparam>
    ///// <param name="obj">GameObject на котором происходит поиск.</param>
    ///// <param name="component">Искомый компонент.</param>
    ///// <returns>Компонент или null.</returns>
    //public static bool TryFindComponentInObjectHierarchy<T>(this GameObject obj, Transform root, out T component)
    //    where T : Component
    //{
    //    component = obj.GetComponent<T>();

    //    if (component == null)
    //        component = root.GetComponentInChildren<T>();

    //    return component != null;
    //}

    /// <summary>
    /// Попытаться найти компонент в иерархии объектов (для Component).
    /// </summary>
    /// <typeparam name="T">Тип компонента.</typeparam>
    /// <param name="obj">Компонент на котором происходит поиск.</param>
    /// <param name="component">Искомый компонент.</param>
    /// <returns>Компонент или null.</returns>
    public static bool TryFindComponentInObjectChildren<T>(this GameObject obj, Transform root, out T component)
        where T : Component
    {
        component = obj.GetComponent<T>();

        if (component == null)
            component = obj.GetComponentInChildren<T>();

        return component != null;
    }


    ///// <summary>
    ///// Установить родительский объект System (для GameObject).
    ///// </summary>
    ///// <param name="obj">GameObject которому будет выставлен родитель.</param>
    //public static void SetParentSystem(this GameObject obj)
    //{
    //    var systemTransform = InstanceUtils.GetOrCreateEmptySystemObject();
    //    obj.transform.SetParent(systemTransform.transform);
    //}

    ///// <summary>
    ///// Попытаться найти компонент от корня (для GameObject).
    ///// </summary>
    ///// <typeparam name="T">Тип компонента.</typeparam>
    ///// <param name="obj">GameObject на котором происходит поиск.</param>
    ///// <param name="component">Искомый компонент.</param>
    ///// <returns>Компонент или null.</returns>
    //public static bool TryFindComponentFromRoot<T>(this GameObject obj, out T component)
    //    where T : Component
    //{
    //    component = obj.GetComponent<T>();

    //    var root = obj.transform.root;
    //    if (root != null && component == null)
    //        component = root.GetComponentInChildren<T>();

    //    return component != null;
    //}

    ///// <summary>
    ///// Попытаться найти компоненты в иерархии объектов (для GameObject).
    ///// </summary>
    ///// <typeparam name="T">Тип компонента.</typeparam>
    ///// <param name="obj">GameObject на котором происходит поиск.</param>
    ///// <param name="components">Искомая коллекция компонентов.</param>
    ///// <returns>Компоненты или пустая коллекция.</returns>
    //public static bool TryFindComponentsInObjectHierarchy<T>(this GameObject obj, out List<T> components)
    //    where T : Component
    //{
    //    components = new List<T>();
    //    components.AddRange(obj.GetComponents<T>());
    //    components.AddRange(obj.GetComponentsInParent<T>());
    //    components.AddRange(obj.GetComponentsInChildren<T>());
    //    return components.Count > 0;
    //}
}
