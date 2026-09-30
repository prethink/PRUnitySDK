#if PRSDK_TESTS
/// <summary>
/// Сущность для проверки поиска по Definition и базовым метаданным.
/// </summary>
public class EntityDefinitionSearchTestEntity : EntityDefinition<EntityMetadata>
{
    public void ConfigureForTest(EntityMetadata metadata, EntityMetadata definition)
    {
        Metadata = metadata;
        typeof(EntityDefinition<EntityMetadata>).GetProperty(nameof(Definition)).SetValue(this, definition);
        InitializeEntityMetadata();
    }
}
#endif
