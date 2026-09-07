using System;
using System.Collections.Generic;

namespace PRGameSessions
{
    /// <summary>
    /// Освобождает ресурсы только своего экземпляра сессии или раунда.
    /// </summary>
    public sealed class SessionScope : IDisposable
    {
        private readonly HashSet<IDisposable> resources = new();
        private readonly Action<Exception> reportError;
        public Guid Id { get; } = Guid.NewGuid();
        public bool IsClosed { get; private set; }
        public int Count => resources.Count;

        internal SessionScope(Action<Exception> reportError) => this.reportError = reportError;

        /// <summary>
        /// Добавляет ресурс до начала очистки. Один ресурс не должен принадлежать двум scope.
        /// </summary>
        public void Own(IDisposable resource)
        {
            if (resource == null) throw new ArgumentNullException(nameof(resource));
            if (IsClosed) throw new InvalidOperationException("The session scope is closed.");
            resources.Add(resource);
        }

        public bool Forget(IDisposable resource) => resources.Remove(resource);

        public void Dispose()
        {
            if (IsClosed) return;
            IsClosed = true;
            var snapshot = new List<IDisposable>(resources);
            resources.Clear();
            foreach (var resource in snapshot)
            {
                try { resource.Dispose(); }
                catch (Exception exception) { reportError(exception); }
            }
        }
    }
}
