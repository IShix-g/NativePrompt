using System;
using System.Collections.Generic;
using System.Threading;
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
        private readonly List<LoadingRequest> _loadings = new List<LoadingRequest>();
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

        internal int ActiveLoadingCount
        {
            get
            {
                lock (_gate)
                {
                    return _loadings.Count;
                }
            }
        }

        internal AlertHandle ShowAlert(AlertOptions options, Action<AlertResult> onCompleted)
        {
            var request = new AlertRequest(NextRequestId("alert"), options, onCompleted);
            var lifetime = new PromptHandleLifetime(
                () => DismissAlert(request),
                () => DisposeAlert(request));
            request.Lifetime = lifetime;
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
                lifetime);
        }

        internal Awaitable<AlertResult> ShowAlertAsync(
            AlertOptions options,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return CreateCanceledAwaitable<AlertResult>();
            }

            var request = new AlertRequest(this, NextRequestId("alert"), options);
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

            request.RegisterCancellation(cancellationToken);
            if (startImmediately && !request.Cancelled)
            {
                StartAlert(request);
            }

            return request.Awaitable;
        }

        internal BottomSheetHandle ShowBottomSheet(
            BottomSheetOptions options,
            Action<BottomSheetResult> onCompleted)
        {
            var request = new BottomSheetRequest(
                NextRequestId("bottom-sheet"),
                options,
                onCompleted);
            var lifetime = new PromptHandleLifetime(
                () => DismissBottomSheet(request),
                () => DisposeBottomSheet(request));
            request.Lifetime = lifetime;
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
                ReleaseRequest(request);
                throw;
            }

            return new BottomSheetHandle(
                request.RequestId,
                request.Tag,
                request.GroupId,
                lifetime);
        }

        internal Awaitable<BottomSheetResult> ShowBottomSheetAsync(
            BottomSheetOptions options,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return CreateCanceledAwaitable<BottomSheetResult>();
            }

            var request = new BottomSheetRequest(
                this,
                NextRequestId("bottom-sheet"),
                options);
            lock (_gate)
            {
                _bottomSheets.Add(request.RequestId, request);
            }

            request.RegisterCancellation(cancellationToken);
            if (!request.Cancelled)
            {
                try
                {
                    request.PlatformStarted = true;
                    _strategy.ShowBottomSheet(request.RequestId, options);
                }
                catch (Exception exception)
                {
                    lock (_gate)
                    {
                        request.Cancelled = true;
                        _bottomSheets.Remove(request.RequestId);
                    }
                    FailRequestCompletion(request, exception);
                }
            }

            return request.Awaitable;
        }

        internal ToastHandle ShowToast(
            ToastOptions options,
            Action<ToastDismissReason> onDismissed)
        {
            var request = new ToastRequest(NextRequestId("toast"), options, onDismissed);
            var lifetime = new PromptHandleLifetime(
                () => DismissToast(request),
                () => DisposeToast(request));
            request.Lifetime = lifetime;
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
                ReleaseRequest(request);
                throw;
            }

            return new ToastHandle(
                request.RequestId,
                request.Tag,
                request.GroupId,
                lifetime);
        }

        internal Awaitable<ToastDismissReason> ShowToastAsync(
            ToastOptions options,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return CreateCanceledAwaitable<ToastDismissReason>();
            }

            var request = new ToastRequest(this, NextRequestId("toast"), options);
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

            request.RegisterCancellation(cancellationToken);
            if (!request.Cancelled)
            {
                try
                {
                    request.PlatformStarted = true;
                    _strategy.ShowToast(request.RequestId, request.Options);
                }
                catch (Exception exception)
                {
                    lock (_gate)
                    {
                        request.Cancelled = true;
                        if (ReferenceEquals(_activeToast, request))
                        {
                            _activeToast = null;
                        }
                    }
                    FailRequestCompletion(request, exception);
                }
            }

            return request.Awaitable;
        }

        internal LoadingHandle ShowLoading(LoadingOptions options)
        {
            var request = new LoadingRequest(NextRequestId("loading"), options);
            var lifetime = new PromptHandleLifetime(
                () => EndLoading(request, LoadingEndReason.Dismissed),
                () => EndLoading(request, LoadingEndReason.Disposed),
                () => EndLoading(request, LoadingEndReason.Cancelled));
            request.Lifetime = lifetime;
            int activeCount;
            lock (_gate)
            {
                _loadings.Add(request);
                activeCount = _loadings.Count;
            }

            try
            {
                _strategy.ShowLoading(request.RequestId, request.Options);
            }
            catch
            {
                LoadingRequest replacement;
                lock (_gate)
                {
                    _loadings.Remove(request);
                    request.Cancelled = true;
                    replacement = _loadings.Count == 0
                        ? null
                        : _loadings[_loadings.Count - 1];
                }
                ReleaseRequest(request);
                if (replacement == null)
                {
                    TryPlatformAction(() => _strategy.DismissLoading(request.RequestId));
                }
                else
                {
                    TryPlatformAction(() =>
                        _strategy.ShowLoading(replacement.RequestId, replacement.Options));
                }
                throw;
            }

            PostLoadingStarted(request, activeCount);

            return new LoadingHandle(
                request.RequestId,
                request.Tag,
                request.GroupId,
                lifetime);
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
            var requests = new HashSet<PromptRequest>();
            var loadingRequests = new List<LoadingRequest>();
            lock (_gate)
            {
                if (_activeAlert != null)
                {
                    _activeAlert.Cancelled = true;
                    requests.Add(_activeAlert);
                }
                foreach (AlertRequest request in _alertQueue)
                {
                    request.Cancelled = true;
                    requests.Add(request);
                }
                foreach (BottomSheetRequest request in _bottomSheets.Values)
                {
                    request.Cancelled = true;
                    requests.Add(request);
                }
                if (_activeToast != null)
                {
                    _activeToast.Cancelled = true;
                    requests.Add(_activeToast);
                }
                foreach (LoadingRequest request in _loadings)
                {
                    request.Cancelled = true;
                    requests.Add(request);
                    loadingRequests.Add(request);
                }
                foreach (PromptRequest request in _pendingDeliveries)
                {
                    request.Cancelled = true;
                    requests.Add(request);
                }

                _alertQueue.Clear();
                _activeAlert = null;
                _bottomSheets.Clear();
                _activeToast = null;
                _loadings.Clear();
                _pendingDeliveries.Clear();
            }

            foreach (PromptRequest request in requests)
            {
                CancelRequestCompletion(request, fromCancellationToken: false);
            }
            for (int i = 0; i < loadingRequests.Count; i++)
            {
                PostLoadingEnded(
                    loadingRequests[i],
                    LoadingEndReason.Reset,
                    0,
                    i == loadingRequests.Count - 1);
            }
            _strategy.Reset();
        }

        private void StartAlert(AlertRequest request)
        {
            lock (_gate)
            {
                if (request.Cancelled || !ReferenceEquals(_activeAlert, request))
                {
                    return;
                }
                request.PlatformStarted = true;
            }

            try
            {
                _strategy.ShowAlert(request.RequestId, request.Options);
            }
            catch (Exception exception)
            {
                AlertRequest next;
                lock (_gate)
                {
                    request.Cancelled = true;
                    next = MoveToNextAlert(request);
                }
                if (request.IsAsync)
                {
                    FailRequestCompletion(request, exception);
                    if (next != null)
                    {
                        TryPlatformAction(() => StartAlert(next));
                    }
                    return;
                }

                ReleaseRequest(request);
                if (next != null)
                {
                    StartAlert(next);
                }
                throw;
            }
        }

        private void CancelAsyncRequest(PromptRequest request)
        {
            bool found;
            bool dismissPlatform = false;
            AlertRequest nextAlert = null;
            lock (_gate)
            {
                if (!request.IsAsync || request.Cancelled)
                {
                    return;
                }

                found = _pendingDeliveries.Remove(request);
                if (request is AlertRequest alert)
                {
                    if (ReferenceEquals(_activeAlert, alert))
                    {
                        found = true;
                        dismissPlatform = alert.PlatformStarted && !alert.Completed;
                        nextAlert = MoveToNextAlert(alert);
                    }
                    else if (RemoveQueuedAlert(alert))
                    {
                        found = true;
                    }
                }
                else if (request is BottomSheetRequest bottomSheet)
                {
                    bool registered = _bottomSheets.TryGetValue(
                        bottomSheet.RequestId,
                        out BottomSheetRequest current) &&
                        ReferenceEquals(current, bottomSheet);
                    found |= registered;
                    if (registered)
                    {
                        _bottomSheets.Remove(bottomSheet.RequestId);
                        dismissPlatform = bottomSheet.PlatformStarted && !bottomSheet.Completed;
                    }
                }
                else if (request is ToastRequest toast)
                {
                    bool active = ReferenceEquals(_activeToast, toast);
                    found |= active;
                    if (active)
                    {
                        _activeToast = null;
                        dismissPlatform = toast.PlatformStarted && !toast.Completed;
                    }
                }

                if (!found)
                {
                    return;
                }
                request.Cancelled = true;
            }

            CancelRequestCompletion(
                request,
                fromCancellationToken: true,
                dispatch: false);
            if (dismissPlatform)
            {
                if (request is AlertRequest)
                {
                    TryPlatformAction(() => _strategy.DismissAlert(request.RequestId));
                }
                else if (request is BottomSheetRequest)
                {
                    TryPlatformAction(() => _strategy.DismissBottomSheet(request.RequestId));
                }
                else
                {
                    TryPlatformAction(() => _strategy.DismissToast(request.RequestId));
                }
            }
            if (nextAlert != null)
            {
                TryPlatformAction(() => StartAlert(nextAlert));
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
                if (isActive)
                {
                    request.PlatformDismissRequested = true;
                }
            }

            if (isActive)
            {
                try
                {
                    _strategy.DismissAlert(request.RequestId);
                }
                finally
                {
                    AlertRequest next = null;
                    bool cancelled;
                    lock (_gate)
                    {
                        request.PlatformDismissCompleted = true;
                        cancelled = request.Cancelled;
                        if (cancelled)
                        {
                            next = MoveToNextAlert(request);
                        }
                    }

                    if (cancelled)
                    {
                        if (next != null)
                        {
                            StartAlert(next);
                        }
                    }
                    else
                    {
                        PostAlertCompletion(request, AlertResult.Dismissed);
                    }
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

        private void DisposeAlert(AlertRequest request)
        {
            bool dismissPlatform = false;
            bool found;
            AlertRequest next = null;
            lock (_gate)
            {
                if (request.Cancelled)
                {
                    return;
                }

                found = _pendingDeliveries.Remove(request);
                if (ReferenceEquals(_activeAlert, request))
                {
                    found = true;
                    dismissPlatform = !request.Completed;
                    if (!request.PlatformDismissRequested ||
                        request.PlatformDismissCompleted)
                    {
                        next = MoveToNextAlert(request);
                    }
                }
                else if (RemoveQueuedAlert(request))
                {
                    found = true;
                }

                if (!found)
                {
                    return;
                }
                request.Cancelled = true;
            }

            ReleaseRequest(request);
            if (dismissPlatform)
            {
                TryPlatformAction(() => _strategy.DismissAlert(request.RequestId));
            }
            if (next != null)
            {
                TryPlatformAction(() => StartAlert(next));
            }
        }

        private void DisposeBottomSheet(BottomSheetRequest request)
        {
            bool registered;
            bool found;
            lock (_gate)
            {
                if (request.Cancelled)
                {
                    return;
                }

                registered = _bottomSheets.TryGetValue(
                    request.RequestId,
                    out BottomSheetRequest current) && ReferenceEquals(current, request);
                found = registered | _pendingDeliveries.Remove(request);
                if (!found)
                {
                    return;
                }

                request.Cancelled = true;
                if (registered)
                {
                    _bottomSheets.Remove(request.RequestId);
                }
            }

            ReleaseRequest(request);
            if (registered && !request.Completed)
            {
                TryPlatformAction(() => _strategy.DismissBottomSheet(request.RequestId));
            }
        }

        private void DisposeToast(ToastRequest request)
        {
            bool active;
            bool found;
            lock (_gate)
            {
                if (request.Cancelled)
                {
                    return;
                }

                active = ReferenceEquals(_activeToast, request);
                found = active | _pendingDeliveries.Remove(request);
                if (!found)
                {
                    return;
                }

                request.Cancelled = true;
                if (active)
                {
                    _activeToast = null;
                }
            }

            ReleaseRequest(request);
            if (active && !request.Completed)
            {
                TryPlatformAction(() => _strategy.DismissToast(request.RequestId));
            }
        }

        private void EndLoading(LoadingRequest request, LoadingEndReason reason)
        {
            LoadingRequest replacement = null;
            bool wasActive;
            int activeCount;
            lock (_gate)
            {
                int index = _loadings.IndexOf(request);
                if (index < 0 || request.Cancelled || request.Completed)
                {
                    return;
                }

                wasActive = index == _loadings.Count - 1;
                _loadings.RemoveAt(index);
                request.Completed = true;
                request.Cancelled = true;
                activeCount = _loadings.Count;
                if (wasActive && _loadings.Count > 0)
                {
                    replacement = _loadings[_loadings.Count - 1];
                }
            }

            ReleaseRequest(request);
            if (wasActive)
            {
                if (replacement == null)
                {
                    TryPlatformAction(() => _strategy.DismissLoading(request.RequestId));
                }
                else
                {
                    TryPlatformAction(() =>
                        _strategy.ShowLoading(replacement.RequestId, replacement.Options));
                }
            }

            PostLoadingEnded(request, reason, activeCount, activeCount == 0);
        }

        private void PostLoadingStarted(LoadingRequest request, int activeCount)
        {
            var args = new LoadingStartedEventArgs(
                request.RequestId,
                request.Tag,
                request.GroupId,
                activeCount);
            _dispatcher.Post(() =>
            {
                NP.RaiseLoadingStarted(args);
                if (activeCount == 1)
                {
                    NP.RaiseLoadingStateChanged(true);
                }
            });
        }

        private void PostLoadingEnded(
            LoadingRequest request,
            LoadingEndReason reason,
            int activeCount,
            bool loadingStateChanged)
        {
            var args = new LoadingEndedEventArgs(
                request.RequestId,
                request.Tag,
                request.GroupId,
                activeCount,
                reason);
            _dispatcher.Post(() =>
            {
                NP.RaiseLoadingEnded(args);
                if (loadingStateChanged)
                {
                    NP.RaiseLoadingStateChanged(false);
                }
            });
        }

        private void PostAlertCompletion(AlertRequest request, AlertResult result)
        {
            PostDelivery(request, () =>
            {
                SafeInvoke(request.Callback, result);
                request.TrySetAsyncResult(result);
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
                request.TrySetAsyncResult(result);
                NP.RaiseBottomSheetCompleted(new BottomSheetCompletedEventArgs(
                    request.RequestId, request.Tag, request.GroupId, result));
            });
        }

        private void PostToastCompletion(ToastRequest request, ToastDismissReason reason)
        {
            PostDelivery(request, () =>
            {
                SafeInvoke(request.Callback, reason);
                request.TrySetAsyncResult(reason);
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

                if (!request.TryBeginDelivery())
                {
                    request.ReleaseContent();
                    return;
                }
                try
                {
                    deliver();
                }
                finally
                {
                    request.ReleaseContent();
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

        private static void TryPlatformAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void ReleaseRequest(PromptRequest request)
        {
            request.AbandonCompletion();
            request.ReleaseContent();
        }

        private void CancelRequestCompletion(
            PromptRequest request,
            bool fromCancellationToken,
            bool dispatch = true)
        {
            if (!request.IsAsync)
            {
                ReleaseRequest(request);
                return;
            }

            request.ReleaseContent();
            if (dispatch)
            {
                _dispatcher.Post(() => request.TrySetAsyncCanceled(fromCancellationToken));
            }
            else
            {
                request.TrySetAsyncCanceled(fromCancellationToken);
            }
        }

        private void PostAsyncCancellation(PromptRequest request)
        {
            _dispatcher.Post(() => CancelAsyncRequest(request));
        }

        private void FailRequestCompletion(PromptRequest request, Exception exception)
        {
            if (!request.IsAsync)
            {
                ReleaseRequest(request);
                return;
            }

            request.ReleaseContent();
            _dispatcher.Post(() => request.TrySetAsyncException(
                exception ?? new InvalidOperationException(
                    "The native prompt failed to start.")));
        }

        private Awaitable<T> CreateCanceledAwaitable<T>()
        {
            var completionSource = new AwaitableCompletionSource<T>();
            Awaitable<T> awaitable = completionSource.Awaitable;
            _dispatcher.Post(() => completionSource.TrySetCanceled());
            return awaitable;
        }

        private abstract class PromptRequest
        {
            private NativePromptCoordinator _owner;
            private CancellationTokenRegistration _registration;
            private int _hasRegistration;
            private int _asyncCompletionState;

            protected PromptRequest(string requestId, string tag, string groupId)
            {
                RequestId = requestId;
                Tag = tag;
                GroupId = groupId;
            }

            protected PromptRequest(
                NativePromptCoordinator owner,
                string requestId,
                string tag,
                string groupId)
                : this(requestId, tag, groupId)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            internal string RequestId { get; }
            internal string Tag { get; }
            internal string GroupId { get; }
            internal bool Opened { get; set; }
            internal bool Completed { get; set; }
            internal bool Cancelled { get; set; }
            internal bool PlatformStarted { get; set; }
            internal PromptHandleLifetime Lifetime { get; set; }
            internal bool IsAsync => _owner != null;

            internal void RegisterCancellation(CancellationToken cancellationToken)
            {
                if (!cancellationToken.CanBeCanceled)
                {
                    return;
                }

                CancellationTokenRegistration registration = cancellationToken.Register(
                    AsyncCancellationCallback.Callback,
                    this,
                    useSynchronizationContext: false);
                if (Volatile.Read(ref _asyncCompletionState) != 0)
                {
                    registration.Dispose();
                    return;
                }

                _registration = registration;
                Volatile.Write(ref _hasRegistration, 1);
                if (Volatile.Read(ref _asyncCompletionState) != 0 &&
                    Interlocked.Exchange(ref _hasRegistration, 0) != 0)
                {
                    registration.Dispose();
                }
            }

            internal bool TryBeginDelivery()
            {
                return IsAsync || TryCompleteLifetime();
            }

            internal bool TryCompleteLifetime()
            {
                PromptHandleLifetime lifetime = Lifetime;
                Lifetime = null;
                return lifetime != null && lifetime.TryComplete();
            }

            internal void AbandonCompletion()
            {
                if (!IsAsync)
                {
                    TryCompleteLifetime();
                    return;
                }

                TryPrepareAsyncCompletion(disposeRegistration: true);
            }

            internal void TrySetAsyncCanceled(bool fromCancellationToken)
            {
                if (TryPrepareAsyncCompletion(
                    disposeRegistration: !fromCancellationToken))
                {
                    TrySetAsyncCanceledCore();
                }
            }

            internal void TrySetAsyncException(Exception exception)
            {
                if (TryPrepareAsyncCompletion(disposeRegistration: true))
                {
                    TrySetAsyncExceptionCore(exception);
                }
            }

            protected bool TryPrepareAsyncResult()
            {
                return TryPrepareAsyncCompletion(disposeRegistration: true);
            }

            protected abstract void TrySetAsyncCanceledCore();
            protected abstract void TrySetAsyncExceptionCore(Exception exception);

            internal abstract void ReleaseContent();

            private void CancelFromToken()
            {
                _owner?.PostAsyncCancellation(this);
            }

            private bool TryPrepareAsyncCompletion(bool disposeRegistration)
            {
                if (Interlocked.Exchange(ref _asyncCompletionState, 1) != 0)
                {
                    return false;
                }

                _owner = null;
                if (Interlocked.Exchange(ref _hasRegistration, 0) != 0 &&
                    disposeRegistration)
                {
                    _registration.Dispose();
                }
                _registration = default;
                return true;
            }

            private static class AsyncCancellationCallback
            {
                internal static readonly Action<object> Callback =
                    state => ((PromptRequest)state).CancelFromToken();
            }
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

            internal AlertRequest(
                NativePromptCoordinator owner,
                string requestId,
                AlertOptions options)
                : base(owner, requestId, options.Tag, options.GroupId)
            {
                Options = options;
                CompletionSource = new AwaitableCompletionSource<AlertResult>();
            }

            internal AlertOptions Options { get; private set; }
            internal Action<AlertResult> Callback { get; private set; }
            internal AwaitableCompletionSource<AlertResult> CompletionSource { get; }
            internal Awaitable<AlertResult> Awaitable => CompletionSource.Awaitable;
            internal bool PlatformDismissRequested { get; set; }
            internal bool PlatformDismissCompleted { get; set; }

            internal void TrySetAsyncResult(AlertResult result)
            {
                if (CompletionSource != null && TryPrepareAsyncResult())
                {
                    CompletionSource.TrySetResult(result);
                }
            }

            protected override void TrySetAsyncCanceledCore()
            {
                CompletionSource.TrySetCanceled();
            }

            protected override void TrySetAsyncExceptionCore(Exception exception)
            {
                CompletionSource.TrySetException(exception);
            }

            internal override void ReleaseContent()
            {
                Options = null;
                Callback = null;
            }
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

            internal BottomSheetRequest(
                NativePromptCoordinator owner,
                string requestId,
                BottomSheetOptions options)
                : base(owner, requestId, options.Tag, options.GroupId)
            {
                CompletionSource = new AwaitableCompletionSource<BottomSheetResult>();
            }

            internal Action<BottomSheetResult> Callback { get; private set; }
            internal AwaitableCompletionSource<BottomSheetResult> CompletionSource { get; }
            internal Awaitable<BottomSheetResult> Awaitable => CompletionSource.Awaitable;

            internal void TrySetAsyncResult(BottomSheetResult result)
            {
                if (CompletionSource != null && TryPrepareAsyncResult())
                {
                    CompletionSource.TrySetResult(result);
                }
            }

            protected override void TrySetAsyncCanceledCore()
            {
                CompletionSource.TrySetCanceled();
            }

            protected override void TrySetAsyncExceptionCore(Exception exception)
            {
                CompletionSource.TrySetException(exception);
            }

            internal override void ReleaseContent()
            {
                Callback = null;
            }
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

            internal ToastRequest(
                NativePromptCoordinator owner,
                string requestId,
                ToastOptions options)
                : base(owner, requestId, options.Tag, options.GroupId)
            {
                Options = options;
                CompletionSource = new AwaitableCompletionSource<ToastDismissReason>();
            }

            internal ToastOptions Options { get; private set; }
            internal Action<ToastDismissReason> Callback { get; private set; }
            internal AwaitableCompletionSource<ToastDismissReason> CompletionSource { get; }
            internal Awaitable<ToastDismissReason> Awaitable => CompletionSource.Awaitable;

            internal void TrySetAsyncResult(ToastDismissReason result)
            {
                if (CompletionSource != null && TryPrepareAsyncResult())
                {
                    CompletionSource.TrySetResult(result);
                }
            }

            protected override void TrySetAsyncCanceledCore()
            {
                CompletionSource.TrySetCanceled();
            }

            protected override void TrySetAsyncExceptionCore(Exception exception)
            {
                CompletionSource.TrySetException(exception);
            }

            internal override void ReleaseContent()
            {
                Options = null;
                Callback = null;
            }
        }

        private sealed class LoadingRequest : PromptRequest
        {
            internal LoadingRequest(string requestId, LoadingOptions options)
                : base(requestId, options.Tag, options.GroupId)
            {
                Options = options;
            }

            internal LoadingOptions Options { get; private set; }

            protected override void TrySetAsyncCanceledCore()
            {
                throw new InvalidOperationException("Loading requests do not support awaitables.");
            }

            protected override void TrySetAsyncExceptionCore(Exception exception)
            {
                throw new InvalidOperationException("Loading requests do not support awaitables.");
            }

            internal override void ReleaseContent()
            {
                Options = null;
            }
        }
    }
}
