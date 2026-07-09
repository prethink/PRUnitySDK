using UnityEngine;

[CreateAssetMenu(fileName = "Lang Action", menuName = "PRUnitySDK/Actions/Lang action")]
public class LangAction : ActionBase
{
    #region ScriptableObject

    /// <summary>
    /// язык.
    /// </summary>
    [SerializeField] protected LangType lang;

    #endregion

    #region Ѕазовый класс

    protected override void Action()
    {
        PRUnitySDK.LanguageManager.SwitchLang(LocalizationUtils.GetLanguageCode(lang));
    }

    #endregion
}
