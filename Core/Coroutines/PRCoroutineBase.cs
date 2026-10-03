using System.Collections;
using UnityEngine;

/// <summary>
///Базовая обёртка над Unity-корутиной с единым API запуска, перезапуска и остановки.
///Если владелец <see cref="MonoBehaviour"/> не передан, корутина выполняется на
///глобальном <see cref="PRMonoBehaviourHost"/> и не зависит от жизненного цикла
///объекта, создавшего эту обёртку.
///</summary>
public abstract class PRCoroutineBase 
{
    /// <summary>
    ///Создаёт перечислитель с фактической логикой корутины.
    ///</summary>
    protected abstract IEnumerator InternalExecute();

    /// <summary>
    ///Последний запущенный экземпляр Unity-корутины.
    ///</summary>
    protected Coroutine CurrentCoroutine;

    /// <summary>
    ///Необязательный владелец корутины. При его отсутствии используется
    ///<see cref="PRMonoBehaviourHost"/>.
    ///</summary>
    protected MonoBehaviour instance;

    /// <summary>
    ///Запускает новый экземпляр корутины. Уже запущенный экземпляр автоматически
    ///не останавливается; для безопасного перезапуска используйте <see cref="StopAndExecute"/>.
    ///</summary>
    /// <returns>Запущенная Unity-корутина.</returns>
    public virtual Coroutine Execute()
    {
        if (instance != null)
            CurrentCoroutine = instance.StartCoroutine(InternalExecute());
        else
            CurrentCoroutine = PRMonoBehaviourHost.Instance.StartCoroutine(InternalExecute());

        return CurrentCoroutine;
    }

    /// <summary>
    ///Останавливает предыдущий запуск, если он существует, и запускает корутину заново.
    ///</summary>
    /// <returns>Новая Unity-корутина.</returns>
    public virtual Coroutine StopAndExecute()
    {
        Stop();
        return Execute();
    }

    /// <summary>
    ///Останавливает последний запущенный экземпляр корутины.
    ///</summary>
    /// <returns><see langword="true"/>, если корутина была зарегистрирована и отправлена на остановку.</returns>
    public bool Stop()
    {
        if (CurrentCoroutine == null)
            return false;

        if(instance != null)
            instance.StopCoroutine(CurrentCoroutine);
        // Хост здесь не создаём: останавливают и из OnDestroy, при закрытии сцены, когда
        // хоста уже нет. Обращение к Instance создало бы его заново, и Unity ругалась бы
        // на объект, появившийся во время выгрузки. Нет хоста — корутина умерла вместе с ним.
        else if (PRMonoBehaviourHost.TryGetExisting(out PRMonoBehaviourHost host))
            host.StopCoroutine(CurrentCoroutine);

        return true;
    }

    protected PRCoroutineBase()
    {
        
    }

    protected PRCoroutineBase(MonoBehaviour instance)
    {
        this.instance = instance;
    }
}
