using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Предметы, которые игрок получает не покупкой.
/// </summary>
/// <remarks>
/// Отвечает на один вопрос: достаётся ли эта вещь откуда-то ещё. Витрине этого хватает,
/// чтобы не раздавать даром то, что игрок и так найдёт в подарке или получит за
/// достижение, — и при этом не знать ни об одной из тех систем.
/// <para>
/// Системы регистрируются сами через <see cref="IReservedItemsProvider"/>: менеджер не
/// перечисляет их и не лезет в их каталоги. Разовые случаи — предмет, лежащий в ящике
/// на уровне, — резервируются вручную через <see cref="Reserve"/>.
/// </para>
/// </remarks>
public class ReservedItemsManager : SingletonProviderBase<ReservedItemsManager>
{
    private readonly List<IReservedItemsProvider> providers = new();
    private readonly Dictionary<string, string> manual = new(StringComparer.Ordinal);

    /// <summary>
    /// Собранный состав: идентификатор предмета — кто его выдаёт.
    /// </summary>
    /// <remarks>
    /// Кешируется: витрина спрашивает об этом на каждой перерисовке карточки, а обход
    /// всех наград каждый раз обошёлся бы дороже самой отрисовки.
    /// </remarks>
    private Dictionary<string, string> cached;

    /// <summary>
    /// Ставит на учёт системы, помеченные <see cref="AutoReservedItemsProviderAttribute"/>.
    /// </summary>
    /// <remarks>
    /// Вызывается при инициализации SDK. Так модуль объявляет себя источником там, где
    /// живёт, и не правит контейнер менеджеров ради одной строки регистрации.
    /// </remarks>
    public void RegisterAutoProviders()
    {
        foreach (Type type in FindAutoProviderTypes())
        {
            try
            {
                if (Activator.CreateInstance(type) is IReservedItemsProvider provider)
                    Register(provider);
            }
            catch (Exception exception)
            {
                PRLog.WriteError(typeof(ReservedItemsManager), $"Не удалось создать {type.Name}: {exception}");
            }
        }
    }

    /// <summary>
    /// Собирает помеченные типы, пригодные к созданию.
    /// </summary>
    /// <remarks>
    /// Сканируется только сборка, в которой объявлен <see cref="IReservedItemsProvider"/>:
    /// обход всех сборок домена стоил бы заметного времени на старте, а системы проекта
    /// всё равно живут здесь.
    /// </remarks>
    private static List<Type> FindAutoProviderTypes()
    {
        Type[] assemblyTypes;

        try
        {
            assemblyTypes = typeof(IReservedItemsProvider).Assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            PRLog.WriteError(typeof(ReservedItemsManager), $"Не удалось просмотреть сборку: {exception}");
            assemblyTypes = exception.Types.Where(type => type != null).ToArray();
        }

        var found = new List<(Type Type, int Order)>();

        foreach (Type type in assemblyTypes)
        {
            var attribute = type.GetCustomAttribute<AutoReservedItemsProviderAttribute>(inherit: false);

            if (attribute == null || !attribute.Enabled)
                continue;

            if (!typeof(IReservedItemsProvider).IsAssignableFrom(type))
            {
                PRLog.WriteWarning(typeof(ReservedItemsManager),
                    $"{type.Name} помечен как источник, но не реализует {nameof(IReservedItemsProvider)}.");
                continue;
            }

            if (typeof(Component).IsAssignableFrom(type))
            {
                PRLog.WriteWarning(typeof(ReservedItemsManager),
                    $"{type.Name} — компонент сцены: он появляется вместе с объектом и регистрируется сам.");
                continue;
            }

            if (type.IsAbstract || type.ContainsGenericParameters)
            {
                PRLog.WriteWarning(typeof(ReservedItemsManager),
                    $"{type.Name} нельзя создать автоматически: тип абстрактный или обобщённый.");
                continue;
            }

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                PRLog.WriteWarning(typeof(ReservedItemsManager),
                    $"У {type.Name} нет публичного конструктора без параметров — регистрируйте вручную.");
                continue;
            }

            found.Add((type, attribute.Order));
        }

        return found
            .OrderBy(entry => entry.Order)
            .ThenBy(entry => entry.Type.Name, StringComparer.Ordinal)
            .Select(entry => entry.Type)
            .ToList();
    }

    /// <summary>
    /// Ставит систему на учёт.
    /// </summary>
    public void Register(IReservedItemsProvider provider)
    {
        if (provider == null || providers.Contains(provider))
            return;

        providers.Add(provider);
        Invalidate();
    }

    /// <summary>
    /// Снимает систему с учёта.
    /// </summary>
    public void Unregister(IReservedItemsProvider provider)
    {
        if (provider != null && providers.Remove(provider))
            Invalidate();
    }

    /// <summary>
    /// Резервирует предмет вручную.
    /// </summary>
    /// <remarks>
    /// Для того, что живёт не в каталоге, а на уровне: ящик с вещью внутри появляется
    /// вместе со сценой и о себе сообщает сам.
    /// </remarks>
    public void Reserve(string itemId, string source)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return;

        manual[itemId] = source ?? string.Empty;
        Invalidate();
    }

    /// <summary>
    /// Снимает ручную бронь.
    /// </summary>
    public void Release(string itemId)
    {
        if (!string.IsNullOrWhiteSpace(itemId) && manual.Remove(itemId))
            Invalidate();
    }

    /// <summary>
    /// Предмет можно получить не покупкой.
    /// </summary>
    public bool IsReserved(ItemDefinitionBase item)
    {
        return item != null && IsReserved(item.Id);
    }

    /// <summary>
    /// Предмет можно получить не покупкой.
    /// </summary>
    public bool IsReserved(string itemId)
    {
        return TryGetSource(itemId, out _);
    }

    /// <summary>
    /// Кто выдаёт предмет.
    /// </summary>
    /// <returns><see langword="false"/>, если предмет ниоткуда не выдаётся.</returns>
    public bool TryGetSource(string itemId, out string source)
    {
        source = null;

        return !string.IsNullOrWhiteSpace(itemId)
               && GetReservations().TryGetValue(itemId, out source);
    }

    /// <summary>
    /// Сбрасывает собранный состав.
    /// </summary>
    /// <remarks>
    /// Нужен, когда каталоги меняются на ходу: в редакторе и при переключении набора базы.
    /// </remarks>
    public void Invalidate()
    {
        cached = null;
    }

    /// <summary>
    /// Все брони: и от систем, и выставленные вручную.
    /// </summary>
    private Dictionary<string, string> GetReservations()
    {
        if (cached != null)
            return cached;

        var reservations = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (IReservedItemsProvider provider in providers)
            Collect(provider, reservations);

        foreach (KeyValuePair<string, string> pair in manual)
            reservations[pair.Key] = pair.Value;

        cached = reservations;
        return cached;
    }

    /// <summary>
    /// Забирает состав у одной системы.
    /// </summary>
    /// <remarks>
    /// Сломавшаяся система не отменяет ответ остальных: витрина продолжит работать,
    /// просто её вещи не будут считаться зарезервированными.
    /// </remarks>
    private static void Collect(IReservedItemsProvider provider, Dictionary<string, string> reservations)
    {
        if (provider == null)
            return;

        try
        {
            IEnumerable<string> ids = provider.GetReservedItemIds();

            if (ids == null)
                return;

            foreach (string id in ids)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    reservations[id] = provider.ReservationSource;
            }
        }
        catch (Exception exception)
        {
            PRLog.WriteError(typeof(ReservedItemsManager), $"{provider.GetType().Name}: {exception}");
        }
    }
}
