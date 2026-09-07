using System;
using UnityEngine;

namespace PRGameSessions
{
    /// <summary>
    /// Привязка назначается после выдачи Entity из пула и снимается при следующей выдаче.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SessionEntityBinding : MonoBehaviour
    {
        private Lease current;
        public Guid ScopeId => current?.Scope.Id ?? Guid.Empty;

        public void Attach(SessionScope scope, EntityBase entity)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (scope.IsClosed) throw new InvalidOperationException("Cannot bind to a closed scope.");
            if (entity == null || entity.gameObject != gameObject) throw new ArgumentException("Entity must belong to this object.", nameof(entity));
            if (entity.InPool) throw new InvalidOperationException("Bind the entity after taking it from the pool.");
            Detach();
            current = new Lease(this, scope, entity);
            scope.Own(current);
        }

        public void Detach() => current?.Detach();
        private void OnDestroy() => Detach();

        private sealed class Lease : IDisposable
        {
            private readonly SessionEntityBinding binding;
            private readonly EntityBase entity;
            public SessionScope Scope { get; }

            public Lease(SessionEntityBinding binding, SessionScope scope, EntityBase entity)
            {
                this.binding = binding;
                this.entity = entity;
                Scope = scope;
                entity.OnEntityDestroy += OnEntityDestroy;
                entity.PoolBehaviour.OnInitializeObject += OnPoolInitialize;
            }

            private void OnEntityDestroy(EntityBase value) => Detach();
            private void OnPoolInitialize(bool first) => Detach();

            public void Detach()
            {
                Scope.Forget(this);
                entity.OnEntityDestroy -= OnEntityDestroy;
                entity.PoolBehaviour.OnInitializeObject -= OnPoolInitialize;
                if (ReferenceEquals(binding.current, this)) binding.current = null;
            }

            public void Dispose()
            {
                bool ownsEntity = ReferenceEquals(binding.current, this);
                Detach();
                if (ownsEntity && entity != null && !entity.InPool)
                    entity.DestroyEntity();
            }
        }
    }
}
