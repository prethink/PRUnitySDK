using UnityEngine;

/// <summary>
/// Ставит экранный canvas постоянного интерфейса на учёт в <see cref="HudTracker"/>.
/// </summary>
/// <remarks>
/// Обычный <see cref="MonoBehaviour"/>: компонент только гасит canvas и ни на что
/// не подписывается, а регистрация в трекерах и сохранениях ему ни к чему.
/// </remarks>
[RequireComponent(typeof(Canvas))]
public class HudCanvasElement : MonoBehaviour, IHudElement
{
    private Canvas canvas;

    /// <inheritdoc />
    public void SetHudVisible(bool isVisible)
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();

        if (canvas != null)
            canvas.enabled = isVisible;
    }

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        PRUnitySDK.Trackers.Hud.Register(this);
    }

    private void OnDestroy()
    {
        PRUnitySDK.Trackers.Hud.Unregister(this);
    }
}
