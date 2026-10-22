#if !PRSDK_DISABLE_YG2
using YG;

public partial class Bootstrap
{
    [OverrideBootstrap]
    public void OverrideInitialize()
    {
        isOverriden = true;
    }

    [MethodHook(MethodHookStage.PostOnEnable)]
    private void OnEnableYG()
    {
        YG2.onGetSDKData += InitializeFromPlatform;
        //TODO:YG2.onDefaultSaves += InitializeSDK;

        // Отписки нет намеренно: загрузчик исчезает вместе со своей сценой раньше сигнала.
        GameShownSignal.SubscribeOnReady(ReportGameReady);
    }

    [MethodHook(MethodHookStage.PostOnDisable)]
    private void OnDisableYG()
    {
        YG2.onGetSDKData -= InitializeFromPlatform;
        //TODO:YG2.onDefaultSaves -= InitializeSDK;
    }

    /// <summary>
    /// Данные площадки пришли: язык игрока уже известен, и экран загрузки можно подписать до сборки SDK.
    /// </summary>
    private void InitializeFromPlatform()
    {
        platformLanguageCode = YG2.lang;
        InitializeSDK();
    }

    /// <summary>
    /// Сообщает площадке, что игра загружена и игрок может играть.
    /// </summary>
    /// <remarks>
    /// Вручную, а не настройкой плагина <c>autoGRA</c> (она выключена): плагин сообщил бы о готовности
    /// сразу после своего запуска, ещё до экрана загрузки. Модерация Яндекс Игр проверяет, что после
    /// этого сообщения игрок уже не ждёт загрузку.
    /// </remarks>
    private static void ReportGameReady()
    {
        YG2.GameReadyAPI();
    }
}
#endif
