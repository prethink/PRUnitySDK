using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class ReflectionExtension
{
    #region Кеш рефлексии

    /// <summary>
    /// Метод-хук вместе с посчитанным один раз количеством параметров. Количество нужно
    /// на каждом вызове (чтобы выбрать, передавать аргументы или нет), а GetParameters()
    /// каждый раз создаёт новый массив - поэтому значение считается при построении кеша.
    /// </summary>
    private readonly struct HookMethod
    {
        public readonly MethodInfo Method;

        public readonly int ParameterCount;

        public HookMethod(MethodInfo method)
        {
            Method = method;
            ParameterCount = method.GetParameters().Length;
        }
    }

    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>
    /// Хуки экземпляра по типу и названию стадии. Набор методов у типа неизменен в рамках
    /// домена, поэтому результат сканирования переиспользуется - без этого каждый вызов
    /// RunMethodHooks заново перебирал все методы типа и по несколько раз читал атрибут
    /// у каждого из них (а ProjectData.Clone() дёргается на каждом сохранении).
    /// </summary>
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, HookMethod[]>> instanceHooksCache = new();

    /// <summary>Статические хуки по типу и названию стадии.</summary>
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, HookMethod[]>> staticHooksCache = new();

    /// <summary>Обработчики переопределения свойства экземпляра по паре (тип, требуемый тип).</summary>
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, MethodInfo>> overridePropertyCache = new();

    /// <summary>Статические обработчики переопределения свойства по паре (тип, требуемый тип).</summary>
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, MethodInfo>> overridePropertyStaticCache = new();

    private static HookMethod[] GetMethodsHooks(this object instance, string methodHookStage)
    {
        return GetHookMethods(instance.GetType(), methodHookStage, instanceHooksCache, InstanceFlags);
    }

    private static HookMethod[] GetStaticMethodHooks(this Type type, string methodHookStage)
    {
        return GetHookMethods(type, methodHookStage, staticHooksCache, StaticFlags);
    }

    private static HookMethod[] GetHookMethods(
        Type type,
        string methodHookStage,
        ConcurrentDictionary<Type, ConcurrentDictionary<string, HookMethod[]>> cache,
        BindingFlags bindingFlags)
    {
        // Сравнение стадии регистронезависимое, поэтому компаратор задаётся у вложенного словаря.
        var stages = cache.GetOrAdd(type, _ => new ConcurrentDictionary<string, HookMethod[]>(StringComparer.OrdinalIgnoreCase));

        return stages.GetOrAdd(methodHookStage, stage => BuildHookMethods(type, stage, bindingFlags));
    }

    private static HookMethod[] BuildHookMethods(Type type, string methodHookStage, BindingFlags bindingFlags)
    {
        var hooks = new List<(MethodInfo Method, int Order)>();

        foreach (var method in type.GetMethods(bindingFlags))
        {
            var attribute = method.GetCustomAttribute<MethodHookAttribute>();

            if (attribute == null || !attribute.MethodHookStage.Equals(methodHookStage, StringComparison.OrdinalIgnoreCase) || !attribute.IsEnabled)
                continue;

            hooks.Add((method, attribute.Order));
        }

        return hooks
            .OrderBy(hook => hook.Order)
            .Select(hook => new HookMethod(hook.Method))
            .ToArray();
    }

    private static MethodInfo GetOverridePropertyMethod(this object instance, Type requiredType)
    {
        return GetOverridePropertyMethod(instance.GetType(), requiredType, overridePropertyCache, InstanceFlags);
    }

    private static MethodInfo GetOverridePropertyStaticMethod(this Type type, Type requiredType)
    {
        return GetOverridePropertyMethod(type, requiredType, overridePropertyStaticCache, StaticFlags);
    }

    private static MethodInfo GetOverridePropertyMethod(
        Type type,
        Type requiredType,
        ConcurrentDictionary<Type, ConcurrentDictionary<Type, MethodInfo>> cache,
        BindingFlags bindingFlags)
    {
        var byRequiredType = cache.GetOrAdd(type, _ => new ConcurrentDictionary<Type, MethodInfo>());

        return byRequiredType.GetOrAdd(requiredType, required => type.GetMethods(bindingFlags)
            .Select(method => (Method: method, Attribute: method.GetCustomAttribute<OverridePropertyAttribute>()))
            .Where(item => item.Attribute != null && item.Attribute.OverrideType.IsAssignableFrom(required))
            .OrderBy(item => item.Attribute.Order)
            .Select(item => item.Method)
            .FirstOrDefault());
    }

    #endregion

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
        RunMethodHooks(instance, methodHookStage, (object[])null);
    }

    /// <summary>
    /// Запускает методы указанного этапа, передавая им аргументы. Хуки без параметров
    /// вызываются как раньше - это позволяет на одном этапе держать и те, и другие
    /// (например, ProjectData передаёт в Cloning целевой объект, а старые хуки без
    /// параметров продолжают работать).
    /// </summary>
    public static void RunMethodHooks(this object instance, MethodHookStage methodHookStage, params object[] arguments)
    {
        RunMethodHooks(instance, methodHookStage.ToString(), arguments);
    }

    /// <summary>
    /// Запускает методы указанного этапа по строковому имени, передавая им аргументы.
    /// </summary>
    public static void RunMethodHooks(this object instance, string methodHookStage, params object[] arguments)
    {
        var argumentCount = arguments?.Length ?? 0;
        var methods = instance.GetMethodsHooks(methodHookStage);

        foreach (var hook in methods)
        {
            if (hook.ParameterCount == 0)
            {
                hook.Method.Invoke(instance, null);
                continue;
            }

            if (hook.ParameterCount == argumentCount)
            {
                hook.Method.Invoke(instance, arguments);
                continue;
            }

            PRLog.WriteWarning(instance, $"Hook '{hook.Method.DeclaringType?.Name}.{hook.Method.Name}' expects " +
                $"{hook.ParameterCount} argument(s) on stage '{methodHookStage}', but {argumentCount} was passed. Skipped.");
        }
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
        foreach (var hook in methods)
            hook.Method.Invoke(null, null);
    }

    /// <summary>
    /// Вызывает первый подходящий статический обработчик переопределения свойства.
    /// </summary>
    public static void TryOverrideStaticProperty(this Type type, Type requiredType)
    {
        var method = type.GetOverridePropertyStaticMethod(requiredType);
        method?.Invoke(null, null);
    }

    /// <summary>
    /// Вызывает первый подходящий обработчик переопределения свойства экземпляра.
    /// </summary>
    public static void TryOverrideProperty(this object instance, Type requiredType)
    {
        var method = instance.GetOverridePropertyMethod(requiredType);
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
