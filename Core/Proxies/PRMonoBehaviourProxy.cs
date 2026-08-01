using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Базовый прокси-класс для MonoBehaviour.
/// Позволяет делегировать работу на другой объект и получать компоненты через прокси.
/// </summary>
public class PRMonoBehaviourProxy : PRMonoBehaviour
{
    // Ссылка на реальный объект, которому делегируются события
    [SerializeField] protected PRMonoBehaviour refObject;
    [SerializeField] protected HashSet<PRMonoBehaviour> registeredLink = new();

    /// <summary>
    /// Универсальный метод для получения компонента с реального объекта через прокси.
    /// </summary>
    /// <typeparam name="T">Тип компонента</typeparam>
    /// <param name="component">Выходной параметр для найденного компонента</param>
    /// <returns>true, если компонент найден, иначе false</returns>
    public bool TryComponentFromProxy<T>(out T component)
    {
        component = default(T);

        // Пытаемся получить компонент с реального объекта
        if (refObject?.TryGetComponent<T>(out component) == true)
            return true;

        return false;
    }

    public bool Subscribe(PRMonoBehaviour obj)
    {
        return registeredLink.Add(obj);
    }

    public bool Unsubscribe(PRMonoBehaviour obj)
    {
        return registeredLink.Remove(obj);
    }

    protected void Invoke<TArg>(UnityEvent<TArg> unityEvent, TArg arg, Action<PRMonoBehaviour> callback)
    {
        unityEvent?.Invoke(arg);

        if (refObject != null)
            callback(refObject);

        foreach (var obj in registeredLink)
        {
            if (obj != null)
                callback(obj);
        }
    }
}