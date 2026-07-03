public interface IOnSecondEvent : IGlobalSubscriber
{
    void OnSecondTick(long currentSecond);
}
