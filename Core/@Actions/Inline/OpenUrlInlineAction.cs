using System;
using UnityEngine;

/// <summary>
/// Встроенное действие: открывает ссылку.
/// </summary>
/// <remarks>
/// Тот же смысл, что у ассета <see cref="OpenURLAction"/>, но настраивается прямо
/// в инспекторе владельца - удобно, когда ссылка уникальна для одной кнопки.
/// </remarks>
[Serializable]
public class OpenUrlInlineAction : InlineActionBase
{
    [SerializeField]
    [Tooltip("Абсолютный http- или https-адрес.")]
    private string url;

    /// <inheritdoc />
    public override ActionResult CanExecute()
    {
        ActionResult availability = base.CanExecute();
        if (availability.IsFailed)
            return availability;
        return Uri.TryCreate(url, UriKind.Absolute, out Uri uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? ActionResult.Success : ActionResult.Fail(ActionLabels.InvalidUrl);
    }

    /// <inheritdoc />
    protected override ActionResult Action()
    {
        Application.OpenURL(url);
        return ActionResult.Success;
    }
}
