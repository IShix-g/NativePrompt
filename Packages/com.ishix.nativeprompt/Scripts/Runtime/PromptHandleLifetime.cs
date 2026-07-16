using System;
using System.Threading;

namespace NativePrompt
{
    internal sealed class PromptHandleLifetime
    {
        private static readonly Action<object> DisposeFromCancellationCallback =
            state => ((PromptHandleLifetime)state).DisposeFromCancellation();

        private readonly object _gate = new object();
        private Action _dismiss;
        private Action _dispose;
        private Action _cancel;
        private CancellationTokenRegistration _registration;
        private bool _hasRegistration;
        private State _state;

        internal PromptHandleLifetime(Action dismiss, Action dispose, Action cancel = null)
        {
            _dismiss = dismiss ?? throw new ArgumentNullException(nameof(dismiss));
            _dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
            _cancel = cancel ?? dispose;
        }

        internal void Dismiss()
        {
            Action dismiss;
            lock (_gate)
            {
                if (_state != State.Active)
                {
                    return;
                }

                _state = State.DismissRequested;
                dismiss = _dismiss;
                _dismiss = null;
            }

            dismiss();
        }

        internal void Dispose()
        {
            Dispose(fromCancellation: false);
        }

        private void DisposeFromCancellation()
        {
            Dispose(fromCancellation: true);
        }

        private void Dispose(bool fromCancellation)
        {
            Action dispose;
            CancellationTokenRegistration registration = default;
            bool hasRegistration;
            lock (_gate)
            {
                if (_state == State.Disposed || _state == State.Completed)
                {
                    return;
                }

                _state = State.Disposed;
                _dismiss = null;
                dispose = fromCancellation ? _cancel : _dispose;
                _dispose = null;
                _cancel = null;
                hasRegistration = TakeRegistration(out registration);
            }

            try
            {
                dispose();
            }
            finally
            {
                if (hasRegistration && !fromCancellation)
                {
                    registration.Dispose();
                }
            }
        }

        internal void AddTo(CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (_state == State.Disposed || _state == State.Completed)
                {
                    return;
                }
            }

            CancellationTokenRegistration registration =
                cancellationToken.Register(
                    DisposeFromCancellationCallback,
                    this,
                    useSynchronizationContext: false);
            CancellationTokenRegistration previous = default;
            bool disposeRegistration = false;
            bool hasPrevious = false;
            lock (_gate)
            {
                if (_state == State.Disposed || _state == State.Completed)
                {
                    disposeRegistration = true;
                }
                else
                {
                    hasPrevious = TakeRegistration(out previous);
                    _registration = registration;
                    _hasRegistration = true;
                }
            }

            if (hasPrevious)
            {
                previous.Dispose();
            }
            if (disposeRegistration)
            {
                registration.Dispose();
            }
        }

        internal bool TryComplete()
        {
            CancellationTokenRegistration registration = default;
            bool hasRegistration;
            lock (_gate)
            {
                if (_state == State.Disposed || _state == State.Completed)
                {
                    return false;
                }

                _state = State.Completed;
                _dismiss = null;
                _dispose = null;
                _cancel = null;
                hasRegistration = TakeRegistration(out registration);
            }

            if (hasRegistration)
            {
                registration.Dispose();
            }

            return true;
        }

        private bool TakeRegistration(out CancellationTokenRegistration registration)
        {
            registration = _registration;
            if (!_hasRegistration)
            {
                return false;
            }

            _registration = default;
            _hasRegistration = false;
            return true;
        }

        private enum State
        {
            Active,
            DismissRequested,
            Disposed,
            Completed
        }
    }
}
