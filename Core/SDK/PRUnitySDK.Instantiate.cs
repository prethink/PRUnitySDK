using UnityEngine;

public partial class PRUnitySDK
{
    public static T Instantiate<T>(T original, InstantiateParameters parameters) 
        where T : Object
    {
        //TODO: override
        return Object.Instantiate<T>(original, parameters);
    }
}
