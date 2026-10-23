using UnityEngine;

public partial class PRSDKSettings
{
    /// <summary>
    /// Правила характеристик проекта.
    /// </summary>
    /// <remarks>
    /// Не <c>GameRules</c>: так называется статический реестр, и внутри настроек имя свойства закрыло бы его.
    /// </remarks>
    [field: SerializeField]
    public GameRulesSettings StatRules { get; private set; } = new();
}
