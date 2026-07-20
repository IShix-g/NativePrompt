#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine.iOS;

namespace NativePrompt
{
    internal sealed class IOSNativePromptStrategy : INativePromptStrategy
    {
        private delegate void ActionSelectedCallback(IntPtr requestId, IntPtr actionId);

        private delegate void CancelledCallback(IntPtr requestId);

        private delegate void ToastDismissedCallback(IntPtr requestId, int reason);

        private delegate void AlertCompletedCallback(IntPtr requestId, int result);

        private delegate void OpenedCallback(IntPtr requestId);

        private static readonly ActionSelectedCallback ActionSelected = OnActionSelected;
        private static readonly CancelledCallback Cancelled = OnCancelled;
        private static readonly ToastDismissedCallback ToastDismissed = OnToastDismissed;
        private static readonly AlertCompletedCallback AlertCompleted = OnAlertCompleted;
        private static readonly OpenedCallback AlertOpened = OnAlertOpened;
        private static readonly OpenedCallback BottomSheetOpened = OnBottomSheetOpened;
        private static readonly OpenedCallback ToastShown = OnToastShown;

        public void ShowAlert(string requestId, AlertOptions options)
        {
            NativePrompt_ShowAlert(
                requestId,
                options.Title,
                options.Content,
                options.YesButtonText,
                options.NoButtonText,
                options.CloseButtonText,
                AlertOpened,
                AlertCompleted);
        }

        public void DismissAlert(string requestId)
        {
            NativePrompt_DismissAlert(requestId);
        }

        public void ShowBottomSheet(string requestId, BottomSheetOptions options)
        {
            NativePrompt_ShowBottomSheet(
                requestId,
                NativeBottomSheetPayload.ToJson(options),
                BottomSheetOpened,
                ActionSelected,
                Cancelled);
        }

        public void DismissBottomSheet(string requestId)
        {
            NativePrompt_DismissBottomSheet(requestId);
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
                ToastShown,
                ToastDismissed);
        }

        public void DismissToast(string requestId)
        {
            NativePrompt_DismissToast(requestId);
        }

        public void ShowLoading(string requestId, LoadingOptions options)
        {
            NativePrompt_ShowLoading(
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
            NativePrompt_DismissLoading(requestId);
        }

        public void RequestReview()
        {
            Device.RequestStoreReview();
        }

        public void Reset()
        {
            NativePrompt_ResetBottomSheets();
            NativePrompt_ResetToasts();
            NativePrompt_ResetAlerts();
            NativePrompt_ResetLoading();
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

        [AOT.MonoPInvokeCallback(typeof(OpenedCallback))]
        private static void OnAlertOpened(IntPtr requestId)
        {
            NativePromptCallbackReceiver.AlertOpened(Marshal.PtrToStringUTF8(requestId));
        }

        [AOT.MonoPInvokeCallback(typeof(OpenedCallback))]
        private static void OnBottomSheetOpened(IntPtr requestId)
        {
            NativePromptCallbackReceiver.BottomSheetOpened(Marshal.PtrToStringUTF8(requestId));
        }

        [AOT.MonoPInvokeCallback(typeof(OpenedCallback))]
        private static void OnToastShown(IntPtr requestId)
        {
            NativePromptCallbackReceiver.ToastShown(Marshal.PtrToStringUTF8(requestId));
        }

        [DllImport("__Internal")]
        private static extern void NativePrompt_ShowBottomSheet(
            string requestId,
            string payload,
            OpenedCallback onOpened,
            ActionSelectedCallback onActionSelected,
            CancelledCallback onCancelled);

        [DllImport("__Internal")]
        private static extern void NativePrompt_DismissBottomSheet(string requestId);

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
            OpenedCallback onShown,
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
            OpenedCallback onOpened,
            AlertCompletedCallback onCompleted);

        [DllImport("__Internal")]
        private static extern void NativePrompt_DismissAlert(string requestId);

        [DllImport("__Internal")]
        private static extern void NativePrompt_ResetAlerts();

        [DllImport("__Internal")]
        private static extern void NativePrompt_ShowLoading(
            string requestId,
            string message,
            [MarshalAs(UnmanagedType.I1)] bool blocksInteraction,
            [MarshalAs(UnmanagedType.I1)] bool showsBackground,
            float backgroundRed,
            float backgroundGreen,
            float backgroundBlue,
            float backgroundOpacity,
            float spinnerRed,
            float spinnerGreen,
            float spinnerBlue,
            float spinnerAlpha,
            float messageRed,
            float messageGreen,
            float messageBlue,
            float messageAlpha,
            float messageFontSize,
            int position,
            int size,
            float showDelaySeconds);

        [DllImport("__Internal")]
        private static extern void NativePrompt_DismissLoading(string requestId);

        [DllImport("__Internal")]
        private static extern void NativePrompt_ResetLoading();

        private static void ThrowNotImplemented()
        {
            throw new NotImplementedException(
                "The iOS native UI strategy is implemented by the platform feature issues.");
        }
    }
}
#endif
