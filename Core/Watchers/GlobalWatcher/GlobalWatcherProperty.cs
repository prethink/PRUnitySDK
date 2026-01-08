using System;
using System.Collections;

public abstract class GlobalWatcherProperty
{
    #region Поля и свойства

    public abstract string Key { get; }

    public abstract int TickSeconds { get; }

    public abstract Func<IEnumerator> IEnumerator { get; }

    #endregion

    #region События

    public event Action OnTick;

    #endregion

    #region Методы

    /// <summary>
    /// Вызов события тика.
    /// </summary>
    protected void OnTickInvoke()
    {
        OnTick?.Invoke();
    }

    #endregion
}

public abstract class GlobalWatcherProperty<T> : GlobalWatcherProperty
{
    #region События

    public event Action<T> CallBackWithParameter;

    #endregion
}
