using System.Linq;

/// <summary>
/// Базовый трекер сущностей с очисткой по времени жизни раунда и сессии.
/// </summary>
public abstract class EntityTrackerBase<T> : TrackerBase<T>
    where T : IEntity
{
    public abstract void Clear();

    /// <summary>
    /// Уничтожает и снимает с регистрации сущности текущего раунда.
    /// </summary>
    public virtual void ClearRound()
    {
        foreach (var entity in elements.Where(x => !x.IsNull() && x.LifeTime == EntityLifeTime.Round).ToList())
        {
            entity.DestroyEntity(new EntityDestroyOptions() { FullDestroy = true });
            Unregister(entity);
        }
    }

    /// <summary>
    /// Очищает сущности сессии, а затем сущности раунда.
    /// </summary>
    public virtual void ClearSession()
    {
        foreach (var entity in elements.Where(x => !x.IsNull() && x.LifeTime == EntityLifeTime.Session).ToList())
        {
            entity.DestroyEntity(new EntityDestroyOptions() { FullDestroy = true });
            Unregister(entity);
        }

        ClearRound();
    }
}
