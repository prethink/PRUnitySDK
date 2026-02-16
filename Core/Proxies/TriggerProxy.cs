using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Proxy-класс для триггеров (OnTriggerEnter/Stay/Exit).
/// Позволяет подписываться на события через UnityEvent и делегировать их реальному объекту.
/// </summary>
public class TriggerProxy : PRMonoBehaviourProxy
{
    // События UnityEvent для подписки в инспекторе
    public UnityEvent<Collider> OnTriggerEnterEvent;  // Вызывается при входе объекта в триггер
    public UnityEvent<Collider> OnTriggerEnterExit;   // Вызывается при выходе объекта из триггера
    public UnityEvent<Collider> OnTriggerEnterStay;   // Вызывается каждый кадр, пока объект в триггере

    /// <summary>
    /// Вызывается при входе объекта в триггер
    /// </summary>
    protected override void PROnTriggerEnter(Collider other)
    {
        base.PROnTriggerEnter(other);

        OnTriggerEnterEvent?.Invoke(other);

        refObject?.InvokeOnTriggerEnter(this, other);
    }

    /// <summary>
    /// Вызывается каждый кадр, пока объект находится в триггере
    /// </summary>
    protected override void PROnTriggerStay(Collider other)
    {
        base.PROnTriggerStay(other);

        OnTriggerEnterStay?.Invoke(other);

        refObject?.InvokeOnTriggerStay(this, other);
    }

    /// <summary>
    /// Вызывается при выходе объекта из триггера
    /// </summary>
    protected override void PROnTriggerExit(Collider other)
    {
        base.PROnTriggerExit(other);

        OnTriggerEnterExit?.Invoke(other);

        refObject?.InvokeOnTriggerExit(this, other);
    }
}