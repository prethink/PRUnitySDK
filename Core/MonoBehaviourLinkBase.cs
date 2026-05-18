using UnityEngine;

public class MonoBehaviourLinkBase<T> : MonoBehaviour where T : new()
{
    public T Link { get; private set; } = new();
}
