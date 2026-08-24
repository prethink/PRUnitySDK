/// <summary>
/// Категория элемента в диагностике инициализации PRUnitySDK.
/// </summary>
public enum PRInitializationCategory
{
    /// <summary>
    /// Обычный тип без более узкой категории.
    /// </summary>
    Type,

    /// <summary>
    /// SDK-сервис, зарегистрированный через InitializeModuleSDK.
    /// </summary>
    Module,

    /// <summary>
    /// Runtime-менеджер из PRManagerContainer.
    /// </summary>
    Manager,

    /// <summary>
    /// Singleton, явно запускаемый при старте SDK.
    /// </summary>
    Singleton,

    /// <summary>
    /// Factory, зарегистрированная для создания singleton.
    /// </summary>
    Factory,

    /// <summary>
    /// Runtime-окно на основе MonoWindowBase.
    /// </summary>
    MonoWindow,

    /// <summary>
    /// Runtime-уведомитель на основе NotifierBase.
    /// </summary>
    Notifier
}
