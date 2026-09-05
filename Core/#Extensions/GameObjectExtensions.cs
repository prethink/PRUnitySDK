using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class GameObjectExtensions 
{
    /// <summary>
    /// Возвращает существующий компонент или добавляет новый.
    /// </summary>
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        T newComponent = go.GetComponent<T>();

        if (newComponent == null)
        {
            newComponent = go.AddComponent<T>();
        }

        return newComponent;
    }

    /// <summary>
    /// Пытается найти компонент среди объекта и его потомков.
    /// </summary>
    public static bool TryGetComponentInChildren<T>(this GameObject gameObject, out T result, bool includeInactive = false)
    {
        result = gameObject.GetComponentInChildren<T>(includeInactive);
        return result != null;
    }

    /// <summary>
    /// Возвращает первый компонент среди объекта и его потомков.
    /// </summary>
    public static T GetComponentInChildren<T>(this GameObject gameObject, bool includeInactive = false)
    {
        return gameObject.GetComponentInChildren<T>(includeInactive);
    }

    /// <summary>
    /// Ищет компонент сначала на объекте, затем среди его потомков.
    /// </summary>
    public static T GetComponentInSelfOrChildren<T>(this GameObject gameObject, bool includeInactive = false) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component != null)
            return component;

        return gameObject.GetComponentInChildren<T>(includeInactive);
    }

    /// <summary>
    /// Возвращает компоненты объекта и его потомков без повторов компонентов корня.
    /// </summary>
    public static List<T> GetComponentsInSelfOrChildren<T>(this GameObject gameObject, bool includeInactive = false) where T : Component
    {
        return new List<T>(gameObject.GetComponentsInChildren<T>(includeInactive));
    }

    /// <summary>
    /// Ищет компонент в потомках родителя; при отсутствии родителя использует сам объект.
    /// </summary>
    public static T ParentGetComponentInChildren<T>(this GameObject gameObject)
    {
        var parent = gameObject.transform.parent != null ? gameObject.transform.parent : gameObject.transform;
        return parent.gameObject.GetComponentInChildren<T>();
    }

    /// <summary>
    /// Возвращает компоненты из потомков родителя; при отсутствии родителя использует сам объект.
    /// </summary>
    public static T[] ParentGetComponentsInChildren<T>(this GameObject gameObject)
    {
        var parent = gameObject.transform.parent != null ? gameObject.transform.parent : gameObject.transform;
        return parent.gameObject.GetComponentsInChildren<T>();
    }

    /// <summary>
    /// Ищет компонент на родителе; при отсутствии родителя использует сам объект.
    /// </summary>
    public static T ParentGetComponent<T>(this GameObject gameObject)
    {
        var parent = gameObject.transform.parent != null ? gameObject.transform.parent : gameObject.transform;
        return parent.gameObject.GetComponent<T>();
    }

    /// <summary>
    /// Принудительно перестраивает активные RectTransform в иерархии объекта.
    /// </summary>
    public static void RefreshLayoutGroupsImmediateAndRecursive(this GameObject root)
    {
        foreach (var layoutGroup in root.GetComponentsInChildren<RectTransform>())
        {
            if(layoutGroup.gameObject.activeSelf)
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup);
        }
    }

    /// <summary>
    /// Возвращает компонент, найденный на объекте либо в иерархии от её корня.
    /// </summary>
    public static T GetComponentInObjectHierarchy<T>(this GameObject obj)
        where T : Component
    {
        T component = default(T);
        obj.TryFindComponentInObjectHierarchy<T>(out component);
        return component;
    }

    /// <summary>
    /// Пытается найти компонент на объекте либо в иерархии от её корня.
    /// </summary>
    public static bool TryFindComponentInObjectHierarchy<T>(this GameObject obj, out T component)
        where T : Component
    {
        return obj.TryFindComponentInObjectChildren<T>(obj.transform.root, out component);
    }

    /// <summary>
    /// Проверяет объект, затем ищет компонент среди потомков указанного корня.
    /// </summary>
    /// <param name="root">Корень области поиска.</param>
    public static bool TryFindComponentInObjectChildren<T>(this GameObject obj, Transform root, out T component)
        where T : Component
    {
        if (obj == null)
            throw new System.ArgumentNullException(nameof(obj));

        if (root == null)
            throw new System.ArgumentNullException(nameof(root));

        component = obj.GetComponent<T>();

        if (component == null)
            component = root.GetComponentInChildren<T>();

        return component != null;
    }

}
