namespace NativePrompt
{
    internal interface INativePromptStrategy
    {
        void ShowAlert(string requestId, AlertOptions options);

        void DismissAlert(string requestId);

        void ShowBottomSheet(string requestId, BottomSheetOptions options);

        void DismissBottomSheet(string requestId);

        void ShowToast(string requestId, ToastOptions options);

        void DismissToast(string requestId);

        void ShowLoading(string requestId, LoadingOptions options);

        void DismissLoading(string requestId);

        void Reset();
    }
}
