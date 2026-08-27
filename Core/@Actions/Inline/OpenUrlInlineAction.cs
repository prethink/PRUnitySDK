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
    public override bool CanExecute()
    {
        return base.CanExecute()
            && Uri.TryCreate(url, UriKind.Absolute, out Uri uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <inheritdoc />
    protected override void Action()
    {
        Application.OpenURL(url);
    }
}
