using System;
using System.Collections.Generic;
using UnityEngine;

public class LateFixedUpdateCoroutine : UnityYieldCoroutineBase<WaitForFixedUpdate>
{
    public LateFixedUpdateCoroutine(Action callback) : base(callback)
    {
    }

    public LateFixedUpdateCoroutine(Action callback, MonoBehaviour instance) : base(callback, instance)
    {
    }

    public LateFixedUpdateCoroutine(IEnumerable<Action> callbacks) : base(callbacks)
    {
    }

    public LateFixedUpdateCoroutine(IEnumerable<Action> callbacks, MonoBehaviour instance) : base(callbacks, instance)
    {
    }
}
