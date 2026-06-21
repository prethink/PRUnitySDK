public class EntityRefreshEventArgs : EntityEventArgsBase
{
    public EntityRefreshEventArgs(IEntity entity) : base(entity)
    {
    }
}

public class EntityRefreshStatsEventArgs : EntityRefreshEventArgs
{
    public EntityRefreshStatsEventArgs(IEntity entity) : base(entity)
    {
    }
}

public class EntityRefreshFlagsEventArgs : EntityRefreshEventArgs
{
    public EntityRefreshFlagsEventArgs(IEntity entity) : base(entity)
    {
    }
}

public interface IEntityRefreshEvent : IGlobalSubscriber
{
    void RefreshEntity(EntityRefreshEventArgs args);
}