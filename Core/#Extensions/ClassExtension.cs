using System;
using System.Reflection;

public static class ClassExtension 
{
    /// <summary>
    /// ѕровер€ет, заблокирован ли метод дл€ данного экземпл€ра через атрибут.
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
    /// ѕровер€ет, заблокирован ли метод дл€ данного типа через атрибут.
    /// </summary>
    public static bool IsMethodDisabled(this Type type, string methodName)
    {
        if (type == null) 
            throw new ArgumentNullException(nameof(type));

        var attr = type.GetCustomAttribute<DisableMethodsAttribute>();
        if (attr == null) 
            return false;

        return attr.MethodsToDisable.Contains(methodName);
    }

    //TODO: чтобы использовать scope который позволить выполн€ть заблокированные методы внутри класса, нужно будет переделать логику на использование стека вызовов.
}
