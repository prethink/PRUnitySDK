using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///Бесконечно вызывает зарегистрированные callback'и в конце каждого кадра,
///после завершения обычных Update/LateUpdate и отрисовки кадра. Работает до
///явной остановки или уничтожения переданного владельца.
///</summary>
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
