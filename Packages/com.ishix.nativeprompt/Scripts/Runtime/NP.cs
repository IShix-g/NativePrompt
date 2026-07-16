using System;
using UnityEngine;

namespace NativePrompt
{
    /// <summary>Exposes common identity and dismissal behavior for prompt handles.</summary>
    public interface IPromptHandle
    {
        /// <summary>Gets the library-generated identifier unique to this prompt request.</summary>
        string RequestId { get; }

        /// <summary>Gets the caller-defined tag captured when the prompt was requested.</summary>
        string Tag { get; }

        /// <summary>Gets the caller-defined group identifier captured when the prompt was requested.</summary>
        string GroupId { get; }

        /// <summary>Requests dismissal. Subsequent calls have no effect.</summary>
        void Dismiss();
    }

    /// <summary>Provides caller-visible identity information for a prompt lifecycle event.</summary>
    public abstract class PromptEventArgs : EventArgs
    {
        internal PromptEventArgs(string requestId, string tag, string groupId)
        {
            RequestId = requestId;
            Tag = tag;
            GroupId = groupId;
        }

        /// <summary>Gets the library-generated request identifier.</summary>
        public string RequestId { get; }

        /// <summary>Gets the caller-defined tag captured at request time.</summary>
        public string Tag { get; }

        /// <summary>Gets the caller-defined group identifier captured at request time.</summary>
        public string GroupId { get; }
    }

    /// <summary>
    /// Provides access to platform-native prompts.
    /// </summary>
    public static class NP
    {
        /// <summary>Occurs after an alert is actually displayed.</summary>
        public static event EventHandler<AlertOpenedEventArgs> AlertOpened;

        /// <summary>Occurs after an alert's individual callback has run.</summary>
        public static event EventHandler<AlertCompletedEventArgs> AlertCompleted;

        /// <summary>Occurs after a bottom sheet is actually displayed.</summary>
        public static event EventHandler<BottomSheetOpenedEventArgs> BottomSheetOpened;

        /// <summary>Occurs after a bottom sheet's individual callback has run.</summary>
        public static event EventHandler<BottomSheetCompletedEventArgs> BottomSheetCompleted;

        /// <summary>Occurs after a toast is actually displayed.</summary>
        public static event EventHandler<ToastShownEventArgs> ToastShown;

        /// <summary>Occurs after a toast's individual callback has run.</summary>
        public static event EventHandler<ToastDismissedEventArgs> ToastDismissed;

        /// <summary>
        /// Shows a native alert.
        /// </summary>
        /// <param name="options">The alert content and button configuration.</param>
        /// <param name="onCompleted">Called once after the alert is closed.</param>
        /// <returns>A handle that identifies and can dismiss this alert.</returns>
        public static AlertHandle ShowAlert(AlertOptions options, Action<AlertResult> onCompleted = null)
        {
            return NativePromptRuntime.ShowAlert(
                NativePromptOptions.Normalize(options),
                onCompleted);
        }

        /// <summary>
        /// Shows a native bottom sheet.
        /// </summary>
        /// <param name="options">The bottom sheet content and actions.</param>
        /// <param name="onCompleted">Called once after an action is selected or the sheet is cancelled.</param>
        /// <returns>A handle that identifies and can dismiss this bottom sheet.</returns>
        public static BottomSheetHandle ShowBottomSheet(
            BottomSheetOptions options,
            Action<BottomSheetResult> onCompleted = null)
        {
            return NativePromptRuntime.ShowBottomSheet(
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

        internal static void RaiseAlertOpened(AlertOpenedEventArgs args) =>
            RaiseSafely(AlertOpened, args);

        internal static void RaiseAlertCompleted(AlertCompletedEventArgs args) =>
            RaiseSafely(AlertCompleted, args);

        internal static void RaiseBottomSheetOpened(BottomSheetOpenedEventArgs args) =>
            RaiseSafely(BottomSheetOpened, args);

        internal static void RaiseBottomSheetCompleted(BottomSheetCompletedEventArgs args) =>
            RaiseSafely(BottomSheetCompleted, args);

        internal static void RaiseToastShown(ToastShownEventArgs args) =>
            RaiseSafely(ToastShown, args);

        internal static void RaiseToastDismissed(ToastDismissedEventArgs args) =>
            RaiseSafely(ToastDismissed, args);

        private static void RaiseSafely<T>(EventHandler<T> handlers, T args)
            where T : EventArgs
        {
            if (handlers == null)
            {
                return;
            }

            foreach (EventHandler<T> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(null, args);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }
    }
}
