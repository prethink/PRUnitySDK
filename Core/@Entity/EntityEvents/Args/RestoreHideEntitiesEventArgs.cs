public class RestoreHideEntitiesEventArgs : EntitiesEventArgsBase
{

}

public interface IRestoreHideEntitiesEvent : IGlobalSubscriber
{
    void RestoreHideEvent(RestoreHideEntitiesEventArgs e);
}