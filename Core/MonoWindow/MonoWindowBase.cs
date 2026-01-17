using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract partial class MonoWindowBase : PRMonoBehaviour
{
    public abstract string Key { get; }

    [Header("Главный контейнер")]
    [SerializeField] protected GameObject container;

    [Header("Заголовок")]
    [SerializeField] protected RectTransform header;
    [SerializeField] protected Image iconHeader;
    [SerializeField] protected TextMeshProUGUI titleHeader;
    [SerializeField] protected Button exitButton;

    [Header("Тело")]
    [SerializeField] protected RectTransform body;

    [SerializeField] protected bool setPauseWhenOpen;

    /// <summary>
    /// Окно в процессе изменения.
    /// </summary>
    public bool IsStateChanging { get; protected set; }

    public bool IsInitTranslate { get; protected set; }

    public int ExecuterId { get; protected set; }   

    public virtual void Show(MonoWindowArgs args)
    {
        container.SetActive(true);
        UpdateTranslate();
        container.RefreshLayoutGroupsImmediateAndRecursive();
        ExecuterId = args.Executer;
        PRUnitySDK.SetWindowsState(true);
        if (setPauseWhenOpen)
            PRUnitySDK.PauseManager.SetLogicPaused(true, this);

        Cursor.visible = true;
    }

    public virtual void Hide(bool isForceClose = false)
    {
        if (container.activeSelf)
            GameManager.Instance.SaveData();

        PRUnitySDK.SetWindowsState(false);
        container.SetActive(false);
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
        //uiWatcher.Register(this);
        UpdateTranslate();
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        exitButton?.onClick.RemoveAllListeners();
        //uiWatcher.UnRegister(this);
        base.OnDisable();
    }
}