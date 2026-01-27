using UnityEngine;

public class GlobalMonoBehaviourEvents : MonoBehaviour
{
    private void Awake()
    {
        EventBus.RaiseEvent<IMonoBehaviourEvents>(x => x.Awake());
    }

    private void Start()
    {
        EventBus.RaiseEvent<IMonoBehaviourEvents>(x => x.Start());
    }

    private void OnEnable()
    {
        EventBus.RaiseEvent<IMonoBehaviourEvents>(x => x.OnEnabled());
    }

    private void OnDisable()
    {
        EventBus.RaiseEvent<IMonoBehaviourEvents>(x => x.OnDisabled());
    }

    private void OnDestroy()
    {
        EventBus.RaiseEvent<IMonoBehaviourEvents>(x => x.OnDestroy());
    }
}
