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

    public override ActionResult CanExecute()
    {
        ActionResult availability = base.CanExecute();
        if (availability.IsFailed)
            return availability;
        return Uri.TryCreate(URL, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? ActionResult.Success : ActionResult.Fail(ActionLabels.InvalidUrl);
    }

    protected override ActionResult Action()
    {
        Application.OpenURL(URL);
        return ActionResult.Success;
    }

    #endregion
}
