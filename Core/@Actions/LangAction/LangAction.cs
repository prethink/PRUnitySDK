using UnityEngine;

[CreateAssetMenu(fileName = "Lang Action", menuName = "PRUnitySDK/Actions/Lang action")]
public class LangAction : ActionBase
{
    #region ScriptableObject

    /// <summary>
    /// Язык.
    /// </summary>
    [SerializeField] protected LangType lang;

    #endregion

    #region Базовый класс

    protected override ActionResult Action()
    {
        string code = LocalizationUtils.GetLanguageCode(lang);
        PRUnitySDK.LanguageManager.SwitchLang(code);

        // Только выбор игрока: язык, который площадка подставила при запуске, сюда не попадает.
        PRUnitySDK.Metric?.SendBranch("settings", "language", code);
        return ActionResult.Success;
    }

    #endregion
}
