#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;

namespace NativePrompt
{
    internal sealed class IOSNativePromptStrategy : INativePromptStrategy
    {
        private delegate void AlertCompletedCallback(IntPtr requestId, int result);

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

        public void ShowBottomSheet(string requestId, BottomSheetOptions options) => ThrowNotImplemented();

        public void ShowToast(string requestId, ToastOptions options) => ThrowNotImplemented();

        public void DismissToast(string requestId)
        {
        }

        public void Reset()
        {
            NativePrompt_ResetAlerts();
        }

        [AOT.MonoPInvokeCallback(typeof(AlertCompletedCallback))]
        private static void OnAlertCompleted(IntPtr requestId, int result)
        {
            NativePromptCallbackReceiver.AlertCompleted(
                Marshal.PtrToStringUTF8(requestId),
                (AlertResult)result);
        }

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
