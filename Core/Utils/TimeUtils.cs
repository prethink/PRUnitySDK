using System;
using System.Collections;
using UnityEngine;

public static class TimeUtils
{
    private static IEnumerator DelayAction(Type type, float timeout, Action<PRToken> action, PRTimeType timeType = PRTimeType.RealTime, PRToken token = null)
    {
        Func<float> currentTime = timeType == PRTimeType.RealTime
            ? () => PRTime.Instance.RealTime
            : () => PRTime.Instance.GameTime;

        var waitTime = currentTime() + timeout;
        yield return new WaitUntil(() => currentTime() > waitTime);

        if (token?.IsCancelled == true)
        {
            PRLog.WriteWarning(typeof(TimeUtils), $"{type} was cancelled.");
            yield break;
        }
        action?.Invoke(token);
    }

    public static Coroutine DelayAction(this MonoBehaviour monoBehaviour, float timeout, Action<PRToken> action, PRTimeType timeType = PRTimeType.RealTime, PRToken token = null)
    {
        return monoBehaviour.StartCoroutine(DelayAction(monoBehaviour.GetType(), timeout, action, timeType, token));
    }

    public static Coroutine DelayAction(this object obj, float timeout, Action<PRToken> action, PRTimeType timeType = PRTimeType.RealTime, PRToken token = null)
    {
        var host = PRMonoBehaviourHost.Instance;
        return host.StartCoroutine(DelayAction(obj.GetType(), timeout, action, timeType, token));
    }

    private static IEnumerator ExecuteActionWithCallback(Type type, Action mainAction, float callbackTimeoutSeconds, Action callback, PRTimeType timeType = PRTimeType.RealTime)
    {
        mainAction.Invoke();
        Func<float> currentTime = timeType == PRTimeType.RealTime
            ? () => PRTime.Instance.RealTime
            : () => PRTime.Instance.GameTime;

        var waitTime = currentTime() + callbackTimeoutSeconds;
        yield return new WaitUntil(() => currentTime() > waitTime);
        callback.Invoke();
    }

    public static Coroutine ExecuteActionWithCallback(this MonoBehaviour monoBehaviour, Action mainAction, float callbackTimeoutSeconds, Action callback, PRTimeType timeType = PRTimeType.RealTime)
    {
        return monoBehaviour.StartCoroutine(ExecuteActionWithCallback(monoBehaviour.GetType(), mainAction, callbackTimeoutSeconds, callback, timeType));
    }

    public static Coroutine ExecuteActionWithCallback(this object obj, Action mainAction, float callbackTimeoutSeconds, Action callback, PRTimeType timeType = PRTimeType.RealTime)
    {
        var host = PRMonoBehaviourHost.Instance;
        return host.StartCoroutine(ExecuteActionWithCallback(obj.GetType(), mainAction, callbackTimeoutSeconds, callback, timeType));
    }
}