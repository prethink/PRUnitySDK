using System;

namespace PRGameSessions
{
    public static class SessionEntityExtensions
    {
        /// <summary>
        /// Назначает владельца экземпляру. Способ освобождения задаёт сама Entity: Destroy либо пул.
        /// </summary>
        public static T OwnEntity<T>(this SessionScope scope, T entity) where T : EntityBase
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (scope.IsClosed) throw new InvalidOperationException("The session scope is closed.");
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var binding = entity.GetComponent<SessionEntityBinding>();
            if (binding == null) binding = entity.gameObject.AddComponent<SessionEntityBinding>();
            binding.Attach(scope, entity);
            return entity;
        }
    }
}
