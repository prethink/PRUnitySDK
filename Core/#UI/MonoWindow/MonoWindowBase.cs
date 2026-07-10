using UnityEngine;
using UnityEngine.UI;

public abstract partial class MonoWindowBase : PRMonoBehaviour
{
    public abstract Enumeration Key { get; }

    [Header("Заголовок")]
    [SerializeField] protected GameObject container;
    [SerializeField] protected RectTransform header;
    [SerializeField] protected Image iconHeader;
    [SerializeField] protected LocalizationObserver titleHeader;
    [SerializeField] protected Button exitButton;

    [Header("Тело")]
    [SerializeField] protected RectTransform body;

    [SerializeField] protected bool setPauseWhenOpen;

    

    public virtual void Show(MonoWindowArgs args)
    {
        GetContainer().SetActive(true);
        GetContainer().RefreshLayoutGroupsImmediateAndRecursive();
        PRUnitySDK.SetWindowsState(true);
        if (setPauseWhenOpen)
            PRUnitySDK.PauseManager.SetLogicPaused(true, this);

        Cursor.visible = true;
    }

    public virtual void Hide(bool isForceClose = false)
    {
        if (GetContainer().activeSelf)
            GameManager.Instance.StartSaveTask();

        PRUnitySDK.SetWindowsState(false);
        GetContainer().SetActive(false);
        PRUnitySDK.PauseManager.SetLogicPaused(false, this);
        GameManager.Instance.LoadingUserCursorState();
    }

    protected GameObject GetContainer()
    {
        return container != null 
            ? container 
            : gameObject;
    }

    public abstract void InitTranslate();

    protected virtual void ExitButtonAction()
    {
        Hide();
    }

    protected override void OnEnable()
    {
        exitButton?.onClick.AddListener(() => { Hide(); });
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        exitButton?.onClick.RemoveAllListeners();
        base.OnDisable();
    }

    protected override void RegisterEventsOnCreated()
    {
        PRUnitySDK.Trackers.MonoWindows.Register(this);
        base.RegisterEventsOnCreated();
    }

    protected override void UnRegisterEventsOnDestroy()
    {
        PRUnitySDK.Trackers.MonoWindows.Unregister(this);
        base.UnRegisterEventsOnDestroy();
    }
}