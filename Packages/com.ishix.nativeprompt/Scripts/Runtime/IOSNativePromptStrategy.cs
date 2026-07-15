#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;

namespace NativePrompt
{
    internal sealed class IOSNativePromptStrategy : INativePromptStrategy
    {
        private delegate void ActionSelectedCallback(IntPtr requestId, IntPtr actionId);

        private delegate void CancelledCallback(IntPtr requestId);

        private static readonly ActionSelectedCallback ActionSelected = OnActionSelected;
        private static readonly CancelledCallback Cancelled = OnCancelled;

        public void ShowAlert(string requestId, AlertOptions options) => ThrowNotImplemented();

        public void ShowBottomSheet(string requestId, BottomSheetOptions options)
        {
            NativePrompt_ShowBottomSheet(
                requestId,
                NativeBottomSheetPayload.ToJson(options),
                ActionSelected,
                Cancelled);
        }

        public void ShowToast(string requestId, ToastOptions options) => ThrowNotImplemented();

        public void DismissToast(string requestId)
        {
        }

        public void Reset()
        {
            NativePrompt_ResetBottomSheets();
        }

        [AOT.MonoPInvokeCallback(typeof(ActionSelectedCallback))]
        private static void OnActionSelected(IntPtr requestId, IntPtr actionId)
        {
            NativePromptCallbackReceiver.BottomSheetActionSelected(
                Marshal.PtrToStringUTF8(requestId),
                Marshal.PtrToStringUTF8(actionId));
        }

        [AOT.MonoPInvokeCallback(typeof(CancelledCallback))]
        private static void OnCancelled(IntPtr requestId)
        {
            NativePromptCallbackReceiver.BottomSheetCancelled(
                Marshal.PtrToStringUTF8(requestId));
        }

        [DllImport("__Internal")]
        private static extern void NativePrompt_ShowBottomSheet(
            string requestId,
            string payload,
            ActionSelectedCallback onActionSelected,
            CancelledCallback onCancelled);

        [DllImport("__Internal")]
        private static extern void NativePrompt_ResetBottomSheets();

        private static void ThrowNotImplemented()
        {
            throw new NotImplementedException(
                "The iOS native UI strategy is implemented by the platform feature issues.");
        }
    }
}
#endif
