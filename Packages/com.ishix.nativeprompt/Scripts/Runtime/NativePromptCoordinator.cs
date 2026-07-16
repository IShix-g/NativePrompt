using System;
using System.Collections.Generic;
using UnityEngine;

namespace NativePrompt
{
    internal sealed class NativePromptCoordinator
    {
        private readonly object _gate = new object();
        private readonly INativePromptStrategy _strategy;
        private readonly IMainThreadDispatcher _dispatcher;
        private readonly Queue<AlertRequest> _alertQueue = new Queue<AlertRequest>();
        private readonly Dictionary<string, BottomSheetRequest> _bottomSheets =
            new Dictionary<string, BottomSheetRequest>(StringComparer.Ordinal);
        private readonly HashSet<PromptRequest> _pendingDeliveries =
            new HashSet<PromptRequest>();
        private AlertRequest _activeAlert;
        private ToastRequest _activeToast;

        internal NativePromptCoordinator(
            INativePromptStrategy strategy,
            IMainThreadDispatcher dispatcher)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        internal int PendingCallbackCount
        {
            get
            {
                lock (_gate)
                {
                    return _alertQueue.Count +
                        (_activeAlert == null ? 0 : 1) +
                        _bottomSheets.Count +
                        (_activeToast == null ? 0 : 1) +
                        _pendingDeliveries.Count;
                }
            }
        }

        internal AlertHandle ShowAlert(AlertOptions options, Action<AlertResult> onCompleted)
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

            return new AlertHandle(
                request.RequestId,
                request.Tag,
                request.GroupId,
                () => DismissAlert(request));
        }

        internal BottomSheetHandle ShowBottomSheet(
            BottomSheetOptions options,
            Action<BottomSheetResult> onCompleted)
        {
            var request = new BottomSheetRequest(
                NextRequestId("bottom-sheet"),
                options,
                onCompleted);
            lock (_gate)
            {
                _bottomSheets.Add(request.RequestId, request);
            }

            try
            {
                _strategy.ShowBottomSheet(request.RequestId, options);
            }
            catch
            {
                lock (_gate)
                {
                    request.Cancelled = true;
                    _bottomSheets.Remove(request.RequestId);
                }
                throw;
            }

            return new BottomSheetHandle(
                request.RequestId,
                request.Tag,
                request.GroupId,
                () => DismissBottomSheet(request));
        }

        internal ToastHandle ShowToast(
            ToastOptions options,
            Action<ToastDismissReason> onDismissed)
        {
            var request = new ToastRequest(NextRequestId("toast"), options, onDismissed);
            ToastRequest replaced;
            lock (_gate)
            {
                replaced = _activeToast;
                if (replaced != null)
                {
                    ClaimForDelivery(replaced);
                }
                _activeToast = request;
            }

            if (replaced != null)
            {
                try
                {
                    _strategy.DismissToast(replaced.RequestId);
                }
                finally
                {
                    PostToastCompletion(replaced, ToastDismissReason.Replaced);
                }
            }

            try
            {
                _strategy.ShowToast(request.RequestId, request.Options);
            }
            catch
            {
                lock (_gate)
                {
                    request.Cancelled = true;
                    if (ReferenceEquals(_activeToast, request))
                    {
                        _activeToast = null;
                    }
                }
                throw;
            }

            return new ToastHandle(
                request.RequestId,
                request.Tag,
                request.GroupId,
                () => DismissToast(request));
        }

        internal void ReceiveAlertOpened(string requestId)
        {
            AlertRequest request;
            lock (_gate)
            {
                request = _activeAlert;
                if (request == null || request.RequestId != requestId ||
                    request.Completed || request.Opened)
                {
                    return;
                }
                request.Opened = true;
            }

            PostOpened(request, () => NP.RaiseAlertOpened(
                new AlertOpenedEventArgs(request.RequestId, request.Tag, request.GroupId)));
        }

        internal void ReceiveAlert(string requestId, AlertResult result)
        {
            AlertRequest request;
            lock (_gate)
            {
                request = _activeAlert;
                if (request == null || request.RequestId != requestId ||
                    !ClaimForDelivery(request))
                {
                    return;
                }
            }

            PostAlertCompletion(request, result);
        }

        internal void ReceiveBottomSheetOpened(string requestId)
        {
            BottomSheetRequest request;
            lock (_gate)
            {
                if (!_bottomSheets.TryGetValue(requestId, out request) ||
                    request.Completed || request.Opened)
                {
                    return;
                }
                request.Opened = true;
            }

            PostOpened(request, () => NP.RaiseBottomSheetOpened(
                new BottomSheetOpenedEventArgs(request.RequestId, request.Tag, request.GroupId)));
        }

        internal void ReceiveBottomSheet(string requestId, BottomSheetResult result)
        {
            BottomSheetRequest request;
            lock (_gate)
            {
                if (!_bottomSheets.TryGetValue(requestId, out request) ||
                    !ClaimForDelivery(request))
                {
                    return;
                }
                _bottomSheets.Remove(requestId);
            }

            PostBottomSheetCompletion(request, result);
        }

        internal void ReceiveToastShown(string requestId)
        {
            ToastRequest request;
            lock (_gate)
            {
                request = _activeToast;
                if (request == null || request.RequestId != requestId ||
                    request.Completed || request.Opened)
                {
                    return;
                }
                request.Opened = true;
            }

            PostOpened(request, () => NP.RaiseToastShown(
                new ToastShownEventArgs(request.RequestId, request.Tag, request.GroupId)));
        }

        internal void ReceiveToast(string requestId, ToastDismissReason reason)
        {
            ToastRequest request;
            lock (_gate)
            {
                request = _activeToast;
                if (request == null || request.RequestId != requestId ||
                    !ClaimForDelivery(request))
                {
                    return;
                }
                _activeToast = null;
            }

            PostToastCompletion(request, reason);
        }

        internal void Reset()
        {
            lock (_gate)
            {
                if (_activeAlert != null)
                {
                    _activeAlert.Cancelled = true;
                }
                foreach (AlertRequest request in _alertQueue)
                {
                    request.Cancelled = true;
                }
                foreach (BottomSheetRequest request in _bottomSheets.Values)
                {
                    request.Cancelled = true;
                }
                if (_activeToast != null)
                {
                    _activeToast.Cancelled = true;
                }
                foreach (PromptRequest request in _pendingDeliveries)
                {
                    request.Cancelled = true;
                }

                _alertQueue.Clear();
                _activeAlert = null;
                _bottomSheets.Clear();
                _activeToast = null;
                _pendingDeliveries.Clear();
            }

            _strategy.Reset();
        }

        private void StartAlert(AlertRequest request)
        {
            try
            {
                _strategy.ShowAlert(request.RequestId, request.Options);
            }
            catch
            {
                AlertRequest next;
                lock (_gate)
                {
                    request.Cancelled = true;
                    next = MoveToNextAlert(request);
                }
                if (next != null)
                {
                    StartAlert(next);
                }
                throw;
            }
        }

        private void DismissAlert(AlertRequest request)
        {
            bool isActive;
            lock (_gate)
            {
                if (request.Completed || request.Cancelled)
                {
                    return;
                }

                isActive = ReferenceEquals(_activeAlert, request);
                if (!isActive && !RemoveQueuedAlert(request))
                {
                    return;
                }
                ClaimForDelivery(request);
            }

            if (isActive)
            {
                try
                {
                    _strategy.DismissAlert(request.RequestId);
                }
                finally
                {
                    PostAlertCompletion(request, AlertResult.Dismissed);
                }
            }
            else
            {
                PostAlertCompletion(request, AlertResult.Dismissed);
            }
        }

        private void DismissBottomSheet(BottomSheetRequest request)
        {
            lock (_gate)
            {
                if (!_bottomSheets.TryGetValue(request.RequestId, out BottomSheetRequest current) ||
                    !ReferenceEquals(current, request) || !ClaimForDelivery(request))
                {
                    return;
                }
                _bottomSheets.Remove(request.RequestId);
            }

            try
            {
                _strategy.DismissBottomSheet(request.RequestId);
            }
            finally
            {
                PostBottomSheetCompletion(request, BottomSheetResult.Cancelled());
            }
        }

        private void DismissToast(ToastRequest request)
        {
            lock (_gate)
            {
                if (!ReferenceEquals(_activeToast, request) || !ClaimForDelivery(request))
                {
                    return;
                }
                _activeToast = null;
            }

            try
            {
                _strategy.DismissToast(request.RequestId);
            }
            finally
            {
                PostToastCompletion(request, ToastDismissReason.ManuallyDismissed);
            }
        }

        private void PostAlertCompletion(AlertRequest request, AlertResult result)
        {
            PostDelivery(request, () =>
            {
                SafeInvoke(request.Callback, result);
                NP.RaiseAlertCompleted(new AlertCompletedEventArgs(
                    request.RequestId, request.Tag, request.GroupId, result));
            }, () =>
            {
                AlertRequest next;
                lock (_gate)
                {
                    next = MoveToNextAlert(request);
                }
                if (next != null)
                {
                    StartAlert(next);
                }
            });
        }

        private void PostBottomSheetCompletion(
            BottomSheetRequest request,
            BottomSheetResult result)
        {
            PostDelivery(request, () =>
            {
                SafeInvoke(request.Callback, result);
                NP.RaiseBottomSheetCompleted(new BottomSheetCompletedEventArgs(
                    request.RequestId, request.Tag, request.GroupId, result));
            });
        }

        private void PostToastCompletion(ToastRequest request, ToastDismissReason reason)
        {
            PostDelivery(request, () =>
            {
                SafeInvoke(request.Callback, reason);
                NP.RaiseToastDismissed(new ToastDismissedEventArgs(
                    request.RequestId, request.Tag, request.GroupId, reason));
            });
        }

        private void PostOpened(PromptRequest request, Action deliver)
        {
            _dispatcher.Post(() =>
            {
                lock (_gate)
                {
                    if (request.Cancelled)
                    {
                        return;
                    }
                }
                deliver();
            });
        }

        private void PostDelivery(PromptRequest request, Action deliver, Action after = null)
        {
            _dispatcher.Post(() =>
            {
                lock (_gate)
                {
                    if (request.Cancelled || !_pendingDeliveries.Remove(request))
                    {
                        return;
                    }
                }

                try
                {
                    deliver();
                }
                finally
                {
                    after?.Invoke();
                }
            });
        }

        private bool ClaimForDelivery(PromptRequest request)
        {
            if (request.Completed || request.Cancelled)
            {
                return false;
            }

            request.Completed = true;
            _pendingDeliveries.Add(request);
            return true;
        }

        private AlertRequest MoveToNextAlert(AlertRequest completed)
        {
            if (!ReferenceEquals(_activeAlert, completed))
            {
                return null;
            }

            _activeAlert = _alertQueue.Count == 0 ? null : _alertQueue.Dequeue();
            return _activeAlert;
        }

        private bool RemoveQueuedAlert(AlertRequest request)
        {
            bool found = false;
            int count = _alertQueue.Count;
            for (int index = 0; index < count; index++)
            {
                AlertRequest candidate = _alertQueue.Dequeue();
                if (!found && ReferenceEquals(candidate, request))
                {
                    found = true;
                }
                else
                {
                    _alertQueue.Enqueue(candidate);
                }
            }
            return found;
        }

        private static string NextRequestId(string prefix)
        {
            return prefix + "-" + Guid.NewGuid().ToString("N");
        }

        private static void SafeInvoke<T>(Action<T> callback, T value)
        {
            if (callback == null)
            {
                return;
            }

            try
            {
                callback(value);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private abstract class PromptRequest
        {
            protected PromptRequest(string requestId, string tag, string groupId)
            {
                RequestId = requestId;
                Tag = tag;
                GroupId = groupId;
            }

            internal string RequestId { get; }
            internal string Tag { get; }
            internal string GroupId { get; }
            internal bool Opened { get; set; }
            internal bool Completed { get; set; }
            internal bool Cancelled { get; set; }
        }

        private sealed class AlertRequest : PromptRequest
        {
            internal AlertRequest(
                string requestId,
                AlertOptions options,
                Action<AlertResult> callback)
                : base(requestId, options.Tag, options.GroupId)
            {
                Options = options;
                Callback = callback;
            }

            internal AlertOptions Options { get; }
            internal Action<AlertResult> Callback { get; }
        }

        private sealed class BottomSheetRequest : PromptRequest
        {
            internal BottomSheetRequest(
                string requestId,
                BottomSheetOptions options,
                Action<BottomSheetResult> callback)
                : base(requestId, options.Tag, options.GroupId)
            {
                Callback = callback;
            }

            internal Action<BottomSheetResult> Callback { get; }
        }

        private sealed class ToastRequest : PromptRequest
        {
            internal ToastRequest(
                string requestId,
                ToastOptions options,
                Action<ToastDismissReason> callback)
                : base(requestId, options.Tag, options.GroupId)
            {
                Options = options;
                Callback = callback;
            }

            internal ToastOptions Options { get; }
            internal Action<ToastDismissReason> Callback { get; }
        }
    }
}
