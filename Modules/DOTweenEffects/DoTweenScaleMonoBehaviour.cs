using DG.Tweening;
using UnityEngine;

public class DoTweenScaleMonoBehaviour : DoTweenBaseEffectMonoBehaviour
{
    [Header("Scale")]
    [SerializeField] private Vector3 scale;

    public void ChangeScale(Vector3 scale)
    {
        this.scale = scale;
    }

    public Vector3 Scale => scale;

    public override Tween CreateAnimation()
    {
        IsCreated = true;
        tween?.Kill();

        Vector3 startScale = gameObject.transform.localScale;

        // ќпредел€ем, какие оси нужно анимировать
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
            return null; // ≈сли изменени€ нет, то и анимации нет
        }

        return tween.SetEase(ease).SetLoops(loopCount, loopType);
    }

    public void Init(Vector3 scale, float duration, int loopCount, Ease ease, LoopType loopType)
    {
        this.scale = scale;
        this.duration = duration;
        this.loopCount = loopCount;
        this.ease = ease;
        this.loopType = loopType;
    }
}
