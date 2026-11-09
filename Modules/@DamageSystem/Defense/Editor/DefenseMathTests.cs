#if PRSDK_TESTS
using System.Collections.Generic;
using NUnit.Framework;

public class DefenseMathTests
{
    private const float Tolerance = 0.0001f;

    private static readonly DefenseSettings Settings = new(
        armorConstant: 100f,
        maxArmorReduction: 0.85f,
        maxResistance: 75f,
        minResistance: -100f,
        armorDamageTypes: DamageType.Bullet | DamageType.Explosion,
        armorAppliesToGeneric: true);

    [TestCase(0f, 0f)]
    [TestCase(100f, 0.5f)]
    [TestCase(300f, 0.75f)]
    [TestCase(100000f, 0.85f)]
    public void ArmorReduction_DiminishesAndCaps(float armor, float expected)
    {
        Assert.AreEqual(expected, DefenseMath.ArmorReduction(armor, 0f, 100f, 0.85f), Tolerance);
    }

    [Test]
    public void ArmorPenetration_IgnoresShareOfArmor()
    {
        // 200 брони, пробивание 50% — как 100 брони: половина урона.
        Assert.AreEqual(0.5f, DefenseMath.ArmorReduction(200f, 50f, 100f, 0.85f), Tolerance);
        Assert.AreEqual(0f, DefenseMath.ArmorReduction(200f, 150f, 100f, 0.85f), Tolerance);
    }

    [TestCase(50f, 0f, 50f)]
    [TestCase(120f, 0f, 75f)]
    [TestCase(-500f, 0f, -100f)]
    [TestCase(120f, 25f, 50f)]
    [TestCase(-90f, 30f, -100f)]
    public void EffectiveResistance_CapsThenPenetrates(float resistance, float penetration, float expected)
    {
        Assert.AreEqual(expected, DefenseMath.EffectiveResistance(resistance, penetration, -100f, 75f), Tolerance);
    }

    [TestCase(75f, 0.25f)]
    [TestCase(0f, 1f)]
    [TestCase(-50f, 1.5f)]
    [TestCase(-100f, 2f)]
    public void ResistanceMultiplier_NegativeIsVulnerability(float resistance, float expected)
    {
        Assert.AreEqual(expected, DefenseMath.ResistanceMultiplier(resistance), Tolerance);
    }

    [Test]
    public void MixedHit_UsesWeakestResistance()
    {
        var resistances = new Dictionary<DamageType, float> { [DamageType.Fire] = 75f, [DamageType.Explosion] = 20f };

        DefenseResult result = Calculate(DamageType.Fire | DamageType.Explosion, resistances);

        Assert.AreEqual(DamageType.Explosion, result.WeakestKind);
        Assert.AreEqual(0.8f, result.ResistanceMultiplier, Tolerance);
    }

    [Test]
    public void Immunity_IgnoresPenetration()
    {
        DefenseResult result = DefenseMath.Calculate(
            DamageType.Fire, _ => 0f, DamageType.Fire, 0f, 0f, 1000f, Settings);

        Assert.IsTrue(result.IsImmune);
        Assert.AreEqual(0f, result.Multiplier, Tolerance);
    }

    [Test]
    public void Immunity_MixedHitStillHitsWeakestKind()
    {
        DefenseResult result = DefenseMath.Calculate(
            DamageType.Fire | DamageType.Explosion, _ => 0f, DamageType.Fire, 0f, 0f, 0f, Settings);

        Assert.IsFalse(result.IsImmune);
        Assert.AreEqual(DamageType.Explosion, result.WeakestKind);
        Assert.AreEqual(1f, result.ResistanceMultiplier, Tolerance);
    }

    [TestCase(DamageType.Fire, DamageType.Fire, true)]
    [TestCase(DamageType.Fire | DamageType.Critical, DamageType.Fire, true)]
    [TestCase(DamageType.Fire | DamageType.Explosion, DamageType.Fire, false)]
    [TestCase(DamageType.Fire | DamageType.Explosion, DamageType.Fire | DamageType.Explosion, true)]
    [TestCase(DamageType.Generic, DamageType.Fire, false)]
    [TestCase(DamageType.Critical, DamageType.Fire | DamageType.Ice, false)]
    public void IsImmune_RequiresEveryKind(DamageType damageType, DamageType immunities, bool expected)
    {
        Assert.AreEqual(expected, DefenseMath.IsImmune(damageType, immunities));
        Assert.AreEqual(expected, DefenseMath.Calculate(damageType, _ => 0f, immunities, 0f, 0f, 0f, Settings).IsImmune);
    }

    [Test]
    public void Armor_CoversGenericAndPhysicalOnly()
    {
        Assert.AreEqual(0.5f, Calculate(DamageType.Generic, null, armor: 100f).ArmorReduction, Tolerance);
        Assert.AreEqual(0.5f, Calculate(DamageType.Bullet | DamageType.Critical, null, armor: 100f).ArmorReduction, Tolerance);
        Assert.AreEqual(0f, Calculate(DamageType.Fire, null, armor: 100f).ArmorReduction, Tolerance);
        Assert.AreEqual(0f, Calculate(DamageType.Fire | DamageType.Explosion, null, armor: 100f).ArmorReduction, Tolerance);
        Assert.AreEqual(0f, Calculate(DamageType.Fall, null, armor: 100f).ArmorReduction, Tolerance);
    }

    [Test]
    public void ArmorAndResistance_Multiply()
    {
        var resistances = new Dictionary<DamageType, float> { [DamageType.Bullet] = 50f };

        DefenseResult result = Calculate(DamageType.Bullet, resistances, armor: 100f);

        Assert.AreEqual(0.25f, result.Multiplier, Tolerance);
    }

    [Test]
    public void MarksOnly_HaveNoResistance()
    {
        DefenseResult result = Calculate(DamageType.Critical | DamageType.TimeBased, null);

        Assert.AreEqual(DamageType.Generic, result.WeakestKind);
        Assert.AreEqual(1f, result.ResistanceMultiplier, Tolerance);
    }

    [Test]
    public void EveryKind_HasResistanceStat()
    {
        for (int bit = 1; bit <= (int)DefenseMath.Kinds; bit <<= 1)
            Assert.IsNotNull(DefenseStatEnumerations.ResistanceFor((DamageType)bit), ((DamageType)bit).ToString());

        Assert.IsNull(DefenseStatEnumerations.ResistanceFor(DamageType.Generic));
        Assert.IsNull(DefenseStatEnumerations.ResistanceFor(DamageType.Critical));
    }

    private static DefenseResult Calculate(DamageType type, Dictionary<DamageType, float> resistances, float armor = 0f)
    {
        return DefenseMath.Calculate(
            type,
            kind => resistances != null && resistances.TryGetValue(kind, out float value) ? value : 0f,
            DamageType.Generic,
            armor,
            0f,
            0f,
            Settings);
    }
}
#endif
