using DG.Tweening;

public static class DoTweenExtensions
{
    /// <summary>
    /// Регистрирует tween в PRUnitySDK: пауза игры и масштаб времени его слоёв.
    /// </summary>
    /// <param name="layers">Слои времени tween; интерфейс — <see cref="TimeLayer.UI"/>.</param>
    public static T Track<T>(this T tween, TimeLayer layers, bool reactionOnPause = true) where T : Tween
    {
        DoTweenTracker.Instance.Register(tween, layers, reactionOnPause);

        return tween;
    }

    public static T Track<T>(this T tween) where T : Tween
    {
        DoTweenTracker.Instance.Register(tween);

        return tween;
    }

    public static T Track<T>(this T tween, bool reactionOnPause) where T : Tween
    {
        DoTweenTracker.Instance.Register(tween, TimeLayer.World, reactionOnPause);

        return tween;
    }
}
