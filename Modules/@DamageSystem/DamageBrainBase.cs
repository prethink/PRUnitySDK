public abstract class DamageBrainBase : PRMonoBehaviour
{
    public IEntity Attacker { get; protected set; }
    public IDamageProvider DamageProvider { get; protected set; }

    public abstract bool CanAttack();

    public abstract bool CanAttackSource();

    public abstract void PerformAttack();
}
