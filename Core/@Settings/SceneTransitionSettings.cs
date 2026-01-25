using System;
using UnityEngine;

[Serializable]
public class SceneTransitionSettings 
{
    [field: SerializeField] public bool UseFadeOnChange { get; protected set; } = true;
    [field: SerializeField] public float LoaderTimeout { get; protected set; }
}
