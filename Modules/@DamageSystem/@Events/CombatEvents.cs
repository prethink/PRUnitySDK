public static class CombatEvents 
{
    public static void RaiseOnKill(EntityKillEventArgs args) => EventBus.RaiseEvent<IEntityKillEvent>(x => x.OnKill(args));

    public static void RaiseOnTakeDamage(TakeDamageEvent args) => EventBus.RaiseEvent<IOnTakeDamageEvents>(x => x.OnTakeDamage(args));
}
