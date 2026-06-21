public class EntityIdGenerator : SingletonProviderBase<EntityIdGenerator>
{
    public long NextId { get; private set; } = 0;

    public long RegisterId()
    {
        return NextId++;
    }
}
