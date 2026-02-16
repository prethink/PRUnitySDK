using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Proxy-класс для обработки столкновений и делегирования их другим объектам.
/// Использует события UnityEvent для подписки в инспекторе.
/// </summary>
public class CollisionProxy : PRMonoBehaviourProxy
{
    // События, которые можно подписывать в инспекторе
    public UnityEvent<Collision> OnCollisionEnterEvent;   // Вызывается при начале столкновения
    public UnityEvent<Collision> OnCollisionEnterExitEvent; // Вызывается при завершении столкновения
    public UnityEvent<Collision> OnCollisionStayEvent;    // Вызывается каждый кадр, пока объект находится в столкновении

    /// <summary>
    /// Вызывается при начале столкновения
    /// </summary>
    protected override void PROnCollisionEnter(Collision collision)
    {
        base.PROnCollisionEnter(collision);

        // Вызываем подписанные события
        OnCollisionEnterEvent?.Invoke(collision);

        // Делегируем вызов реальному объекту, если он указан
        refObject?.InvokePROnCollisionEnter(this, collision);
    }

    /// <summary>
    /// Вызывается каждый кадр, пока объект находится в столкновении
    /// </summary>
    protected override void PROnCollisionStay(Collision collision)
    {
        base.PROnCollisionStay(collision);

        OnCollisionStayEvent?.Invoke(collision);

        refObject?.InvokePROnCollisionStay(this, collision);
    }

    /// <summary>
    /// Вызывается при завершении столкновения
    /// </summary>
    protected override void PROnCollisionExit(Collision collision)
    {
        base.PROnCollisionExit(collision);

        OnCollisionEnterExitEvent?.Invoke(collision);

        refObject?.InvokePROnCollisionExit(this, collision);
    }
}