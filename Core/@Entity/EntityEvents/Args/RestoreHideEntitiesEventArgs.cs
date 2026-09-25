public class RestoreHideEntitiesEventArgs : EntitiesEventArgsBase
{

}

public interface IRestoreHideEntitiesEvent : IGlobalSubscriber
{
    void RestoreHideEvent(RestoreHideEntitiesEventArgs e);
}

/// <summary>
/// Спрятанные сущности восстановлены — все до одной.
/// </summary>
/// <remarks>
/// Приходит сразу после <see cref="IRestoreHideEntitiesEvent"/>. Порядок подписчиков шина
/// не гарантирует, поэтому тот, кто распоряжается уже восстановленными сущностями —
/// заменяет одну из них, прячет снова, — внутри самого восстановления может опередить
/// сущность, и та вернётся поверх его решения.
/// </remarks>
public interface IHideEntitiesRestoredEvent : IGlobalSubscriber
{
    void OnHideEntitiesRestored(RestoreHideEntitiesEventArgs e);
}
