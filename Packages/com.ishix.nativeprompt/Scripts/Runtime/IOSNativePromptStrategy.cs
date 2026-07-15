#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;

namespace NativePrompt
{
    internal sealed class IOSNativePromptStrategy : INativePromptStrategy
    {
        private delegate void ActionSelectedCallback(IntPtr requestId, IntPtr actionId);

        private delegate void CancelledCallback(IntPtr requestId);

        private delegate void ToastDismissedCallback(IntPtr requestId, int reason);

        private delegate void AlertCompletedCallback(IntPtr requestId, int result);

        private static readonly ActionSelectedCallback ActionSelected = OnActionSelected;
        private static readonly CancelledCallback Cancelled = OnCancelled;
        private static readonly ToastDismissedCallback ToastDismissed = OnToastDismissed;
        private static readonly AlertCompletedCallback AlertCompleted = OnAlertCompleted;

        public void ShowAlert(string requestId, AlertOptions options)
        {
            NativePrompt_ShowAlert(
                requestId,
                options.Title,
                options.Content,
                options.YesButtonText,
                options.NoButtonText,
                options.CloseButtonText,
                AlertCompleted);
        }

        public void ShowBottomSheet(string requestId, BottomSheetOptions options)
        {
            NativePrompt_ShowBottomSheet(
                requestId,
                NativeBottomSheetPayload.ToJson(options),
                ActionSelected,
                Cancelled);
        }

        public void ShowToast(string requestId, ToastOptions options)
        {
            NativePrompt_ShowToast(
                requestId,
                options.Message,
                options.Duration,
                options.AutoDismiss,
                options.DismissOnTap,
                (int)options.Position,
                ToastDismissed);
        }

        public void DismissToast(string requestId)
        {
            NativePrompt_DismissToast(requestId);
        }

        public void Reset()
        {
            NativePrompt_ResetBottomSheets();
            NativePrompt_ResetToasts();
            NativePrompt_ResetAlerts();
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

        [AOT.MonoPInvokeCallback(typeof(ToastDismissedCallback))]
        private static void OnToastDismissed(IntPtr requestId, int reason)
        {
            NativePromptCallbackReceiver.ToastDismissed(
                Marshal.PtrToStringUTF8(requestId),
                (ToastDismissReason)reason);
        }

        [AOT.MonoPInvokeCallback(typeof(AlertCompletedCallback))]
        private static void OnAlertCompleted(IntPtr requestId, int result)
        {
            NativePromptCallbackReceiver.AlertCompleted(
                Marshal.PtrToStringUTF8(requestId),
                (AlertResult)result);
        }

        [DllImport("__Internal")]
        private static extern void NativePrompt_ShowBottomSheet(
            string requestId,
            string payload,
            ActionSelectedCallback onActionSelected,
            CancelledCallback onCancelled);

        [DllImport("__Internal")]
        private static extern void NativePrompt_ResetBottomSheets();

        [DllImport("__Internal")]
        private static extern void NativePrompt_ShowToast(
            string requestId,
            string message,
            float duration,
            [MarshalAs(UnmanagedType.I1)] bool autoDismiss,
            [MarshalAs(UnmanagedType.I1)] bool dismissOnTap,
            int position,
            ToastDismissedCallback onDismissed);

        [DllImport("__Internal")]
        private static extern void NativePrompt_DismissToast(string requestId);

        [DllImport("__Internal")]
        private static extern void NativePrompt_ResetToasts();

        [DllImport("__Internal")]
        private static extern void NativePrompt_ShowAlert(
            string requestId,
            string title,
            string content,
            string yesButtonText,
            string noButtonText,
            string closeButtonText,
            AlertCompletedCallback onCompleted);

        [DllImport("__Internal")]
        private static extern void NativePrompt_ResetAlerts();

        private static void ThrowNotImplemented()
        {
            throw new NotImplementedException(
                "The iOS native UI strategy is implemented by the platform feature issues.");
        }
    }
}
#endif
