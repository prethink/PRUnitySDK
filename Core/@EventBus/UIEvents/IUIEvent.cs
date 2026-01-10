public interface IUIEvent : IGlobalSubscriber
{
    void Handle(UIEventArgsBase args);
}
