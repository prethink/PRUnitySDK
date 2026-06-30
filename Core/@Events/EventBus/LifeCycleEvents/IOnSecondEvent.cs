public interface IOnSecondEvent : IGlobalSubscriber
{
    void OnSecondTick(int currentSecond);
}
