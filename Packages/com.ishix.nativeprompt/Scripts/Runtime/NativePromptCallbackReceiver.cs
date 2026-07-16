namespace NativePrompt
{
    internal static class NativePromptCallbackReceiver
    {
        internal static void AlertOpened(string requestId)
        {
            NativePromptRuntime.ReceiveAlertOpened(requestId);
        }

        internal static void AlertCompleted(string requestId, AlertResult result)
        {
            NativePromptRuntime.ReceiveAlert(requestId, result);
        }

        internal static void BottomSheetActionSelected(string requestId, string actionId)
        {
            NativePromptRuntime.ReceiveBottomSheet(
                requestId,
                BottomSheetResult.ForAction(actionId));
        }

        internal static void BottomSheetOpened(string requestId)
        {
            NativePromptRuntime.ReceiveBottomSheetOpened(requestId);
        }

        internal static void BottomSheetCancelled(string requestId)
        {
            NativePromptRuntime.ReceiveBottomSheet(requestId, BottomSheetResult.Cancelled());
        }

        internal static void ToastDismissed(string requestId, ToastDismissReason reason)
        {
            NativePromptRuntime.ReceiveToast(requestId, reason);
        }

        internal static void ToastShown(string requestId)
        {
            NativePromptRuntime.ReceiveToastShown(requestId);
        }
    }
}
