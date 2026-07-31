using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UnityYieldCoroutineBase<T> : PRCoroutineBase where T : YieldInstruction, new()
{
    protected HashSet<Action> callbacks = new();

    protected override IEnumerator InternalExecute()
    {
        T instruction = new T();
        while (true)
        {
            yield return WaitPause.Instance;
            yield return instruction;
            foreach (var callback in callbacks)
            {
                callback?.Invoke();
            }
        }
    }

    public UnityYieldCoroutineBase(Action callback)
    {
        callbacks.Add(callback);
    }

    public UnityYieldCoroutineBase(Action callback, MonoBehaviour instance) : base(instance)
    {
        callbacks.Add(callback);
    }

    public UnityYieldCoroutineBase(IEnumerable<Action> callbacks)
    {
        foreach (var callback in callbacks)
            this.callbacks.Add(callback);
    }

    public UnityYieldCoroutineBase(IEnumerable<Action> callbacks, MonoBehaviour instance) : base(instance)
    {
        foreach (var callback in callbacks)
            this.callbacks.Add(callback);
    }
}
