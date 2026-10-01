using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if PRSDK_RUNNER_PROFILING
using Unity.Profiling;
#endif

/// <summary>
/// Фазы кадра, в которых <see cref="PRMonoBehaviour"/> получает вызовы от раннера.
/// </summary>
[Flags]
public enum PRUpdatePhase
{
    /// <summary>Ни одной: тип не переопределил ни одного хука обновления.</summary>
    None = 0,

    /// <summary><c>PRPreUpdate</c>, <c>PRUpdate</c>, <c>PRPostUpdate</c>.</summary>
    Update = 1,

    /// <summary><c>PRLateUpdate</c>.</summary>
    LateUpdate = 2,

    /// <summary><c>PRFixedUpdate</c>.</summary>
    FixedUpdate = 4
}

/// <summary>
/// Центральный раннер обновлений <see cref="PRMonoBehaviour"/>.
/// </summary>
/// <remarks>
/// Unity вызывает <c>Update</c>, <c>LateUpdate</c> и <c>FixedUpdate</c> у каждого компонента,
/// где они объявлены. Когда они жили в базовом классе, движок переходил в управляемый код
/// у тысяч объектов за кадр, хотя большинство только проверяло паузу и ничего не делало.
/// Теперь их вызывает один <see cref="PRMonoBehaviourHost"/>, а раннер обходит только тех,
/// кто переопределил хук: это проверяется рефлексией один раз на тип.
/// <para>
/// Поведение повторяет Unity: вызываются только включённые объекты (регистрация в
/// <c>OnEnable</c>, снятие в <c>OnDisable</c>), первый вызов - не раньше <c>Start</c>,
/// порядок между типами задаёт <see cref="DefaultExecutionOrderAttribute"/>, исключение в
/// одном объекте не обрывает остальных. Пауза проверяется перед каждым объектом: если её
/// включили посреди прохода, остальные в этом кадре уже не вызываются - как и раньше,
/// когда каждый проверял её сам.
/// </para>
/// </remarks>
public static class PRUpdateRunner
{
    #region Поля и свойства

    private static readonly Dictionary<Type, PRUpdatePhase> phaseCache = new();
    private static readonly Dictionary<Type, int> orderCache = new();

    private static readonly PRUpdateGroups updates = new(PRUpdatePhase.Update);
    private static readonly PRUpdateGroups lateUpdates = new(PRUpdatePhase.LateUpdate);
    private static readonly PRUpdateGroups fixedUpdates = new(PRUpdatePhase.FixedUpdate);

    private static readonly Action<PRMonoBehaviour> callUpdate = b => b.RunnerUpdate();
    private static readonly Action<PRMonoBehaviour> callLateUpdate = b => b.RunnerLateUpdate();
    private static readonly Action<PRMonoBehaviour> callFixedUpdate = b => b.RunnerFixedUpdate();

    private static bool hostRequested;

    /// <summary>
    /// Сколько объектов получает <c>PRUpdate</c>.
    /// </summary>
    public static int UpdateCount => updates.Count;

    /// <summary>
    /// Сколько объектов получает <c>PRLateUpdate</c>.
    /// </summary>
    public static int LateUpdateCount => lateUpdates.Count;

    /// <summary>
    /// Сколько объектов получает <c>PRFixedUpdate</c>.
    /// </summary>
    public static int FixedUpdateCount => fixedUpdates.Count;

    #endregion

    #region Регистрация

    /// <summary>
    /// Ставит объект в фазы, хуки которых переопределил его тип.
    /// </summary>
    internal static void Register(PRMonoBehaviour behaviour)
    {
        Type type = behaviour.GetType();
        PRUpdatePhase phases = GetPhases(type);

        if (phases == PRUpdatePhase.None)
            return;

        int order = GetOrder(type);

        if ((phases & PRUpdatePhase.Update) != 0)
            updates.Add(behaviour, order);

        if ((phases & PRUpdatePhase.LateUpdate) != 0)
            lateUpdates.Add(behaviour, order);

        if ((phases & PRUpdatePhase.FixedUpdate) != 0)
            fixedUpdates.Add(behaviour, order);

        EnsureHost();
    }

    /// <summary>
    /// Снимает объект со всех фаз.
    /// </summary>
    internal static void Unregister(PRMonoBehaviour behaviour)
    {
        updates.Remove(behaviour);
        lateUpdates.Remove(behaviour);
        fixedUpdates.Remove(behaviour);
    }

    /// <summary>
    /// Фазы, хуки которых переопределил тип. Считается один раз на тип.
    /// </summary>
    public static PRUpdatePhase GetPhases(Type type)
    {
        if (phaseCache.TryGetValue(type, out PRUpdatePhase phases))
            return phases;

        phases = PRUpdatePhase.None;

        if (Overrides(type, "PRUpdate") || Overrides(type, "PRPreUpdate") || Overrides(type, "PRPostUpdate"))
            phases |= PRUpdatePhase.Update;

        if (Overrides(type, "PRLateUpdate"))
            phases |= PRUpdatePhase.LateUpdate;

        if (Overrides(type, "PRFixedUpdate"))
            phases |= PRUpdatePhase.FixedUpdate;

        phaseCache[type] = phases;
        return phases;
    }

    private static bool Overrides(Type type, string name)
    {
        MethodInfo method = type.GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null);

        return method != null && method.DeclaringType != typeof(PRMonoBehaviour);
    }

    private static int GetOrder(Type type)
    {
        if (orderCache.TryGetValue(type, out int order))
            return order;

        // Как у Unity: порядок задаёт атрибут самого класса, у наследника он не действует.
        var attribute = (DefaultExecutionOrder)Attribute.GetCustomAttribute(type, typeof(DefaultExecutionOrder), false);
        order = attribute != null ? attribute.order : 0;
        orderCache[type] = order;
        return order;
    }

    /// <summary>
    /// Хост - единственный, у кого Unity вызывает методы кадра. Объекты сцены включаются
    /// раньше, чем SDK создаёт его при инициализации, поэтому первая регистрация создаёт
    /// хост сама: иначе до инициализации ничего бы не обновлялось.
    /// </summary>
    private static void EnsureHost()
    {
        if (hostRequested || !Application.isPlaying)
            return;

        hostRequested = true;
        _ = PRMonoBehaviourHost.Instance;
    }

    /// <summary>
    /// Без перезагрузки домена статика переживает выход из Play Mode: списки держали бы
    /// уничтоженные объекты прошлого запуска.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        updates.Clear();
        lateUpdates.Clear();
        fixedUpdates.Clear();
        hostRequested = false;
    }

    #endregion

    #region Проходы

    /// <summary>
    /// Проход <c>PRUpdate</c>. Зовёт хост.
    /// </summary>
    internal static void RunUpdate() => updates.Run(callUpdate);

    /// <summary>
    /// Проход <c>PRLateUpdate</c>. Зовёт хост.
    /// </summary>
    internal static void RunLateUpdate() => lateUpdates.Run(callLateUpdate);

    /// <summary>
    /// Проход <c>PRFixedUpdate</c>. Зовёт хост.
    /// </summary>
    internal static void RunFixedUpdate() => fixedUpdates.Run(callFixedUpdate);

    #endregion

    #region Списки

    /// <summary>
    /// Списки одной фазы, разложенные по порядку выполнения.
    /// </summary>
    private sealed class PRUpdateGroups
    {
        private readonly PRUpdatePhase phase;
        private readonly SortedList<int, PRUpdateList> groups = new();

        public PRUpdateGroups(PRUpdatePhase phase)
        {
            this.phase = phase;
        }

        public int Count
        {
            get
            {
                var total = 0;
                foreach (PRUpdateList list in groups.Values)
                    total += list.Count;
                return total;
            }
        }

        public void Add(PRMonoBehaviour behaviour, int order)
        {
            if (!groups.TryGetValue(order, out PRUpdateList list))
            {
                list = new PRUpdateList(phase);
                groups.Add(order, list);
            }

            list.Add(behaviour);
        }

        public void Remove(PRMonoBehaviour behaviour)
        {
            ref PRUpdateSlot slot = ref behaviour.GetRunnerSlot(phase);
            slot.List?.Remove(behaviour, ref slot);
        }

        public void Run(Action<PRMonoBehaviour> call)
        {
            IList<PRUpdateList> lists = groups.Values;

            for (var i = 0; i < lists.Count; i++)
            {
                if (!lists[i].Run(call))
                    return;
            }
        }

        public void Clear()
        {
            foreach (PRUpdateList list in groups.Values)
                list.Clear();
        }
    }

    /// <summary>
    /// Массив со счётчиком: объект помнит свой индекс, поэтому снятие - перестановка с
    /// последним за O(1), а не поиск по списку.
    /// </summary>
    /// <remarks>
    /// Во время прохода порядок трогать нельзя: снятые оставляют дыру, новые ждут в очереди.
    /// Дыры сжимаются, а очередь вливается после прохода - новые объекты получат вызов со
    /// следующего кадра, как и у Unity.
    /// </remarks>
    internal sealed class PRUpdateList
    {
        private const int Pending = -2;

        private readonly PRUpdatePhase phase;
        private readonly List<PRMonoBehaviour> pending = new();
        private PRMonoBehaviour[] items = new PRMonoBehaviour[32];
        private int count;
        private int holes;
        private bool running;

        public PRUpdateList(PRUpdatePhase phase)
        {
            this.phase = phase;
        }

        public int Count => count - holes + pending.Count;

        public void Add(PRMonoBehaviour behaviour)
        {
            ref PRUpdateSlot slot = ref behaviour.GetRunnerSlot(phase);
            if (slot.List != null)
                return;

            if (running)
            {
                pending.Add(behaviour);
                slot = new PRUpdateSlot(this, Pending);
                return;
            }

            Append(behaviour, ref slot);
        }

        public void Remove(PRMonoBehaviour behaviour, ref PRUpdateSlot slot)
        {
            int index = slot.Index;
            slot = default;

            if (index == Pending)
            {
                pending.Remove(behaviour);
                return;
            }

            if (running)
            {
                items[index] = null;
                holes++;
                return;
            }

            int last = count - 1;
            PRMonoBehaviour moved = items[last];
            items[index] = moved;
            items[last] = null;
            count--;

            if (index != last)
                moved.GetRunnerSlot(phase) = new PRUpdateSlot(this, index);
        }

        /// <returns><see langword="false"/>, если проход прервала пауза.</returns>
        public bool Run(Action<PRMonoBehaviour> call)
        {
            var completed = true;
            running = true;

            try
            {
                for (var i = 0; i < count; i++)
                {
                    PRMonoBehaviour behaviour = items[i];

                    if (behaviour is null || !behaviour.HasStarted)
                        continue;

                    if (PRUnitySDK.PauseManager.IsLogicPaused)
                    {
                        completed = false;
                        break;
                    }

                    try
                    {
#if PRSDK_RUNNER_PROFILING
                        using (GetMarker(behaviour.GetType()).Auto())
#endif
                        call(behaviour);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, behaviour);
                    }
                }
            }
            finally
            {
                running = false;
                Compact();
                FlushPending();
            }

            return completed;
        }

        public void Clear()
        {
            for (var i = 0; i < count; i++)
            {
                if (items[i] is not null)
                    items[i].GetRunnerSlot(phase) = default;
                items[i] = null;
            }

            foreach (PRMonoBehaviour behaviour in pending)
                behaviour.GetRunnerSlot(phase) = default;

            pending.Clear();
            count = 0;
            holes = 0;
            running = false;
        }

        private void Append(PRMonoBehaviour behaviour, ref PRUpdateSlot slot)
        {
            if (count == items.Length)
                Array.Resize(ref items, items.Length * 2);

            items[count] = behaviour;
            slot = new PRUpdateSlot(this, count);
            count++;
        }

        private void Compact()
        {
            if (holes == 0)
                return;

            var write = 0;
            for (var read = 0; read < count; read++)
            {
                PRMonoBehaviour behaviour = items[read];
                if (behaviour is null)
                    continue;

                if (write != read)
                {
                    items[write] = behaviour;
                    behaviour.GetRunnerSlot(phase) = new PRUpdateSlot(this, write);
                }

                write++;
            }

            for (int i = write; i < count; i++)
                items[i] = null;

            count = write;
            holes = 0;
        }

        private void FlushPending()
        {
            if (pending.Count == 0)
                return;

            foreach (PRMonoBehaviour behaviour in pending)
                Append(behaviour, ref behaviour.GetRunnerSlot(phase));

            pending.Clear();
        }
    }

#if PRSDK_RUNNER_PROFILING
    private static readonly Dictionary<Type, ProfilerMarker> markers = new();

    /// <summary>
    /// Отдельная строка профайлера на тип: без неё всё время уходит под хост.
    /// </summary>
    private static ProfilerMarker GetMarker(Type type)
    {
        if (!markers.TryGetValue(type, out ProfilerMarker marker))
        {
            marker = new ProfilerMarker(type.Name);
            markers[type] = marker;
        }

        return marker;
    }
#endif

    #endregion
}

/// <summary>
/// Где объект стоит в списке одной фазы раннера.
/// </summary>
internal struct PRUpdateSlot
{
    public readonly PRUpdateRunner.PRUpdateList List;
    public readonly int Index;

    public PRUpdateSlot(PRUpdateRunner.PRUpdateList list, int index)
    {
        List = list;
        Index = index;
    }
}
