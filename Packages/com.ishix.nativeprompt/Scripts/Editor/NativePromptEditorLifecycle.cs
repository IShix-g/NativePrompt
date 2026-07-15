using UnityEditor;

namespace NativePrompt.Editor
{
    [InitializeOnLoad]
    internal static class NativePromptEditorLifecycle
    {
        static NativePromptEditorLifecycle()
        {
            AssemblyReloadEvents.beforeAssemblyReload += NativePromptRuntime.Reset;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode ||
                state == PlayModeStateChange.ExitingPlayMode)
            {
                NativePromptRuntime.Reset();
            }
        }
    }
}
