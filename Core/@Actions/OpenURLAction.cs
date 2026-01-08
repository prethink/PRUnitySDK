using UnityEngine;

[CreateAssetMenu(fileName = "URL Action", menuName = "PRUnitySDK/Actions/Open url action")]
public class OpenURLAction : ActionBase
{
    #region ScriptableObject

    /// <summary>
    /// —сылка которую нужно открыть.
    /// </summary>
    [SerializeField] protected string URL;

    #endregion

    #region Ѕазовый класс

    protected override void Action()
    {
        Application.OpenURL(URL);
    }

    #endregion
}
