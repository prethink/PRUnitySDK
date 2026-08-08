using System;
using UnityEngine;

[CreateAssetMenu(fileName = "URL Action", menuName = "PRUnitySDK/Actions/Open url action")]
public class OpenURLAction : ActionBase
{
    #region ScriptableObject

    /// <summary>
    /// Ссылка которую нужно открыть.
    /// </summary>
    [SerializeField] protected string URL;

    #endregion

    #region Базовый класс

    public override bool CanExecute()
    {
        return base.CanExecute()
            && Uri.TryCreate(URL, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    protected override void Action()
    {
        Application.OpenURL(URL);
    }

    #endregion
}
