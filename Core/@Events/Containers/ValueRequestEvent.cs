using System;
using UnityEngine.Events;

[Serializable]
public class ValueRequestEvent<T> : UnityEvent<T> { }