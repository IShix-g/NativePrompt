namespace NativePrompt
{
    internal static class NativePromptCallbackReceiver
    {
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

        internal static void BottomSheetCancelled(string requestId)
        {
            NativePromptRuntime.ReceiveBottomSheet(requestId, BottomSheetResult.Cancelled());
        }

        internal static void ToastDismissed(string requestId, ToastDismissReason reason)
        {
            NativePromptRuntime.ReceiveToast(requestId, reason);
        }
    }
}
