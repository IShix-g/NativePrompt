#if UNITY_IOS && !UNITY_EDITOR
using System;

namespace NativePrompt
{
    internal sealed class IOSNativePromptStrategy : INativePromptStrategy
    {
        public void ShowAlert(string requestId, AlertOptions options) => ThrowNotImplemented();

        public void ShowBottomSheet(string requestId, BottomSheetOptions options) => ThrowNotImplemented();

        public void ShowToast(string requestId, ToastOptions options) => ThrowNotImplemented();

        public void DismissToast(string requestId)
        {
        }

        public void Reset()
        {
        }

        private static void ThrowNotImplemented()
        {
            throw new NotImplementedException(
                "The iOS native UI strategy is implemented by the platform feature issues.");
        }
    }
}
#endif
