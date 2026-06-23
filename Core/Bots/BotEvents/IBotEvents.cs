public interface IBotEvents : IGlobalSubscriber
{
    void Track(BotEventArgs args);
}
