namespace NativePrompt
{
    internal interface INativePromptStrategy
    {
        void ShowAlert(string requestId, AlertOptions options);

        void ShowBottomSheet(string requestId, BottomSheetOptions options);

        void ShowToast(string requestId, ToastOptions options);

        void DismissToast(string requestId);

        void Reset();
    }
}
