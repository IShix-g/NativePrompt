#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NativePrompt
{
    internal sealed class AndroidNativePromptStrategy : INativePromptStrategy
    {
        private const string NativeBottomSheetClassName = "com.ishix.nativeprompt.NativeBottomSheet";
        private const string NativeToastClassName = "com.ishix.nativeprompt.NativeToast";
        private const string NativeAlertClassName = "com.ishix.nativeprompt.NativeAlert";
        private const string NativeLoadingClassName = "com.ishix.nativeprompt.NativeLoading";
        private static readonly AndroidJavaClass NativeBottomSheetClass =
            new AndroidJavaClass(NativeBottomSheetClassName);
        private static readonly AndroidJavaClass NativeToastClass =
            new AndroidJavaClass(NativeToastClassName);
        private static readonly AndroidJavaClass NativeAlertClass =
            new AndroidJavaClass(NativeAlertClassName);
        private static readonly AndroidJavaClass NativeLoadingClass =
            new AndroidJavaClass(NativeLoadingClassName);
        private readonly object _gate = new object();
        private readonly Dictionary<string, BottomSheetCallbackProxy> _bottomSheetCallbacks =
            new Dictionary<string, BottomSheetCallbackProxy>(StringComparer.Ordinal);
        private readonly Dictionary<string, ToastCallbackProxy> _toastCallbacks =
            new Dictionary<string, ToastCallbackProxy>(StringComparer.Ordinal);
        private readonly Dictionary<string, AlertCallbackProxy> _alertCallbacks =
            new Dictionary<string, AlertCallbackProxy>(StringComparer.Ordinal);

        public void ShowAlert(string requestId, AlertOptions options)
        {
            var callback = new AlertCallbackProxy(this, requestId);
            lock (_gate)
            {
                _alertCallbacks.Add(requestId, callback);
            }

            try
            {
                NativeAlertClass.CallStatic(
                    "show",
                    requestId,
                    options.Title,
                    options.Content,
                    options.YesButtonText,
                    options.NoButtonText,
                    options.CloseButtonText,
                    callback);
            }
            catch
            {
                RemoveAlertCallback(requestId, callback);
                throw;
            }
        }

        public void DismissAlert(string requestId)
        {
            lock (_gate)
            {
                _alertCallbacks.Remove(requestId);
            }

            NativeAlertClass.CallStatic("dismiss", requestId);
        }

        public void ShowBottomSheet(string requestId, BottomSheetOptions options)
        {
            var callback = new BottomSheetCallbackProxy(this, requestId);
            lock (_gate)
            {
                _bottomSheetCallbacks.Add(requestId, callback);
            }

            try
            {
                NativeBottomSheetClass.CallStatic(
                    "show",
                    requestId,
                    NativeBottomSheetPayload.ToJson(options),
                    callback);
            }
            catch
            {
                RemoveCallback(requestId, callback);
                throw;
            }
        }

        public void DismissBottomSheet(string requestId)
        {
            lock (_gate)
            {
                _bottomSheetCallbacks.Remove(requestId);
            }

            NativeBottomSheetClass.CallStatic("dismiss", requestId);
        }

        public void ShowToast(string requestId, ToastOptions options)
        {
            var callback = new ToastCallbackProxy(this, requestId);
            lock (_gate)
            {
                _toastCallbacks.Add(requestId, callback);
            }

            try
            {
                NativeToastClass.CallStatic(
                    "show",
                    requestId,
                    options.Message,
                    options.Duration,
                    options.AutoDismiss,
                    options.DismissOnTap,
                    (int)options.Position,
                    callback);
            }
            catch
            {
                RemoveToastCallback(requestId, callback);
                throw;
            }
        }

        public void DismissToast(string requestId)
        {
            lock (_gate)
            {
                _toastCallbacks.Remove(requestId);
            }

            NativeToastClass.CallStatic("dismiss", requestId);
        }

        public void ShowLoading(string requestId, LoadingOptions options)
        {
            NativeLoadingClass.CallStatic(
                "show",
                requestId,
                options.Message,
                options.BlocksInteraction,
                options.ShowsBackground,
                options.BackgroundColor.r,
                options.BackgroundColor.g,
                options.BackgroundColor.b,
                options.BackgroundOpacity,
                options.SpinnerColor.r,
                options.SpinnerColor.g,
                options.SpinnerColor.b,
                options.SpinnerColor.a,
                options.MessageColor.r,
                options.MessageColor.g,
                options.MessageColor.b,
                options.MessageColor.a,
                options.MessageFontSize,
                (int)options.Position,
                (int)options.Size,
                options.ShowDelaySeconds);
        }

        public void DismissLoading(string requestId)
        {
            NativeLoadingClass.CallStatic("dismiss", requestId);
        }

        public void Reset()
        {
            lock (_gate)
            {
                _bottomSheetCallbacks.Clear();
                _toastCallbacks.Clear();
                _alertCallbacks.Clear();
            }

            NativeBottomSheetClass.CallStatic("reset");
            NativeToastClass.CallStatic("reset");
            NativeAlertClass.CallStatic("reset");
            NativeLoadingClass.CallStatic("reset");
        }

        private void CompleteAction(
            string requestId,
            string actionId,
            BottomSheetCallbackProxy callback)
        {
            if (RemoveCallback(requestId, callback))
            {
                NativePromptCallbackReceiver.BottomSheetActionSelected(requestId, actionId);
            }
        }

        private void NotifyBottomSheetOpened(
            string requestId,
            BottomSheetCallbackProxy callback)
        {
            lock (_gate)
            {
                if (!_bottomSheetCallbacks.TryGetValue(requestId, out BottomSheetCallbackProxy current) ||
                    !ReferenceEquals(current, callback))
                {
                    return;
                }
            }
            NativePromptCallbackReceiver.BottomSheetOpened(requestId);
        }

        private void CompleteCancellation(
            string requestId,
            BottomSheetCallbackProxy callback)
        {
            if (RemoveCallback(requestId, callback))
            {
                NativePromptCallbackReceiver.BottomSheetCancelled(requestId);
            }
        }

        private bool RemoveCallback(string requestId, BottomSheetCallbackProxy callback)
        {
            lock (_gate)
            {
                if (!_bottomSheetCallbacks.TryGetValue(requestId, out BottomSheetCallbackProxy current) ||
                    !ReferenceEquals(current, callback))
                {
                    return false;
                }

                _bottomSheetCallbacks.Remove(requestId);
                return true;
            }
        }

        private void CompleteToast(
            string requestId,
            int reason,
            ToastCallbackProxy callback)
        {
            if (RemoveToastCallback(requestId, callback))
            {
                NativePromptCallbackReceiver.ToastDismissed(
                    requestId,
                    (ToastDismissReason)reason);
            }
        }

        private void NotifyToastShown(string requestId, ToastCallbackProxy callback)
        {
            lock (_gate)
            {
                if (!_toastCallbacks.TryGetValue(requestId, out ToastCallbackProxy current) ||
                    !ReferenceEquals(current, callback))
                {
                    return;
                }
            }
            NativePromptCallbackReceiver.ToastShown(requestId);
        }

        private bool RemoveToastCallback(string requestId, ToastCallbackProxy callback)
        {
            lock (_gate)
            {
                if (!_toastCallbacks.TryGetValue(requestId, out ToastCallbackProxy current) ||
                    !ReferenceEquals(current, callback))
                {
                    return false;
                }

                _toastCallbacks.Remove(requestId);
                return true;
            }
        }

        private void CompleteAlert(
            string requestId,
            int result,
            AlertCallbackProxy callback)
        {
            if (RemoveAlertCallback(requestId, callback))
            {
                NativePromptCallbackReceiver.AlertCompleted(
                    requestId,
                    (AlertResult)result);
            }
        }

        private void NotifyAlertOpened(string requestId, AlertCallbackProxy callback)
        {
            lock (_gate)
            {
                if (!_alertCallbacks.TryGetValue(requestId, out AlertCallbackProxy current) ||
                    !ReferenceEquals(current, callback))
                {
                    return;
                }
            }
            NativePromptCallbackReceiver.AlertOpened(requestId);
        }

        private bool RemoveAlertCallback(string requestId, AlertCallbackProxy callback)
        {
            lock (_gate)
            {
                if (!_alertCallbacks.TryGetValue(requestId, out AlertCallbackProxy current) ||
                    !ReferenceEquals(current, callback))
                {
                    return false;
                }

                _alertCallbacks.Remove(requestId);
                return true;
            }
        }

        private sealed class BottomSheetCallbackProxy : AndroidJavaProxy
        {
            private readonly AndroidNativePromptStrategy _owner;
            private readonly string _requestId;

            internal BottomSheetCallbackProxy(
                AndroidNativePromptStrategy owner,
                string requestId)
                : base(NativeBottomSheetClassName + "$Callback")
            {
                _owner = owner;
                _requestId = requestId;
            }

            public void onActionSelected(string requestId, string actionId)
            {
                if (requestId == _requestId)
                {
                    _owner.CompleteAction(requestId, actionId, this);
                }
            }

            public void onOpened(string requestId)
            {
                if (requestId == _requestId)
                {
                    _owner.NotifyBottomSheetOpened(requestId, this);
                }
            }

            public void onCancelled(string requestId)
            {
                if (requestId == _requestId)
                {
                    _owner.CompleteCancellation(requestId, this);
                }
            }
        }

        private sealed class ToastCallbackProxy : AndroidJavaProxy
        {
            private readonly AndroidNativePromptStrategy _owner;
            private readonly string _requestId;

            internal ToastCallbackProxy(
                AndroidNativePromptStrategy owner,
                string requestId)
                : base(NativeToastClassName + "$Callback")
            {
                _owner = owner;
                _requestId = requestId;
            }

            public void onDismissed(string requestId, int reason)
            {
                if (requestId == _requestId)
                {
                    _owner.CompleteToast(requestId, reason, this);
                }
            }

            public void onShown(string requestId)
            {
                if (requestId == _requestId)
                {
                    _owner.NotifyToastShown(requestId, this);
                }
            }
        }

        private sealed class AlertCallbackProxy : AndroidJavaProxy
        {
            private readonly AndroidNativePromptStrategy _owner;
            private readonly string _requestId;

            internal AlertCallbackProxy(
                AndroidNativePromptStrategy owner,
                string requestId)
                : base(NativeAlertClassName + "$Callback")
            {
                _owner = owner;
                _requestId = requestId;
            }

            public void onCompleted(string requestId, int result)
            {
                if (requestId == _requestId)
                {
                    _owner.CompleteAlert(requestId, result, this);
                }
            }

            public void onOpened(string requestId)
            {
                if (requestId == _requestId)
                {
                    _owner.NotifyAlertOpened(requestId, this);
                }
            }
        }

        private static void ThrowNotImplemented()
        {
            throw new NotImplementedException(
                "The Android native UI strategy is implemented by the platform feature issues.");
        }
    }
}
#endif
