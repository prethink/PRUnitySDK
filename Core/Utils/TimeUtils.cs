using System;
using System.Collections;
using UnityEngine;

public static class TimeUtils
{
    private static IEnumerator DelayAction(string type, float timeout, Action<PRToken> action, PRToken token = null)
    {
        var waitTime = PRTime.Instance.Time + timeout;
        yield return new WaitUntil(() => PRTime.Instance.Time > waitTime);

        if (token?.IsCancelled == true)
        {
            PRLog.WriteWarning(typeof(TimeUtils), $"{type} was cancelled.");
            yield break;
        }
        action?.Invoke(token);
    }

    public static Coroutine DelayAction(this MonoBehaviour monoBehaviour, float timeout, Action<PRToken> action, PRToken token = null)
    {
        return monoBehaviour.StartCoroutine(DelayAction(monoBehaviour.GetType().ToString(), timeout, action, token));
    }

    public static Coroutine DelayAction(float timeout, string type, Action<PRToken> action, PRToken token = null)
    {
        var host = PRMonoBehaviourHost.Instance;
        return host.StartCoroutine(DelayAction(type, timeout, action, token));
    }
}