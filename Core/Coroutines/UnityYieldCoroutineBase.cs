using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///Основа бесконечной корутины, вызывающей набор callback'ов после указанной Unity
///yield-инструкции <typeparamref name="T"/>. Выполнение продолжается до явного
///вызова <see cref="PRCoroutineBase.Stop"/> или уничтожения владельца корутины.
///Повторяющиеся callback'и автоматически устраняются через <see cref="HashSet{T}"/>.
///</summary>
/// <typeparam name="T">Unity yield-инструкция с публичным конструктором без параметров.</typeparam>
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
