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
            ThrowNotImplemented();
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
