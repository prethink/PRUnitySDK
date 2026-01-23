public class HealthChangedEventArgsBase
{
    public float CurrentHealth { get; }
    public float MaxHealth { get; }

    public HealthChangedEventArgsBase(float currentHealth, float maxHealth)
    {
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
    }
}
