using System.Linq;

/// <summary>
/// Базовый трекер сущностей с очисткой по времени жизни раунда и сессии.
/// </summary>
public abstract class EntityTrackerBase<T> : TrackerBase<T>
    where T : IEntity
{
    public abstract void Clear();
}
