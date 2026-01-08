public partial class PRUnitySDK
{
    //#region Поля и свойства

    ///// <summary>
    ///// Сервис управления паузой.
    ///// </summary>
    //public static IPauseManager PauseManager { get; private set; }

    //#endregion

    //#region Методы

    ///// <summary>
    ///// Инициализация модуля.
    ///// </summary>
    //[InitializeMethod(InitializeType.SDK, 0)]
    //private static void InitializePauseSystem()
    //{
    //    InitializeModuleSDK(nameof(PauseManager), () =>
    //    {
    //        typeof(PRUnitySDK).TryOverrideStaticProperty(typeof(IPauseManager));
    //        InitializeDefault(nameof(PauseManager), () => PauseManager, () => { PauseManager = new PauseManager(); return PauseManager; });
    //    });
    //}

    //#endregion
}
