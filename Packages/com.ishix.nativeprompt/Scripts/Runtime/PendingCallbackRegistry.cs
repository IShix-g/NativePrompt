using System;
using System.Collections.Generic;

namespace NativePrompt
{
    internal sealed class PendingCallbackRegistry
    {
        private readonly object _gate = new object();
        private readonly Dictionary<string, PendingCallback> _callbacks =
            new Dictionary<string, PendingCallback>(StringComparer.Ordinal);
        private readonly IMainThreadDispatcher _dispatcher;

        internal PendingCallbackRegistry(IMainThreadDispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        internal int Count
        {
            get
            {
                lock (_gate)
                {
                    return _callbacks.Count;
                }
            }
        }

        internal void Register<T>(string requestId, Action<T> callback)
        {
            if (requestId == null)
            {
                throw new ArgumentNullException(nameof(requestId));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            lock (_gate)
            {
                _callbacks.Add(requestId, new PendingCallback(typeof(T), value => callback((T)value)));
            }
        }

        internal bool TryClaim<T>(string requestId, T result, out Action dispatch)
        {
            PendingCallback pending;
            lock (_gate)
            {
                if (!_callbacks.TryGetValue(requestId, out pending) ||
                    pending.State != PendingCallbackState.Pending ||
                    pending.ResultType != typeof(T))
                {
                    dispatch = null;
                    return false;
                }

                pending.State = PendingCallbackState.Scheduled;
            }

            dispatch = () => _dispatcher.Post(() => Deliver(requestId, pending, result));
            return true;
        }

        internal void Cancel(string requestId)
        {
            lock (_gate)
            {
                if (_callbacks.TryGetValue(requestId, out PendingCallback pending))
                {
                    pending.State = PendingCallbackState.Cancelled;
                    _callbacks.Remove(requestId);
                }
            }
        }

        internal void Clear()
        {
            lock (_gate)
            {
                foreach (PendingCallback pending in _callbacks.Values)
                {
                    pending.State = PendingCallbackState.Cancelled;
                }

                _callbacks.Clear();
            }
        }

        private void Deliver<T>(string requestId, PendingCallback pending, T result)
        {
            Action<object> callback;
            lock (_gate)
            {
                if (pending.State != PendingCallbackState.Scheduled ||
                    !_callbacks.TryGetValue(requestId, out PendingCallback current) ||
                    !ReferenceEquals(current, pending))
                {
                    return;
                }

                _callbacks.Remove(requestId);
                pending.State = PendingCallbackState.Completed;
                callback = pending.Callback;
            }

            callback(result);
        }

        private sealed class PendingCallback
        {
            internal PendingCallback(Type resultType, Action<object> callback)
            {
                ResultType = resultType;
                Callback = callback;
            }

            internal Type ResultType { get; }

            internal Action<object> Callback { get; }

            internal PendingCallbackState State { get; set; }
        }

        private enum PendingCallbackState
        {
            Pending,
            Scheduled,
            Completed,
            Cancelled
        }
    }
}
