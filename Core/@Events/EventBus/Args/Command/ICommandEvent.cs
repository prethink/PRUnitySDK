public interface ICommandEvent : IGlobalSubscriber
{
    void Track(CommandEventArgsBase args);
}
