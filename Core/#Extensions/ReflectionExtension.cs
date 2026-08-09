using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class ReflectionExtension 
{


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

    /// <summary>
    /// Возвращает методы экземпляра, отмеченные атрибутом <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">Экземпляр не задан.</exception>
    public static List<MethodInfo> GetMethods<T>(this object instance) where T : Attribute
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        return instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(m => m.GetCustomAttribute<T>() != null)
            .ToList();
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

    /// <summary>
    /// Запускает методы экземпляра, относящиеся к указанному этапу хука.
    /// </summary>
    public static void RunMethodHooks(this object instance, MethodHookStage methodHookStage)
    {
        RunMethodHooks(instance, methodHookStage.ToString());
    }

    /// <summary>
    /// Запускает методы экземпляра с указанным строковым именем этапа хука.
    /// </summary>
    public static void RunMethodHooks(this object instance, string methodHookStage)
    {
        var methods = instance.GetMethodsHooks(methodHookStage);
        foreach (var method in methods)
            method.Invoke(instance, null);
    }

    /// <summary>
    /// Запускает статические методы, относящиеся к указанному этапу хука.
    /// </summary>
    public static void RunStaticMethodHooks(this Type type, MethodHookStage methodHookStage)
    {
        RunStaticMethodHooks(type, methodHookStage.ToString());
    }

    /// <summary>
    /// Запускает статические методы с указанным строковым именем этапа хука.
    /// </summary>
    public static void RunStaticMethodHooks(this Type type, string methodHookStage)
    {
        var methods = type.GetStaticMethodHooks(methodHookStage);
        foreach (var method in methods)
            method.Invoke(null, null);
    }

    /// <summary>
    /// Вызывает первый подходящий статический обработчик переопределения свойства.
    /// </summary>
    public static void TryOverrideStaticProperty(this Type type, Type requiredType)
    {
        var method = type.GetOverridePropertyStaticMethods(requiredType).FirstOrDefault();
        method?.Invoke(null, null);
    }

    /// <summary>
    /// Вызывает первый подходящий обработчик переопределения свойства экземпляра.
    /// </summary>
    public static void TryOverrideProperty(this object instance, Type requiredType)
    {
        var method = instance.GetOverridePropertyMethods(requiredType).FirstOrDefault();
        method?.Invoke(instance, null);
    }

    /// <summary>
    /// Вызывает совместимые методы с <see cref="InvokePartialAttribute"/> и объединяет их результаты
    /// по возрастанию значения Order атрибута.
    /// </summary>
    /// <remarks>Поддерживаются результаты типа T, T[] и IEnumerable&lt;T&gt;.</remarks>
    public static IEnumerable<T> CollectPartialResult<T>(this object instance, params object[] parameters)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        List<T> result = new List<T>();
        Type returnType = typeof(T);

        var methods = instance.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(method => IsPartialMethodMatch(method, returnType, parameters))
            .OrderBy(method => method.GetCustomAttribute<InvokePartialAttribute>().Order)
            .ToList();

        foreach (var method in methods)
        {
            var resultMethod = method.Invoke(instance, parameters);

            if (resultMethod is IEnumerable<T> enumerableResult)
                result.AddRange(enumerableResult);
            else if (resultMethod is T singleResult)
                result.Add(singleResult);
            else if (resultMethod == null)
                PRLog.WriteWarning(instance, $"Method '{method.Name}' returned null and was skipped.");
            else
                PRLog.WriteWarning(instance, $"Method '{nameof(CollectPartialResult)}' returned an unsupported result: {resultMethod.GetType()} - {resultMethod}");
        }

        return result;
    }

    private static bool IsPartialMethodMatch(MethodInfo method, Type returnType, object[] parameters)
    {
        if (method.GetCustomAttribute<InvokePartialAttribute>() == null)
            return false;

        var methodReturnType = method.ReturnType;
        var returnTypeMatches = methodReturnType == returnType ||
            (methodReturnType.IsGenericType &&
             methodReturnType.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
             methodReturnType.GetGenericArguments()[0] == returnType) ||
            (methodReturnType.IsArray && methodReturnType.GetElementType() == returnType);

        if (!returnTypeMatches)
            return false;

        var methodParameters = method.GetParameters();
        if (methodParameters.Length != parameters.Length)
            return false;

        for (var i = 0; i < methodParameters.Length; i++)
        {
            if (parameters[i] == null)
            {
                var parameterType = methodParameters[i].ParameterType;
                if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) == null)
                    return false;
            }
            else if (!methodParameters[i].ParameterType.IsInstanceOfType(parameters[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Находит загруженные конкретные классы, реализующие интерфейс <typeparamref name="T"/>.
    /// </summary>
    public static List<Type> FindClassesImplementingInterface<T>()
    {
        var interfaceType = typeof(T);

        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => interfaceType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .ToList();
    }

    /// <summary>
    /// Находит на сцене, включая неактивные объекты, MonoBehaviour-реализации типа <typeparamref name="T"/>.
    /// </summary>
    public static List<T> FindMonoBehaviourImplementations<T>()
    {
        return UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OfType<T>()
            .ToList();
    }
}
