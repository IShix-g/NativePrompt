using System;
using UnityEditor;
using UnityEngine;

namespace NativePrompt.Editor
{
    [InitializeOnLoad]
    internal sealed class EditorNativePromptStrategy : INativePromptStrategy
    {
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
            ThrowNotImplemented();
        }

        public void DismissToast(string requestId)
        {
        }

        public void Reset()
        {
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
