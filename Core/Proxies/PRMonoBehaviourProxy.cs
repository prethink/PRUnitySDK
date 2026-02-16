using UnityEngine;

/// <summary>
/// Базовый прокси-класс для MonoBehaviour.
/// Позволяет делегировать работу на другой объект и получать компоненты через прокси.
/// </summary>
public class PRMonoBehaviourProxy : PRMonoBehaviour
{
    // Ссылка на реальный объект, которому делегируются события
    [SerializeField] protected PRMonoBehaviour refObject;

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
}