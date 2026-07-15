#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NativePrompt
{
    internal sealed class AndroidNativePromptStrategy : INativePromptStrategy
    {
        private const string NativeClassName = "com.ishix.nativeprompt.NativeBottomSheet";
        private readonly object _gate = new object();
        private readonly Dictionary<string, BottomSheetCallbackProxy> _bottomSheetCallbacks =
            new Dictionary<string, BottomSheetCallbackProxy>(StringComparer.Ordinal);

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

        public void ShowToast(string requestId, ToastOptions options) => ThrowNotImplemented();

        public void DismissToast(string requestId)
        {
        }

        public void Reset()
        {
            lock (_gate)
            {
                _bottomSheetCallbacks.Clear();
            }

            using (var nativeClass = new AndroidJavaClass(NativeClassName))
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

        private static void ThrowNotImplemented()
        {
            throw new NotImplementedException(
                "The Android native UI strategy is implemented by the platform feature issues.");
        }
    }
}
#endif
