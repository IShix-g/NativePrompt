using System;
using UnityEditor;

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
            string title = options.Title ?? string.Empty;
            AlertResult result;
            if (options.YesButtonText != null && options.NoButtonText != null)
            {
                bool selectedYes = EditorUtility.DisplayDialog(
                    title,
                    options.Content,
                    options.YesButtonText,
                    options.NoButtonText);
                result = selectedYes ? AlertResult.Yes : AlertResult.No;
            }
            else if (options.YesButtonText != null)
            {
                EditorUtility.DisplayDialog(title, options.Content, options.YesButtonText);
                result = AlertResult.Yes;
            }
            else if (options.NoButtonText != null)
            {
                EditorUtility.DisplayDialog(title, options.Content, options.NoButtonText);
                result = AlertResult.No;
            }
            else
            {
                EditorUtility.DisplayDialog(title, options.Content, options.CloseButtonText);
                result = AlertResult.Closed;
            }

            NativePromptCallbackReceiver.AlertCompleted(requestId, result);
        }

        public void ShowBottomSheet(string requestId, BottomSheetOptions options)
        {
            ThrowNotImplemented();
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
    }
}
