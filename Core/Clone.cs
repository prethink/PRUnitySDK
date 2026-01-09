using UnityEngine;

/// <summary>
/// Утилита для клонирования объектов Unity с удобными методами копирования.
/// </summary>
public class Clone : MonoBehaviour
{
    /// <summary>
    /// Создает копию текущего объекта и возвращает компонент указанного типа T.
    /// </summary>
    /// <typeparam name="T">Тип компонента, который нужно вернуть из клона.</typeparam>
    /// <param name="position">Локальная позиция клона относительно родителя.</param>
    /// <param name="parent">Родительский трансформ для клона. Если null, объект будет без родителя.</param>
    /// <returns>Компонент типа T из клона.</returns>
    public T Copy<T>(Vector3 position, Transform parent) 
        where T : Component
    {
        Clone instance = Instantiate(this);

        if(parent != null)
            instance.transform.SetParent(parent);

        instance.transform.localPosition = position;

        return instance.GetComponent<T>();
    }

    /// <summary>
    /// Создает копию текущего объекта в указанной позиции без родителя.
    /// </summary>
    /// <typeparam name="T">Тип компонента, который нужно вернуть из клона.</typeparam>
    /// <param name="position">Локальная позиция клона.</param>
    /// <returns>Компонент типа T из клона.</returns>
    public T Copy<T>(Vector3 position)
        where T : Component
    {
        return Copy<T>(position, null);
    }

    /// <summary>
    /// Создает копию текущего объекта с возможностью установить родителя.
    /// </summary>
    /// <typeparam name="T">Тип компонента, который нужно вернуть из клона.</typeparam>
    /// <param name="parent">Родительский трансформ, относительно которого создается объект.</param>
    /// <param name="setParent">Если true, объект будет установлен как дочерний для parent. Иначе позиция родителя используется только для позиции.</param>
    /// <returns>Компонент типа T из клона.</returns>
    public T Copy<T>(Transform parent, bool setParent = true)
        where T : Component
    {
        return Copy<T>(parent.position, setParent ? parent : null);
    }
}