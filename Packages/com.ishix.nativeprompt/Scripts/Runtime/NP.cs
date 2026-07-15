using System;

namespace NativePrompt
{
    /// <summary>
    /// Provides access to platform-native prompts.
    /// </summary>
    public static class NP
    {
        /// <summary>
        /// Shows a native alert.
        /// </summary>
        /// <param name="options">The alert content and button configuration.</param>
        /// <param name="onCompleted">Called once after the alert is closed.</param>
        public static void ShowAlert(AlertOptions options, Action<AlertResult> onCompleted = null)
        {
            NativePromptRuntime.ShowAlert(
                NativePromptOptions.Normalize(options),
                onCompleted);
        }

        /// <summary>
        /// Shows a native bottom sheet.
        /// </summary>
        /// <param name="options">The bottom sheet content and actions.</param>
        /// <param name="onCompleted">Called once after an action is selected or the sheet is cancelled.</param>
        public static void ShowBottomSheet(
            BottomSheetOptions options,
            Action<BottomSheetResult> onCompleted = null)
        {
            NativePromptRuntime.ShowBottomSheet(
                NativePromptOptions.Normalize(options),
                onCompleted);
        }

        /// <summary>
        /// Shows a native toast.
        /// </summary>
        /// <param name="options">The toast content and behavior.</param>
        /// <param name="onDismissed">Called once after the toast is dismissed.</param>
        /// <returns>A handle that can manually dismiss the toast.</returns>
        public static ToastHandle ShowToast(
            ToastOptions options,
            Action<ToastDismissReason> onDismissed = null)
        {
            return NativePromptRuntime.ShowToast(
                NativePromptOptions.Normalize(options),
                onDismissed);
        }
    }
}
