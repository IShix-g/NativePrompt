#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NativePrompt
{
    internal sealed class AndroidNativePromptStrategy : INativePromptStrategy
    {
        private const string NativeClassName = "com.ishix.nativeprompt.NativeBottomSheet";
        private const string NativeToastClassName = "com.ishix.nativeprompt.NativeToast";
        private readonly object _gate = new object();
        private readonly Dictionary<string, BottomSheetCallbackProxy> _bottomSheetCallbacks =
            new Dictionary<string, BottomSheetCallbackProxy>(StringComparer.Ordinal);
        private readonly Dictionary<string, ToastCallbackProxy> _toastCallbacks =
            new Dictionary<string, ToastCallbackProxy>(StringComparer.Ordinal);

        public void ShowAlert(string requestId, AlertOptions options) => ThrowNotImplemented();

        public void ShowBottomSheet(string requestId, BottomSheetOptions options)
        {
            var callback = new BottomSheetCallbackProxy(this, requestId);
            lock (_gate)
            {
                _bottomSheetCallbacks.Add(requestId, callback);
            }

            try
            {
                using (var nativeClass = new AndroidJavaClass(NativeClassName))
                {
                    nativeClass.CallStatic(
                        "show",
                        requestId,
                        NativeBottomSheetPayload.ToJson(options),
                        callback);
                }
            }
            catch
            {
                RemoveCallback(requestId, callback);
                throw;
            }
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
                using (var nativeClass = new AndroidJavaClass(NativeToastClassName))
                {
                    nativeClass.CallStatic(
                        "show",
                        requestId,
                        options.Message,
                        options.Duration,
                        options.AutoDismiss,
                        options.DismissOnTap,
                        (int)options.Position,
                        callback);
                }
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

            using (var nativeClass = new AndroidJavaClass(NativeToastClassName))
            {
                nativeClass.CallStatic("dismiss", requestId);
            }
        }

        public void Reset()
        {
            lock (_gate)
            {
                _bottomSheetCallbacks.Clear();
                _toastCallbacks.Clear();
            }

            using (var nativeClass = new AndroidJavaClass(NativeClassName))
            {
                nativeClass.CallStatic("reset");
            }

            using (var nativeClass = new AndroidJavaClass(NativeToastClassName))
            {
                nativeClass.CallStatic("reset");
            }
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

        private sealed class BottomSheetCallbackProxy : AndroidJavaProxy
        {
            private readonly AndroidNativePromptStrategy _owner;
            private readonly string _requestId;

            internal BottomSheetCallbackProxy(
                AndroidNativePromptStrategy owner,
                string requestId)
                : base(NativeClassName + "$Callback")
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
        }

        private static void ThrowNotImplemented()
        {
            throw new NotImplementedException(
                "The Android native UI strategy is implemented by the platform feature issues.");
        }
    }
}
#endif
