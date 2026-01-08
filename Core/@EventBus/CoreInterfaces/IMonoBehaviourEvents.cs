public interface IMonoBehaviourEvents : IGlobalSubscriber
{
    void Awake();

    void Start();

    void OnEnabled();

    void OnDisabled();

    void OnDestroy();
}
