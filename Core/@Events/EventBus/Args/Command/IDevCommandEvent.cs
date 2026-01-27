public interface IDevCommandEvent : IGlobalSubscriber
{
    void Track(DevCommandEventArgsBase args);
}

