using DG.Tweening;
using UnityEngine;

/// <summary>
/// Изменяет выбранные оси локального масштаба Transform.
/// Нулевое значение оси означает, что эту ось изменять не нужно.
/// </summary>
public class DoTweenScaleMonoBehaviour : DoTweenBaseEffectMonoBehaviour
{
    [Header("Scale")]
    [SerializeField] private Vector3 scale;

    /// <summary>
    /// Изменяет целевой масштаб для следующего CreateAnimation().
    /// </summary>
    public void ChangeScale(Vector3 scale)
    {
        this.scale = scale;
    }

    /// <summary>
    /// Целевой масштаб. Нулевые компоненты сохраняют текущий масштаб соответствующей оси.
    /// </summary>
    public Vector3 Scale => scale;

    /// <summary>
    /// Пересоздаёт анимацию масштаба или возвращает null, если изменять нечего.
    /// </summary>
    public override Tween CreateAnimation()
    {
        tween?.Kill();
        tween = null;
        IsCreated = false;

        Vector3 startScale = gameObject.transform.localScale;

        // Определяем, какие оси нужно анимировать
        bool animateX = scale.x != 0 && !Mathf.Approximately(scale.x, startScale.x);
        bool animateY = scale.y != 0 && !Mathf.Approximately(scale.y, startScale.y);
        bool animateZ = scale.z != 0 && !Mathf.Approximately(scale.z, startScale.z);

        if (animateX && animateY && animateZ)
        {
            tween = gameObject.transform.DOScale(scale, duration);
        }
        else if (animateX && animateY)
        {
            tween = gameObject.transform.DOScale(new Vector2(scale.x, scale.y), duration);
        }
        else if (animateX && animateZ)
        {
            tween = gameObject.transform.DOScale(new Vector3(scale.x, startScale.y, scale.z), duration);
        }
        else if (animateY && animateZ)
        {
            tween = gameObject.transform.DOScale(new Vector3(startScale.x, scale.y, scale.z), duration);
        }
        else if (animateX)
        {
            tween = gameObject.transform.DOScaleX(scale.x, duration);
        }
        else if (animateY)
        {
            tween = gameObject.transform.DOScaleY(scale.y, duration);
        }
        else if (animateZ)
        {
            tween = gameObject.transform.DOScaleZ(scale.z, duration);
        }
        else
        {
            return null;
        }

        IsCreated = true;
        return tween.SetEase(ease).SetLoops(loopCount, loopType);
    }

    /// <summary>
    /// Настраивает все параметры эффекта одним вызовом.
    /// </summary>
    public void Init(Vector3 scale, float duration, int loopCount, Ease ease, LoopType loopType)
    {
        this.scale = scale;
        this.duration = Mathf.Max(0f, duration);
        this.loopCount = loopCount;
        this.ease = ease;
        this.loopType = loopType;
    }
}
