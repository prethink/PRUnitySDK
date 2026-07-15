using System.Collections;
using UnityEngine;

public abstract class PRCoroutineBase 
{
    protected abstract IEnumerator InternalExecute();
    protected Coroutine CurrentCoroutine;
    protected MonoBehaviour instance;

    public virtual Coroutine Execute()
    {
        if (instance != null)
            CurrentCoroutine = instance.StartCoroutine(InternalExecute());
        else
            CurrentCoroutine = PRMonoBehaviourHost.Instance.StartCoroutine(InternalExecute());

        return CurrentCoroutine;
    }

    public virtual Coroutine StopAndExecute()
    {
        Stop();
        return Execute();
    }

    public bool Stop()
    {
        if (CurrentCoroutine == null)
            return false;

        if(instance != null)
            instance.StopCoroutine(CurrentCoroutine);
        else
            PRMonoBehaviourHost.Instance.StopCoroutine(CurrentCoroutine);

        return true;
    }
}
