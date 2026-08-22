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
    public UnityEvent<Collider> OnTriggerExit;   // Вызывается при выходе объекта из триггера
    public UnityEvent<Collider> OnTriggerStay;   // Вызывается каждый кадр, пока объект в триггере

    /// <summary>
    /// Вызывается при входе объекта в триггер
    /// </summary>
    protected override void PROnTriggerEnter(Collider other)
    {
        base.PROnTriggerEnter(other);
        Invoke(OnTriggerEnterEvent, other, r => r.InvokeOnTriggerEnter(this, other));
    }

    /// <summary>
    /// Вызывается каждый кадр, пока объект находится в триггере
    /// </summary>
    protected override void PROnTriggerStay(Collider other)
    {
        base.PROnTriggerStay(other);
        Invoke(OnTriggerStay, other, r => r.InvokeOnTriggerStay(this, other));
    }

    /// <summary>
    /// Вызывается при выходе объекта из триггера
    /// </summary>
    protected override void PROnTriggerExit(Collider other)
    {
        base.PROnTriggerExit(other);
        Invoke(OnTriggerExit, other, r => r.InvokeOnTriggerExit(this, other));
    }
}