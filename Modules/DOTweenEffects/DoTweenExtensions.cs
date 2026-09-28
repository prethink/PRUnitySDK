using DG.Tweening;

public static class DoTweenExtensions
{
    /// <summary>
    /// Регистрирует tween в PRUnitySDK.
    /// </summary>
    public static T Track<T>(this T tween, Enumeration layer = null, bool reactionOnPause = true) where T : Tween
    {
        DoTweenTracker.Instance.Register(tween,layer,reactionOnPause);

        return tween;
    }

    public static T Track<T>(this T tween) where T : Tween
    {
        DoTweenTracker.Instance.Register(tween);

        return tween;
    }

    public static T Track<T>(this T tween, bool reactionOnPause) where T : Tween
    {
        DoTweenTracker.Instance.Register(tween, null, reactionOnPause);

        return tween;
    }
}