using DG.Tweening;
using UnityEngine;

/// <summary>
/// Перемещает Transform: смещением от собственной позиции, в координаты родителя
/// либо к абсолютной точке мира.
/// </summary>
/// <remarks>
/// Как понимать вектор, задаёт <see cref="DoTweenMovementSpace"/>. По умолчанию это
/// смещение от себя: покачивание и подскоки — самая частая работа этого эффекта,
/// а мировая точка тянет объект со своего места туда, где его никто не ждал.
/// <para>
/// Смещение делается blendable-твином, то есть прибавкой к текущей позиции, а не записью
/// в неё. Это то, на чём держится смешивание повадок: два позиционных эффекта на одном
/// объекте складываются, а не перебивают друг друга каждый кадр.
/// </para>
/// </remarks>
public class DoTweenMovementMonoBehaviour : DoTweenBaseEffectMonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Tooltip("Как понимать вектор: смещением от себя, точкой у родителя или точкой в мире.")]
    private DoTweenMovementSpace space = DoTweenMovementSpace.Offset;

    [SerializeField] private Vector3 movement;

    /// <summary>
    /// Задаёт, как понимать вектор перемещения.
    /// </summary>
    public DoTweenMovementMonoBehaviour SetSpace(DoTweenMovementSpace space)
    {
        this.space = space;
        return this;
    }

    /// <summary>
    /// Задаёт вектор перемещения для следующего <see cref="CreateAnimation"/>.
    /// </summary>
    public DoTweenMovementMonoBehaviour SetMovement(Vector3 movement)
    {
        this.movement = movement;
        return this;
    }

    /// <summary>
    /// Пересоздаёт анимацию перемещения.
    /// </summary>
    /// <remarks>
    /// Смещение от себя рассчитано на <c>Loop Type = Yoyo</c>: объект уходит на вектор
    /// и возвращается ровно туда, где стоял. Запоминать исходную точку при этом не нужно —
    /// прибавка считается от текущей позиции, где бы объект ни оказался.
    /// </remarks>
    public override Tween CreateAnimation()
    {
        KillCurrent();

        if (space == DoTweenMovementSpace.World)
            tween = transform.DOMove(movement, duration);
        else if (space == DoTweenMovementSpace.Local)
            tween = transform.DOLocalMove(movement, duration);
        else
            tween = transform.DOBlendableLocalMoveBy(movement, duration);

        tween = tween
            .SetLoops(loopCount, loopType)
            .SetEase(ease);

        IsCreated = true;

        return tween;
    }

    /// <inheritdoc />
    public override void DestroyAnimation()
    {
        RewindBlendable();

        base.DestroyAnimation();
    }

    /// <summary>
    /// Убивает текущий твин, не оставив после себя смещения.
    /// </summary>
    private void KillCurrent()
    {
        RewindBlendable();

        tween?.Kill();
        tween = null;
    }

    /// <summary>
    /// Возвращает blendable-твину нулевую прибавку перед смертью.
    /// </summary>
    /// <remarks>
    /// Blendable не записывает позицию, а прибавляет к ней, поэтому убитый на полпути
    /// твин оставляет свой вклад в объекте навсегда, и следующая анимация начинает
    /// со сдвига. <c>Rewind</c> снимает именно его: записать вместо этого исходную
    /// позицию нельзя — вместе со своим вкладом стёрся бы и чужой, а на объекте может
    /// висеть ещё одна позиционная повадка.
    /// </remarks>
    private void RewindBlendable()
    {
        if (space != DoTweenMovementSpace.Offset)
            return;

        if (tween != null && tween.active)
            tween.Rewind();
    }
}
