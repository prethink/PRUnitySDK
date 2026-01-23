using UnityEngine;

[SerializeField]
public class DefaultSettings 
{
    [field: SerializeField]  public DefaultControlSettings Control { get; private set; }

    [field: SerializeField] public DefailtGameSettings GameSettings { get; private set; }
}
