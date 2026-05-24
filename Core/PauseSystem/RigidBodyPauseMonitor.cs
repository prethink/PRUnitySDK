using System.Collections.Generic;
using UnityEngine;

public class RigidBodyPauseMonitor : MonoBehaviour, IPauseStateListener
{
    protected struct RigidbodyData
    {
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        public bool IsKinematic;
        public bool UseGravity;
    }

    /// <summary>
    /// Отслеживаемые rigidbodies.
    /// </summary>
    protected List<Rigidbody> rigidbodies = new List<Rigidbody>();

    /// <summary>
    /// Состояние rigidbodies.
    /// </summary>
    protected Dictionary<Rigidbody, RigidbodyData> rigidbodyStates = new();

    #region MonoBehaviour

    private void OnEnable()
    {
        EventBus.Subscribe(this);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(this);
    }

    private void Awake()
    {
        var rigidBodies = this.gameObject.GetComponentsInSelfOrChildren<Rigidbody>();
        foreach (var rb in rigidBodies)
            RegisterRigidBody(rb);
    }

    #endregion

    /// <summary>
    /// Регистрация rigidBody.
    /// </summary>
    /// <param name="rigidbody">rigidbody.</param>
    public virtual void RegisterRigidBody(Rigidbody rigidbody)
    {
        if (rigidbody == null)
            return;

        rigidbodies.Add(rigidbody);
        OnPauseStateChanged(new PauseStateEventArgs());
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
                UseGravity = rb.useGravity,
                IsKinematic = rb.isKinematic
            };

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            //rb.isKinematic = true;
        }
    }

    #region IPauseStateListener

    public void OnPauseStateChanged(PauseStateEventArgs args)
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused)
            PauseRigidBody();
        else
            ResumeRigidBody();
    }

    #endregion
}
