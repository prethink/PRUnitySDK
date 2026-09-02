using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Собирает персональные модификаторы характеристик сущности.
/// </summary>
/// <remarks>
/// Источников два: компоненты-провайдеры на дочерних объектах — надетая шляпа, питомец,
/// набор предметов — и модификаторы, добавленные из кода через <see cref="AddModifier"/>
/// для того, у чего объекта на сцене нет: бустера, VIP-статуса, способности.
/// </remarks>
[RequireComponent(typeof(EntityLinkBase))]
public class StatModifierCollector : PRMonoBehaviour, IEntityEquipmentChangedEvent
{
    private readonly List<IStatModifiersProvider> providers = new();
    private readonly List<IStatModifierProvider> singleProviders = new();
    private readonly Dictionary<object, List<StatModifier>> ownedModifiers = new();
    private readonly FloatPropertyContainer properties = new();

    private EntityLinkBase entityLink;

    /// <summary>
    /// Возвращает текущий снимок всех собранных модификаторов.
    /// </summary>
    public IEnumerable<StatModifier> GetAllModifiers =>
        providers.SelectMany(GetModifiersSafely)
            .Concat(singleProviders.Select(provider => provider.StatModifier))
            .Concat(ownedModifiers.Values.SelectMany(list => list))
            .Where(modifier => modifier != null);

    protected override void InitializationComponents()
    {
        entityLink ??= GetComponent<EntityLinkBase>();
        base.InitializationComponents();
    }

    protected override void Start()
    {
        base.Start();
        CollectProviders(false);
    }

    /// <summary>
    /// Повторно собирает активные дочерние источники модификаторов.
    /// </summary>
    public void CollectProviders()
    {
        CollectProviders(true);
    }

    /// <summary>
    /// Применяет персональные модификаторы к базовому значению характеристики.
    /// </summary>
    public float ApplyStatModifier(Enumeration stat, float currentValue)
    {
        return properties.Get(stat, currentValue);
    }

    /// <summary>
    /// Добавляет модификатор от владельца, у которого нет объекта на сцене.
    /// </summary>
    /// <remarks>
    /// Модификаторы владельца переживают пересборку: при смене экипировки они
    /// применяются заново. Один владелец может добавить несколько модификаторов.
    /// </remarks>
    /// <param name="owner">Тот, кто выдал модификатор: бустер, статус, способность.</param>
    /// <param name="modifier">Модификатор характеристики.</param>
    public void AddModifier(object owner, StatModifier modifier)
    {
        if (owner.IsNull() || modifier == null)
            return;

        if (!ownedModifiers.TryGetValue(owner, out List<StatModifier> modifiers))
        {
            modifiers = new List<StatModifier>();
            ownedModifiers[owner] = modifiers;
        }

        modifiers.Add(modifier);
        ApplyModifier(owner, modifier);
        NotifyStatsChanged();
    }

    /// <summary>
    /// Убирает все модификаторы владельца.
    /// </summary>
    /// <param name="owner">Тот, кто выдавал модификаторы.</param>
    /// <returns><c>true</c>, если у владельца были модификаторы.</returns>
    public bool RemoveModifiers(object owner)
    {
        if (owner.IsNull() || !ownedModifiers.Remove(owner))
            return false;

        properties.ClearSource(owner);
        NotifyStatsChanged();

        return true;
    }

    /// <summary>
    /// Обновляет модификаторы после изменения экипировки связанной сущности.
    /// </summary>
    public void OnEntityEquipmentChanged(EntityEquipmentChangedEventArgs args)
    {
        if (args?.Entity == null || entityLink?.Entity == null || args.Entity.Id != entityLink.Entity.Id)
            return;

        CollectProviders();
    }

    private void CollectProviders(bool notify)
    {
        providers.Clear();
        singleProviders.Clear();
        properties.Clear();

        GetComponentsInChildren(false, providers);
        GetComponentsInChildren(false, singleProviders);

        foreach (IStatModifiersProvider provider in providers)
        {
            foreach (StatModifier modifier in GetModifiersSafely(provider))
                ApplyModifier(provider, modifier);
        }

        foreach (IStatModifierProvider provider in singleProviders)
            ApplyModifier(provider, provider.StatModifier);

        // Модификаторы владельцев без объекта на сцене переживают пересборку.
        foreach (KeyValuePair<object, List<StatModifier>> owned in ownedModifiers)
        {
            foreach (StatModifier modifier in owned.Value)
                ApplyModifier(owned.Key, modifier);
        }

        if (notify)
            NotifyStatsChanged();
    }

    /// <summary>
    /// Сообщает сущности, что итоговые характеристики изменились.
    /// </summary>
    private void NotifyStatsChanged()
    {
        if (entityLink?.Entity != null)
            EntityEvents.RefreshStats(entityLink.Entity);
    }

    private void ApplyModifier(object source, StatModifier modifier)
    {
        if (source.IsNull() || modifier == null)
            return;

        Enumeration stat = modifier.GetEnumeration();
        if (stat == null)
            return;

        properties.Add(stat, source, modifier.Value, ConvertType(modifier.Type), modifier.Priority);
    }

    private static IEnumerable<StatModifier> GetModifiersSafely(IStatModifiersProvider provider)
    {
        return provider?.StatModifiers ?? Enumerable.Empty<StatModifier>();
    }

    private static Enumeration ConvertType(StatModifierType type)
    {
        return type switch
        {
            StatModifierType.Add => ModifierTypes.Add,
            StatModifierType.Multiply => ModifierTypes.Multiply,
            StatModifierType.Override => ModifierTypes.Override,
            _ => ModifierTypes.Add
        };
    }
}
