using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace NativePrompt
{
    internal sealed class NativePromptCoordinator
    {
        private readonly object _gate = new object();
        private readonly INativePromptStrategy _strategy;
        private readonly PendingCallbackRegistry _callbacks;
        private readonly Queue<AlertRequest> _alertQueue = new Queue<AlertRequest>();
        private AlertRequest _activeAlert;
        private ToastRequest _activeToast;
        private long _nextRequestId;

        internal NativePromptCoordinator(
            INativePromptStrategy strategy,
            IMainThreadDispatcher dispatcher)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _callbacks = new PendingCallbackRegistry(dispatcher);
        }

        internal int PendingCallbackCount => _callbacks.Count;

        internal void ShowAlert(AlertOptions options, Action<AlertResult> onCompleted)
        {
            var request = new AlertRequest(NextRequestId("alert"), options, onCompleted);
            bool startImmediately;
            lock (_gate)
            {
                _alertQueue.Enqueue(request);
                startImmediately = _activeAlert == null;
                if (startImmediately)
                {
                    _activeAlert = _alertQueue.Dequeue();
                }
            }

            if (startImmediately)
            {
                StartAlert(request);
            }
        }

        internal void ShowBottomSheet(
            BottomSheetOptions options,
            Action<BottomSheetResult> onCompleted)
        {
            string requestId = NextRequestId("bottom-sheet");
            _callbacks.Register<BottomSheetResult>(requestId, result => onCompleted?.Invoke(result));
            try
            {
                _strategy.ShowBottomSheet(requestId, options);
            }
            catch
            {
                _callbacks.Cancel(requestId);
                throw;
            }
        }

        internal ToastHandle ShowToast(
            ToastOptions options,
            Action<ToastDismissReason> onDismissed)
        {
            var request = new ToastRequest(NextRequestId("toast"), options, onDismissed);
            ToastRequest replaced;
            Action dispatchReplaced = null;

            lock (_gate)
            {
                replaced = _activeToast;
                _activeToast = request;
                _callbacks.Register<ToastDismissReason>(
                    request.RequestId,
                    reason => request.Callback?.Invoke(reason));

                if (replaced != null)
                {
                    _callbacks.TryClaim(
                        replaced.RequestId,
                        ToastDismissReason.Replaced,
                        out dispatchReplaced);
                }
            }

            try
            {
                if (replaced != null)
                {
                    _strategy.DismissToast(replaced.RequestId);
                }

                _strategy.ShowToast(request.RequestId, request.Options);
            }
            catch
            {
                _callbacks.Cancel(request.RequestId);
                lock (_gate)
                {
                    if (ReferenceEquals(_activeToast, request))
                    {
                        _activeToast = null;
                    }
                }

                throw;
            }
            finally
            {
                dispatchReplaced?.Invoke();
            }

            return new ToastHandle(() => DismissToast(request.RequestId));
        }

        internal void ReceiveAlert(string requestId, AlertResult result)
        {
            lock (_gate)
            {
                if (_activeAlert == null || _activeAlert.RequestId != requestId)
                {
                    return;
                }
            }

            if (_callbacks.TryClaim(requestId, result, out Action dispatch))
            {
                dispatch();
            }
        }

        internal void ReceiveBottomSheet(string requestId, BottomSheetResult result)
        {
            if (_callbacks.TryClaim(requestId, result, out Action dispatch))
            {
                dispatch();
            }
        }

        internal void ReceiveToast(string requestId, ToastDismissReason reason)
        {
            Action dispatch;
            lock (_gate)
            {
                if (_activeToast == null || _activeToast.RequestId != requestId ||
                    !_callbacks.TryClaim(requestId, reason, out dispatch))
                {
                    return;
                }

                _activeToast = null;
            }

            dispatch();
        }

        internal void Reset()
        {
            lock (_gate)
            {
                _alertQueue.Clear();
                _activeAlert = null;
                _activeToast = null;
                _callbacks.Clear();
            }

            _strategy.Reset();
        }

        private void StartAlert(AlertRequest request)
        {
            _callbacks.Register<AlertResult>(request.RequestId, result => CompleteAlert(request, result));
            try
            {
                _strategy.ShowAlert(request.RequestId, request.Options);
            }
            catch
            {
                _callbacks.Cancel(request.RequestId);
                AlertRequest next = MoveToNextAlert(request);
                if (next != null)
                {
                    StartAlert(next);
                }

                throw;
            }
        }

        private void CompleteAlert(AlertRequest request, AlertResult result)
        {
            try
            {
                request.Callback?.Invoke(result);
            }
            finally
            {
                AlertRequest next = MoveToNextAlert(request);
                if (next != null)
                {
                    StartAlert(next);
                }
            }
        }

        private AlertRequest MoveToNextAlert(AlertRequest completed)
        {
            lock (_gate)
            {
                if (!ReferenceEquals(_activeAlert, completed))
                {
                    return null;
                }

                _activeAlert = _alertQueue.Count == 0 ? null : _alertQueue.Dequeue();
                return _activeAlert;
            }
        }

        private void DismissToast(string requestId)
        {
            Action dispatch;
            lock (_gate)
            {
                if (_activeToast == null || _activeToast.RequestId != requestId ||
                    !_callbacks.TryClaim(
                        requestId,
                        ToastDismissReason.ManuallyDismissed,
                        out dispatch))
                {
                    return;
                }

                _activeToast = null;
            }

            try
            {
                _strategy.DismissToast(requestId);
            }
            finally
            {
                dispatch();
            }
        }

        private string NextRequestId(string prefix)
        {
            long value = Interlocked.Increment(ref _nextRequestId);
            return prefix + "-" + value.ToString(CultureInfo.InvariantCulture);
        }

        private sealed class AlertRequest
        {
            internal AlertRequest(
                string requestId,
                AlertOptions options,
                Action<AlertResult> callback)
            {
                RequestId = requestId;
                Options = options;
                Callback = callback;
            }

            internal string RequestId { get; }

            internal AlertOptions Options { get; }

            internal Action<AlertResult> Callback { get; }
        }

        private sealed class ToastRequest
        {
            internal ToastRequest(
                string requestId,
                ToastOptions options,
                Action<ToastDismissReason> callback)
            {
                RequestId = requestId;
                Options = options;
                Callback = callback;
            }

            internal string RequestId { get; }

            internal ToastOptions Options { get; }

            internal Action<ToastDismissReason> Callback { get; }
        }
    }
}
