using System.Linq;

public abstract class EntityTrackerBase<T> : TrackerBase<T>
    where T : IEntity
{
    protected int generatedNextId = 0;

    protected virtual int GenerateId()
    {
        return generatedNextId++;
    }

    public abstract void Clear();

    public virtual void ClearRound()
    {
        foreach (var entity in elements.Where(x => x.LifeTime == EntityLifeTime.Round).ToList())
        {
            entity.DestroyEntity(new EntityDestroyOptions() { FullDestroy = true });
            elements.Remove(entity);
        }
    }

    public virtual void ClearSession()
    {
        foreach (var entity in elements.Where(x => x.LifeTime == EntityLifeTime.Session).ToList())
        {
            entity.DestroyEntity(new EntityDestroyOptions() { FullDestroy = true });
            elements.Remove(entity);
        }

        ClearRound();
    }
}