using System.Linq;
using UnityEngine;

public partial class Bootstrap : MonoBehaviour, ISDKEvents
{
    #region  Поля и свойства

    /// <summary>
    /// Признак того, что инициализация SDK была переопределена.
    /// </summary>
    private bool isOverriden;

    /// <summary>
    /// Предотвращает повторную смену сцены при одновременном получении EventBus и ReadySignal.
    /// </summary>
    private bool sceneChangeRequested;

    #endregion

    #region MonoBehaviour

    /// <inheritdoc />
    private void Awake()
    {
        TryOverrideBootstrap();

        if (!isOverriden)
            InitializeSDK();
    }

    private void OnEnable()
    {
        this.RunMethodHooks(MethodHookStage.PreOnEnable);

        EventBus.Subscribe(this);
        PRUnitySDK.ReadySignal.SubscribeOnReady(OnInitialized);

        this.RunMethodHooks(MethodHookStage.PostOnEnable);
    }

    // Отписываемся от ивента onGetSDKData
    private void OnDisable()
    {
        this.RunMethodHooks(MethodHookStage.PreOnDisable);

        PRUnitySDK.ReadySignal.UnSubscribe(OnInitialized);
        EventBus.Unsubscribe(this);

        this.RunMethodHooks(MethodHookStage.PostOnDisable);
    }

    #endregion

    #region Методы

    /// <summary>
    /// Перехват метода инициализации SDK для возможности кастомной инициализации.
    /// </summary>
    private void TryOverrideBootstrap()
    {
        var overrideMethod = this.GetMethods<OverrideBootstrapAttribute>().FirstOrDefault();
        overrideMethod?.Invoke(this, null);
    }

    /// <summary>
    /// Инициализация SDK.
    /// </summary>
    private void InitializeSDK()
    {
        PRUnitySDK.InitializeSDK();
    }

    #endregion

    #region ISDKEvents

    public void OnInitialized()
    {
        if (sceneChangeRequested)
            return;

        sceneChangeRequested = true;
        SceneChanger.Instance.SceneChange(1);
    }

    #endregion
}
