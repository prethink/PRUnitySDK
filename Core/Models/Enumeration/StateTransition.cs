using UnityEngine;

public class StateTransition : MonoBehaviour
{
    [EnumerationOptions(typeof(StateId))]
    public EnumerationReference NextState;

    public Enumeration Next => NextState.ToEnumeration();
}
