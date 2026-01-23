using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Описывает данные об уроне, которые могут быть переданы объекту при получении попадания.
/// Используется для объединения нескольких источников урона и отслеживания уникальных модификаторов.
/// </summary>
public class DamageData
{
    /// <summary>
    /// Уникальный идентификатор серии урона (DamageId).  
    /// Используется для того, чтобы различать урон от разных выстрелов,  
    /// но объединять все попадания, произошедшие в рамках одного выстрела (например, дробь).
    /// </summary>
    public Guid DamageId { get; set; }

    /// <summary>
    /// Величина урона (с учётом всех модификаторов).
    /// </summary>
    public float Damage { get; set; }

    /// <summary>
    /// Сила импульса (отброса), которая должна применяться к объекту при получении урона.
    /// </summary>
    public float KnockBackPower { get; set; }

    /// <summary>
    /// Тип урона (например, физический, огненный, ядовитый и т.д.).
    /// Может содержать несколько флагов при использовании <see cref="FlagsAttribute"/>.
    /// </summary>
    public DamageType DamageType { get; set; }

    /// <summary>
    /// Источник урона.
    /// </summary>
    public IEntity DamageSource { get; set; }

    /// <summary>
    /// Список уникальных идентификаторов модификаторов, применённых к этому урону.
    /// Используется для предотвращения повторного применения одного и того же эффекта.
    /// </summary>
    public HashSet<IDamageModifier> AppliedModifiers { get;  set; } = new();

    /// <summary>
    /// Показывает степень болевого шока, вызванного этим уроном (0–100).  
    /// Значение 0 — урона недостаточно, чтобы вызвать шок.  
    /// Значение 100 — максимальная боль/оглушение.
    /// </summary>
    /// <remarks>
    /// Может использоваться для расчёта дезориентации, оглушения, тряски камеры или реакции AI.
    /// </remarks>
    public float PainShock { get; set; }

    /// <summary>
    /// Проверяет, применён ли указанный модификатор к данному урону.
    /// </summary>
    /// <param name="modifier">Модификатор урона для проверки.</param>
    /// <returns><c>true</c>, если модификатор уже был применён; иначе — <c>false</c>.</returns>
    public bool IsAppliedModifier(IDamageModifier modifier)
    {
        return AppliedModifiers.Any(x => x.ModifierIdentifier == modifier.ModifierIdentifier);
    }

    /// <summary>
    /// Объединяет текущие данные урона с другими, формируя новый <see cref="DamageData"/> объект.  
    /// Используется, например, при попадании несколькими пулями из одной серии выстрелов (дробовик).
    /// </summary>
    /// <param name="damageData">Второй источник урона для объединения.</param>
    /// <param name="damageId">Общий идентификатор урона (серии выстрелов).</param>
    /// <returns>Новый объединённый экземпляр <see cref="DamageData"/>.</returns>
    public DamageData CombineDamageData(DamageData damageData, Guid damageId)
    {
        return new DamageData
        {
            DamageId = damageId,
            Damage = this.Damage + damageData.Damage,
            KnockBackPower = this.KnockBackPower + damageData.KnockBackPower,
            DamageType = this.DamageType | damageData.DamageType,
            PainShock = Math.Clamp(this.PainShock + damageData.PainShock, 0, 100),
            AppliedModifiers = this.AppliedModifiers.Union(damageData.AppliedModifiers).ToHashSet()
        };
    }

    /// <summary>
    /// Создаёт новый экземпляр <see cref="DamageData"/> с указанным значением урона.
    /// </summary>
    /// <param name="damage">Величина урона.</param>
    /// <returns>Новый экземпляр <see cref="DamageData"/>.</returns>
    public static DamageData Create(float damage, float painShock = 0)
    {
        return new DamageData
        {
            DamageId = Guid.NewGuid(),
            Damage = damage,
            PainShock = Math.Clamp(painShock, 0, 100)
        };
    }

    /// <summary>
    /// Объединяет два экземпляра урона в один, используя общий идентификатор урона.
    /// </summary>
    /// <param name="damageOneData">Первый источник урона.</param>
    /// <param name="damageTwoData">Второй источник урона.</param>
    /// <param name="damageId">Общий идентификатор серии урона.</param>
    /// <returns>Новый экземпляр <see cref="DamageData"/> с объединёнными значениями.</returns>
    public static DamageData CombineDamageData(DamageData damageOneData, DamageData damageTwoData, Guid damageId)
    {
        return damageOneData.CombineDamageData(damageTwoData, damageId);
    }

    /// <summary>
    /// Создаёт копию текущего экземпляра (глубокое копирование коллекций).
    /// </summary>
    /// <returns>Новый экземпляр <see cref="DamageData"/> с идентичными параметрами.</returns>
    public DamageData Clone()
    {
        return new DamageData
        {
            DamageId = this.DamageId,
            Damage = this.Damage,
            KnockBackPower = this.KnockBackPower,
            DamageType = this.DamageType,
            PainShock = this.PainShock,
            AppliedModifiers = new HashSet<IDamageModifier>(this.AppliedModifiers)
        };
    }
}
