#if PRSDK_TESTS
using UnityEngine;

/// <summary>
/// Игрок для проверки здоровья без регистрации в общих событиях.
/// </summary>
public class EntityTrackerPlayerSearchTestEntity : PlayerBase
{
    public override PlayerType PlayerType => PlayerType.AI;
    public override Enumeration EntityType => EntityTypeEnumerations.Player;

    public void InitializeForTest(EntityMetadata metadata)
    {
        Metadata = metadata;
        InitializationComponents();
        typeof(HealthComponent).GetProperty(nameof(HealthComponent.Entity)).SetValue(Health, this);
        typeof(HealthComponent).GetProperty(nameof(HealthComponent.GameObject)).SetValue(Health, EntityGameObject);
        Health.InitHealth();
    }

    protected override void RegisterEventsOnCreated()
    {
    }
}
#endif
