using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class ReflectionExtension 
{
    private static List<MethodInfo> GetPartialMethodsForInitialized(this object instance, InitializeType initializeType)
    {
        return instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                  .Where(m => m.GetCustomAttribute<InitializeMethodAttribute>() != null && m.GetCustomAttribute<InitializeMethodAttribute>().InitializeType == initializeType)
                  .OrderBy(m => m.GetCustomAttribute<InitializeMethodAttribute>().Order).ToList();
    }

    private static List<MethodInfo> GetPartialStaticMethodsForInitialized(this Type type, InitializeType initializeType)
    {
        return type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                  .Where(m => m.GetCustomAttribute<InitializeMethodAttribute>() != null && m.GetCustomAttribute<InitializeMethodAttribute>().InitializeType == initializeType)
                  .OrderBy(m => m.GetCustomAttribute<InitializeMethodAttribute>().Order).ToList();
    }

    private static List<MethodInfo> GetOverridePropertyMethods(this object instance, Type requiredType)
    {
        return instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(m => m.GetCustomAttribute<OverridePropertyAttribute>() != null && m.GetCustomAttribute<OverridePropertyAttribute>().OverrideType.IsAssignableFrom(requiredType))
            .OrderBy(m => m.GetCustomAttribute<OverridePropertyAttribute>().Order).ToList(); ;
    }

    private static List<MethodInfo> GetOverridePropertyStaticMethods(this Type type, Type requiredType)
    {
        return type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(m => m.GetCustomAttribute<OverridePropertyAttribute>() != null && m.GetCustomAttribute<OverridePropertyAttribute>().OverrideType.IsAssignableFrom(requiredType))
            .OrderBy(m => m.GetCustomAttribute<OverridePropertyAttribute>().Order).ToList(); ;
    }

    public static List<MethodInfo> GetMethods<T>(this object instance) where T : Attribute
    {
        return instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(m => m.GetCustomAttribute<Attribute>() != null).ToList(); ;
    }

    private static List<MethodInfo> GetMatchingMethods(this object instance, Type returnType, Type[] parameterTypes)
    {
        return instance.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(m =>
            {
                var attr = m.GetCustomAttribute<InvokePartialAttribute>();
                if (attr == null)
                    return false;

                Type methodReturnType = m.ReturnType;

                bool returnTypeMatches = methodReturnType == returnType ||
                    (methodReturnType.IsGenericType && methodReturnType.GetGenericTypeDefinition() == typeof(IEnumerable<>) && methodReturnType.GetGenericArguments()[0] == returnType) ||
                    (methodReturnType.IsArray && methodReturnType.GetElementType() == returnType);

                if (!returnTypeMatches) 
                    return false;

                var methodParams = m.GetParameters().Select(p => p.ParameterType).ToArray();
                return methodParams.SequenceEqual(parameterTypes);
            })
            .OrderBy(m => m.GetCustomAttribute<InvokePartialAttribute>().Order)
            .ToList();
    }

    public static void InvokePartialMethods(this object instance, InitializeType initializeType)
    {
        var methods = instance.GetPartialMethodsForInitialized(initializeType);
        foreach (var method in methods)
            method.Invoke(instance, null);
    }

    public static void InvokePartialStaticMethods(this Type type, InitializeType initializeType)
    {
        var methods = type.GetPartialStaticMethodsForInitialized(initializeType);
        foreach (var method in methods)
            method.Invoke(null, null);
    }

    public static void TryOverrideStaticProperty(this Type type, Type requiredType)
    {
        var method = type.GetOverridePropertyStaticMethods(requiredType).FirstOrDefault();
        method?.Invoke(null, null);
    }

    public static void TryOverrideProperty(this object instance, Type requiredType)
    {
        var method = instance.GetOverridePropertyMethods(requiredType).FirstOrDefault();
        method?.Invoke(instance, null);
    }

    public static IEnumerable<T> CollectPartialResult<T>(this object instance, params object[] parameters)
    {
        List<T> result = new List<T>();
        Type returnType = typeof(T);
        Type[] parameterTypes = parameters.Select(p => p.GetType()).ToArray();

        var methods = instance.GetMatchingMethods(returnType, parameterTypes);

        foreach (var method in methods)
        {
            var resultMethod = method.Invoke(instance, parameters);

            if (resultMethod is IEnumerable<T> enumerableResult)
                result.AddRange(enumerableResult);
            else if (resultMethod is T singleResult)
                result.Add(singleResult);
            else
                PRLog.WriteWarning(instance, $"Метод '{nameof(CollectPartialResult)}' не обработал возвращаемый результат {resultMethod.GetType()} - {resultMethod.ToString()}");
        }

        return result;
    }
}
