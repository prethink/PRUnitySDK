using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

public static class ClassExtension
{
    #region Кеш заблокированных методов

    /// <summary>
    /// Заблокированные методы по типу. Атрибут задаётся на типе и не меняется в рамках
    /// домена, поэтому результат чтения переиспользуется: без кеша GetCustomAttribute
    /// выполнялся на каждом OnTriggerStay/OnCollisionStay, то есть каждый физический
    /// кадр на каждом контакте, и к нему добавлялся линейный поиск по List.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, HashSet<string>> disabledMethodsCache = new();

    /// <summary>
    /// Общий пустой набор для типов без атрибута. Типов без блокировок подавляющее
    /// большинство, поэтому отдельный объект под каждый из них создавать незачем.
    /// </summary>
    private static readonly HashSet<string> emptyDisabledMethods = new();

    /// <summary>
    /// Возвращает набор заблокированных имён для типа, при первом обращении читая атрибут.
    /// </summary>
    /// <param name="type">Проверяемый тип.</param>
    /// <returns>Набор имён методов; пустой набор, если блокировок нет.</returns>
    private static HashSet<string> GetDisabledMethods(Type type)
    {
        return disabledMethodsCache.GetOrAdd(type, key =>
        {
            // Атрибут объявлен с Inherited = true и AllowMultiple = false, поэтому при
            // наличии атрибутов и на базовом, и на производном типе будет возвращён
            // только ближайший: список наследника заменяет базовый, а не дополняет его.
            var attribute = key.GetCustomAttribute<DisableMethodsAttribute>();
            if (attribute == null || attribute.MethodsToDisable.Count == 0)
                return emptyDisabledMethods;

            return new HashSet<string>(attribute.MethodsToDisable, StringComparer.Ordinal);
        });
    }

    #endregion

    /// <summary>
    /// Проверяет, заблокирован ли метод для данного экземпляра через атрибут.
    /// </summary>
    public static bool IsMethodDisabled(this object obj, string methodName)
    {
        if (obj == null) 
            throw new ArgumentNullException(nameof(obj));

        return obj.GetType().IsMethodDisabled(methodName);
    }

    public static bool IsNull(this object obj)
    {
        if (obj == null)
            return true;

        if (obj is UnityEngine.Object unityObj)
            return unityObj == null;

        return false;
    }

    /// <summary>
    /// Проверяет, заблокирован ли метод для данного типа через атрибут.
    /// </summary>
    public static bool IsMethodDisabled(this Type type, string methodName)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        return GetDisabledMethods(type).Contains(methodName);
    }

    //TODO: чтобы использовать scope который позволить выполнять заблокированные методы внутри класса, нужно будет переделать логику на использование стека вызовов.
}
