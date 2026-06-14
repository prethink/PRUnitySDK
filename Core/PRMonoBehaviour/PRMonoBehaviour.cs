using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public abstract partial class PRMonoBehaviour : MonoBehaviour, IPauseStateListener, IReadySceneGameEvent, IReadyGameEvent, ISaveable
{
    #region MonoBehaviour

    protected virtual void Awake()
    {
        InitializationComponents(); 
    }

    protected virtual void Start()  
    {
        if(UseCoroutineLateFixedUpdate())
            StartCoroutine(LateFixedUpdate());

        if (UseCoroutineWaitForEndOfFrame())
            StartCoroutine(WaitForEndOfFrame());
    }

    protected virtual void Update()
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        if (!PRPreUpdate())
            return;

        PRUpdate();
        PRPostUpdate();
    }

    private void LateUpdate()
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PRLateUpdate();
    }

    private void FixedUpdate()
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PRFixedUpdate();
    }

    protected virtual void OnEnable()
    {
        
    }

    protected virtual void OnDisable()
    {

    }

    protected virtual void OnValidate()
    {

    }

    protected virtual void RegisterEventsOnCreated()
    {
        EventBus.Subscribe(this);
        PRUnitySDK.Trackers.Saveables.Add(this);
    }

    protected virtual void UnRegisterEventsOnDestroy()
    {
        EventBus.Unsubscribe(this);
        PRUnitySDK.Trackers.Saveables.Remove(this);
    }

    private void OnTriggerStay(Collider other)
    {
        if (this.IsMethodDisabled(nameof(OnTriggerStay)))
            return;

        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        if (PRTime.Instance.Time < LastTriggerTick + PROnTriggerStayTimeout())
            return;

        LastTriggerTick = PRTime.Instance.Time;

        PROnTriggerStay(other);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (this.IsMethodDisabled(nameof(OnTriggerEnter)))
            return;

        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PROnTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (this.IsMethodDisabled(nameof(OnTriggerExit)))
            return;

        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PROnTriggerExit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (this.IsMethodDisabled(nameof(OnCollisionEnter)))
            return;

        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PROnCollisionEnter(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (this.IsMethodDisabled(nameof(OnCollisionStay)))
            return;

        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        if (Time.time < LastCollisionTick + PROnCollisionStayTimeout())
            return;

        LastCollisionTick = Time.time;

        PROnCollisionStay(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (this.IsMethodDisabled(nameof(OnCollisionExit)))
            return;

        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PROnCollisionExit(collision);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (this.IsMethodDisabled(nameof(OnTriggerEnter2D)))
            return;

        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PROnTriggerEnter2D(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (this.IsMethodDisabled(nameof(OnTriggerExit2D)))
            return;

        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PROnTriggerExit2D(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (this.IsMethodDisabled(nameof(OnTriggerStay2D)))
            return;

        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PROnTriggerStay2D(collision);
    }

    #endregion

    #region Поля и свойства

    /// <summary>
    /// Время тика.
    /// </summary>
    public long TickTime { get; protected set; }

    public float LastCollisionTick { get; protected set; } = -Mathf.Infinity;

    public float LastTriggerTick { get; protected set; } = -Mathf.Infinity;

    #endregion

    #region IPauseStateListener

    public virtual void OnPauseStateChanged(PauseStateEventArgs args)
    {
        if(this.IsMethodDisabled(nameof(OnPauseStateChanged)))
            return;

        this.RunMethodHooks(MethodHookStage.Pause);
    }

    #endregion

    public virtual void OnReadyGame() { }

    public virtual void OnReadyScene() { }

    #region Методы

    public virtual void PRDestroy(GameObject obj, float timeout)
    {
        if (timeout < 0)
            return;

        if (timeout == 0)
        {
            Destroy(obj);
            return;
        }

        this.DelayAction(timeout, (t) => Destroy(obj));
    }

    public virtual void PRDestroy(GameObject obj)
    {
        PRDestroy(obj, 0);
    }

    public virtual void PRDestroy(MonoBehaviour obj)
    {
        PRDestroy(obj.gameObject, 0);
    }

    public virtual void PRDestroy(MonoBehaviour obj, float timeout)
    {
        PRDestroy(obj.gameObject, timeout);
    }

    private void OnDestroy()
    {
        UnRegisterEventsOnDestroy();
    }

    protected virtual bool PRPreUpdate() { return true; }

    protected virtual void PRUpdate() { }

    protected virtual void PRPostUpdate() { }

    protected virtual void InitializationComponents()
    {
        RegisterEventsOnCreated();
    }

    protected virtual void PRLateUpdate() { }

    protected virtual void PRFixedUpdate() { }

    protected virtual void PRLateFixedUpdate() { }

    protected virtual void PREndOfFrame() { }

    protected virtual float PROnTriggerStayTimeout()
    {
        return 0f;
    }

    protected virtual void PROnTriggerStay(Collider other) { }

    protected virtual void PROnTriggerEnter(Collider other) { }

    protected virtual void PROnTriggerExit(Collider other) { }

    protected virtual void PROnCollisionEnter(Collision collision) { }

    protected virtual float PROnCollisionStayTimeout()
    {
        return 0f;
    }

    protected virtual void PROnCollisionStay(Collision collision) { }

    protected virtual void PROnCollisionExit(Collision collision) { }

    protected virtual void PROnTriggerEnter2D(Collider2D collision) { }

    protected virtual void PROnTriggerExit2D(Collider2D collision) { }

    protected virtual void PROnTriggerStay2D(Collider2D collision) { }

    public void InvokePROnCollisionEnter(object invoker, Collision collision)
    {
        PRLog.WriteDebug(invoker, nameof(InvokePROnCollisionEnter));

        PROnCollisionEnter(collision);
    }

    public void InvokePROnCollisionStay(object invoker, Collision collision)
    {
        PRLog.WriteDebug(invoker, nameof(InvokePROnCollisionStay));

        PROnCollisionStay(collision);
    }

    public void InvokePROnCollisionExit(object invoker, Collision collision)
    {
        PRLog.WriteDebug(invoker, nameof(InvokePROnCollisionExit));

        PROnCollisionExit(collision);
    }

    public void InvokeOnTriggerEnter(object invoker, Collider other)
    {
        PRLog.WriteDebug(invoker, nameof(InvokeOnTriggerEnter));

        OnTriggerEnter(other);
    }

    public void InvokeOnTriggerStay(object invoker, Collider other)
    {
        PRLog.WriteDebug(invoker, nameof(InvokeOnTriggerStay));

        OnTriggerStay(other);
    }

    public void InvokeOnTriggerExit(object invoker, Collider other)
    {
        PRLog.WriteDebug(invoker, nameof(InvokeOnTriggerExit));

        OnTriggerExit(other);
    }

    protected virtual bool UseCoroutineLateFixedUpdate()
    {
        return false;
    }

    IEnumerator LateFixedUpdate()
    {
        WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
        while (true)
        {
            yield return WaitPause.Instance;
            yield return waitForFixedUpdate;
            PRLateFixedUpdate();
        }
    }

    protected virtual bool UseCoroutineWaitForEndOfFrame()
    {
        return false;
    }

    IEnumerator WaitForEndOfFrame()
    {
        WaitForEndOfFrame waitForFrame = new WaitForEndOfFrame();
        while (true)
        {
            yield return WaitPause.Instance;
            yield return waitForFrame;
            PREndOfFrame();
        }
    }

    public virtual async Task<bool> TrySaveData()
    {
        return true;
    }

    #endregion
}