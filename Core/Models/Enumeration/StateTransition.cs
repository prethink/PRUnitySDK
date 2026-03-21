using UnityEngine;

public class StateTransition : MonoBehaviour
{
    //[EnumerationOptions(typeof(StateId))]
    public EnumerationReference<StateId> NextState;

    //public Enumeration Next => NextState.ToEnumeration();
}
