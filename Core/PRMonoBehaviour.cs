using System.Collections.Generic;
using UnityEngine;

public abstract partial class PRMonoBehaviour : MonoBehaviour, IPauseStateListener, IReadySceneGameEvent, IReadyGameEvent
{
    #region MonoBehaviour

    private readonly HashSet<Collider> collidersInside = new();

    protected virtual void Awake()
    {
        InitializationComponents(); 
    }

    protected virtual void Start()  { }

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
        EventBus.Subscribe(this);
    }

    protected virtual void OnDisable()
    {
        EventBus.Unsubscribe(this);
    }

    protected virtual void OnValidate()
    {
        InitializationComponents();
    }

    private void OnTriggerStay(Collider other)
    {
        if(PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        if (Time.time < LastTriggerTick + PROnTriggerStayTimeout())
            return;

        LastTriggerTick = Time.time;

        PROnTriggerStay(other);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused || !collidersInside.Add(other))
            return;

        PROnTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused || collidersInside.Remove(other) )
            return;

        PROnTriggerExit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PROnCollisionEnter(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        if (Time.time < LastCollisionTick + PROnCollisionStayTimeout())
            return;

        LastCollisionTick = Time.time;

        PROnCollisionStay(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PROnCollisionExit(collision);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PROnTriggerEnter2D(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PROnTriggerExit2D(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if ( PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PROnTriggerStay2D(collision);
    }

    #endregion

    #region Поля и свойства

    /// <summary>
    /// Отслеживаемые rigidbodies.
    /// </summary>
    protected List<Rigidbody> rigidbodies = new List<Rigidbody>();

    /// <summary>
    /// Состояние rigidbodies.
    /// </summary>
    protected Dictionary<Rigidbody, RigidbodyData> rigidbodyStates = new();


    protected readonly HashSet<Animator> animators = new();

    protected readonly Dictionary<Animator, AnimatorData> animatorStates = new();

    /// <summary>
    /// Время тика.
    /// </summary>
    public long TickTime { get; protected set; }

    public float LastCollisionTick { get; protected set; } = -Mathf.Infinity;

    public float LastTriggerTick { get; protected set; } = -Mathf.Infinity;

    #endregion

    #region IPauseNotify
    public virtual void OnPauseStateChanged(PauseEventArgs args)
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused)
        {
            PauseRigidBody();
            PauseAnimators();
        }
        else
        {
            ResumeRigidBody();
            ResumeAnimators();
        }
    }

    public virtual void OnReadyGame() { }

    public virtual void OnReadyScene() { }

    #endregion

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

        //this.DelayAction(timeout, (t) => Destroy(obj));  

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

    protected virtual bool PRPreUpdate() { return true; }

    protected virtual void PRUpdate() { }

    protected virtual void PRPostUpdate() { }

    protected virtual void InitializationComponents()
    {

    }

    protected virtual void PRLateUpdate() { }

    protected virtual void PRFixedUpdate() { }

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

    /// <summary>
    /// Регистрация rigidBody.
    /// </summary>
    /// <param name="rigidbody">rigidbody.</param>
    protected virtual void RegisterRigidBody(Rigidbody rigidbody)
    {
        if (rigidbody == null)
            return;

        rigidbodies.Add(rigidbody);
        OnPauseStateChanged(new PauseEventArgs());
    }

    protected virtual void ResumeRigidBody()
    {
        foreach (var pair in rigidbodyStates)
        {
            var rb = pair.Key;
            var data = pair.Value;

            if (rb == null || rb.isKinematic)
                continue;
            //TODO: Баг с бесконечным открыванием магазина обби
            //rb.isKinematic = data.IsKinematic;
            rb.useGravity = data.UseGravity;
            rb.velocity = data.Velocity;
            rb.angularVelocity = data.AngularVelocity;
        }

        rigidbodyStates.Clear();
    }

    protected virtual void PauseRigidBody()
    {
        foreach (var rb in rigidbodies)
        {
            if (rb == null || rb.isKinematic || rigidbodyStates.TryGetValue(rb, out var _))
                continue;

            rigidbodyStates[rb] = new RigidbodyData
            {
                Velocity = rb.velocity,
                AngularVelocity = rb.angularVelocity,
                UseGravity = rb.useGravity
                //IsKinematic = rb.isKinematic
            };

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            //rb.isKinematic = true;
        }
    }

    protected void RegisterAnimator(Animator animator)
    {
        if (animator == null)
            return;

        animators.Add(animator);
        OnPauseStateChanged(new PauseEventArgs()); // если сразу надо применить
    }

    private void PauseAnimators()
    {
        foreach (var animator in animators)
        {
            if (animator == null || animatorStates.ContainsKey(animator))
                continue;

            animatorStates[animator] = new AnimatorData
            {
                Speed = animator.speed,
                Enabled = animator.enabled
            };

            animator.speed = 0f;
        }
    }

    private void ResumeAnimators()
    {
        foreach (var pair in animatorStates)
        {
            var animator = pair.Key;
            var data = pair.Value;

            if (animator == null)
                continue;

            animator.speed = data.Speed;
        }

        animatorStates.Clear();
    }

    #endregion

    protected struct RigidbodyData
    {
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        public bool IsKinematic;
        public bool UseGravity;
    }

    protected class AnimatorData
    {
        public float Speed;
        public bool Enabled;
    }
}