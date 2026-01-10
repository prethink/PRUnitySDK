using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class ReflectionExtension 
{
    //TODO: Нужен кэш.


    private static List<MethodInfo> GetMethodsHooks(this object instance, string methodHookStage)
    {
        return instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                  .Where(m => m.GetCustomAttribute<MethodHookAttribute>() != null && m.GetCustomAttribute<MethodHookAttribute>().MethodHookStage.Equals(methodHookStage, StringComparison.OrdinalIgnoreCase))
                  .OrderBy(m => m.GetCustomAttribute<MethodHookAttribute>().Order).ToList();
    }

    private static List<MethodInfo> GetStaticMethodHooks(this Type type, string methodHookStage)
    {
        return type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                  .Where(m => m.GetCustomAttribute<MethodHookAttribute>() != null && m.GetCustomAttribute<MethodHookAttribute>().MethodHookStage.Equals(methodHookStage, StringComparison.OrdinalIgnoreCase))
                  .OrderBy(m => m.GetCustomAttribute<MethodHookAttribute>().Order).ToList();
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

    public static void RunMethodHooks(this object instance, MethodHookStage methodHookStage)
    {
        RunMethodHooks(instance, methodHookStage.ToString());
    }

    public static void RunMethodHooks(this object instance, string methodHookStage)
    {
        var methods = instance.GetMethodsHooks(methodHookStage);
        foreach (var method in methods)
            method.Invoke(instance, null);
    }

    public static void RunStaticMethodHooks(this Type type, MethodHookStage methodHookStage)
    {
        RunStaticMethodHooks(type, methodHookStage.ToString());
    }

    public static void RunStaticMethodHooks(this Type type, string methodHookStage)
    {
        var methods = type.GetStaticMethodHooks(methodHookStage);
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
