using UnityEngine;
using UnityEngine.UI;

public abstract partial class MonoWindowBase : PRMonoBehaviour
{
    public abstract Enumeration Key { get; }

    [Header("Заголовок")]
    [SerializeField] protected RectTransform header;
    [SerializeField] protected Image iconHeader;
    [SerializeField] protected LocalizationObserver titleHeader;
    [SerializeField] protected Button exitButton;

    [Header("Тело")]
    [SerializeField] protected RectTransform body;

    [SerializeField] protected bool setPauseWhenOpen;

    public virtual void Show(MonoWindowArgs args)
    {
        gameObject.SetActive(true);
        gameObject.RefreshLayoutGroupsImmediateAndRecursive();
        PRUnitySDK.SetWindowsState(true);
        if (setPauseWhenOpen)
            PRUnitySDK.PauseManager.SetLogicPaused(true, this);

        Cursor.visible = true;
    }

    public virtual void Hide(bool isForceClose = false)
    {
        if (gameObject.activeSelf)
            GameManager.Instance.StartSaveTask();

        PRUnitySDK.SetWindowsState(false);
        gameObject.SetActive(false);
        PRUnitySDK.PauseManager.SetLogicPaused(false, this);
        GameManager.Instance.LoadingUserCursorState();
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