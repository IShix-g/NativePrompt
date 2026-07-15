using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NativePrompt.Editor
{
    [InitializeOnLoad]
    internal sealed class EditorNativePromptStrategy : INativePromptStrategy
    {
        private readonly Dictionary<string, double> _toastDeadlines =
            new Dictionary<string, double>(StringComparer.Ordinal);

        static EditorNativePromptStrategy()
        {
            NativePromptStrategyRegistry.RegisterEditorFactory(
                () => new EditorNativePromptStrategy());
        }

        public void ShowAlert(string requestId, AlertOptions options)
        {
            ThrowNotImplemented();
        }

        public void ShowBottomSheet(string requestId, BottomSheetOptions options)
        {
            Debug.Log(FormatBottomSheet(options));
            NativePromptCallbackReceiver.BottomSheetCancelled(requestId);
        }

        public void ShowToast(string requestId, ToastOptions options)
        {
            Debug.Log($"NativePrompt Toast: {options.Message}");
            if (!options.AutoDismiss)
            {
                return;
            }

            _toastDeadlines[requestId] = EditorApplication.timeSinceStartup + options.Duration;
            EditorApplication.update -= UpdateToasts;
            EditorApplication.update += UpdateToasts;
        }

        public void DismissToast(string requestId)
        {
            _toastDeadlines.Remove(requestId);
            UnsubscribeWhenIdle();
        }

        public void Reset()
        {
            _toastDeadlines.Clear();
            EditorApplication.update -= UpdateToasts;
        }

        private void UpdateToasts()
        {
            double now = EditorApplication.timeSinceStartup;
            string[] requestIds = null;
            int count = 0;
            foreach (KeyValuePair<string, double> toast in _toastDeadlines)
            {
                if (toast.Value > now)
                {
                    continue;
                }

                if (requestIds == null)
                {
                    requestIds = new string[_toastDeadlines.Count];
                }

                requestIds[count++] = toast.Key;
            }

            for (int index = 0; index < count; index++)
            {
                string requestId = requestIds[index];
                if (_toastDeadlines.Remove(requestId))
                {
                    NativePromptCallbackReceiver.ToastDismissed(
                        requestId,
                        ToastDismissReason.TimedOut);
                }
            }

            UnsubscribeWhenIdle();
        }

        private void UnsubscribeWhenIdle()
        {
            if (_toastDeadlines.Count == 0)
            {
                EditorApplication.update -= UpdateToasts;
            }
        }

        private static void ThrowNotImplemented()
        {
            throw new NotImplementedException(
                "The Editor native UI strategy is implemented by the prompt feature issues.");
        }

        private static string FormatBottomSheet(BottomSheetOptions options)
        {
            var message = new System.Text.StringBuilder("NativePrompt Bottom Sheet");
            message.Append("\nTitle: ").Append(options.Title ?? "<none>");
            message.Append("\nContent: ").Append(options.Content ?? "<none>");
            message.Append("\nCancel: ").Append(options.CancelButtonText);
            message.Append("\nActions:");
            for (int index = 0; index < options.Actions.Length; index++)
            {
                BottomSheetAction action = options.Actions[index];
                message.Append("\n- ")
                    .Append(action.Id)
                    .Append(": ")
                    .Append(action.Text)
                    .Append(" [")
                    .Append(action.Style)
                    .Append(", Enabled=")
                    .Append(action.Enabled)
                    .Append(']');
            }

            return message.ToString();
        }
    }
}
