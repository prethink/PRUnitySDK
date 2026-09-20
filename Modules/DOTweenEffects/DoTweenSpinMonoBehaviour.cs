using DG.Tweening;
using UnityEngine;

/// <summary>
/// Бесконечно крутит Transform вокруг собственной оси.
/// </summary>
/// <remarks>
/// Отдельно от <see cref="DoTweenRotateMonoBehaviour"/>, потому что это другая задача:
/// тот поворачивает объект в заданный угол, а этот крутит без остановки, и целевого
/// угла у него нет вовсе.
/// <para>
/// Задать вечное вращение углом не выходит: <c>DORotate</c> в режиме по умолчанию идёт
/// кратчайшим путём, а кратчайший путь до 360° равен нулю — объект не шевельнётся.
/// Поэтому здесь <c>RotateMode.LocalAxisAdd</c>: поворот задаётся прибавкой, а не целью,
/// и складывается с другими поворотными повадками на том же объекте.
/// </para>
/// </remarks>
public class DoTweenSpinMonoBehaviour : DoTweenBaseEffectMonoBehaviour
{
    [Header("Spin")]
    [SerializeField, Tooltip("Ось вращения в координатах объекта.")]
    private Vector3 axis = Vector3.up;

    [SerializeField, Tooltip("Градусов за один цикл. Длительность цикла задаёт Duration.")]
    private float degreesPerCycle = 360f;

    /// <summary>
    /// Задаёт ось вращения.
    /// </summary>
    public DoTweenSpinMonoBehaviour SetAxis(Vector3 axis)
    {
        this.axis = axis;
        return this;
    }

    /// <summary>
    /// Задаёт, на сколько градусов объект поворачивается за цикл.
    /// </summary>
    public DoTweenSpinMonoBehaviour SetDegreesPerCycle(float degreesPerCycle)
    {
        this.degreesPerCycle = degreesPerCycle;
        return this;
    }

    /// <summary>
    /// Пересоздаёт анимацию вращения.
    /// </summary>
    public override Tween CreateAnimation()
    {
        tween?.Kill();
        tween = null;
        IsCreated = false;

        Vector3 direction = axis.sqrMagnitude > 0f ? axis.normalized : Vector3.up;

        tween = transform
            .DOLocalRotate(direction * degreesPerCycle, duration, RotateMode.LocalAxisAdd)
            .SetLoops(loopCount, loopType)
            .SetEase(ease);

        IsCreated = true;

        return tween;
    }

    /// <summary>
    /// Значения, с которыми вращение выглядит вращением сразу после добавления.
    /// </summary>
    /// <remarks>
    /// Крутиться нужно ровно и без конца, а настройки базового класса рассчитаны
    /// на разовый эффект: с ними объект дёргался бы рывками по одному обороту.
    /// </remarks>
    private void Reset()
    {
        ease = Ease.Linear;
        loopType = LoopType.Restart;
        loopCount = -1;
        duration = 2f;
        playAnimationOnStart = true;
    }
}
