public interface IOnRealSecondsEvent : IGlobalSubscriber
{
    void OnRealSecondTick(long currentSecond);
}

public interface IOnGameSecondsEvent : IGlobalSubscriber
{
    void OnGameSecondTick(long currentSecond);
}