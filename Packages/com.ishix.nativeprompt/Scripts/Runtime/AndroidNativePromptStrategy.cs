#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NativePrompt
{
    internal sealed class AndroidNativePromptStrategy : INativePromptStrategy
    {
        private const string NativeAlertClassName = "com.ishix.nativeprompt.NativeAlert";
        private readonly object _gate = new object();
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
                using (var nativeClass = new AndroidJavaClass(NativeAlertClassName))
                {
                    nativeClass.CallStatic(
                        "show",
                        requestId,
                        options.Title,
                        options.Content,
                        options.YesButtonText,
                        options.NoButtonText,
                        options.CloseButtonText,
                        callback);
                }
            }
            catch
            {
                RemoveAlertCallback(requestId, callback);
                throw;
            }
        }

        public void ShowBottomSheet(string requestId, BottomSheetOptions options) => ThrowNotImplemented();

        public void ShowToast(string requestId, ToastOptions options) => ThrowNotImplemented();

        public void DismissToast(string requestId)
        {
        }

        public void Reset()
        {
            lock (_gate)
            {
                _alertCallbacks.Clear();
            }

            using (var nativeClass = new AndroidJavaClass(NativeAlertClassName))
            {
                nativeClass.CallStatic("reset");
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
        }

        private static void ThrowNotImplemented()
        {
            throw new NotImplementedException(
                "The Android native UI strategy is implemented by the platform feature issues.");
        }
    }
}
#endif
