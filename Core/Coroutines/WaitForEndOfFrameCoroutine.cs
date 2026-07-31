using System;
using System.Collections.Generic;
using UnityEngine;

public class WaitForEndOfFrameCoroutine : UnityYieldCoroutineBase<WaitForEndOfFrame>
{
    public WaitForEndOfFrameCoroutine(Action callback) : base(callback)
    {
    }

    public WaitForEndOfFrameCoroutine(Action callback, MonoBehaviour instance) : base(callback, instance)
    {
    }

    public WaitForEndOfFrameCoroutine(IEnumerable<Action> callbacks) : base(callbacks)
    {
    }

    public WaitForEndOfFrameCoroutine(IEnumerable<Action> callbacks, MonoBehaviour instance) : base(callbacks, instance)
    {
    }
}
