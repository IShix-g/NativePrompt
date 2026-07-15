using System;

namespace NativePrompt
{
    internal static class NativePromptStrategyRegistry
    {
#if UNITY_EDITOR
        private static Func<INativePromptStrategy> _editorFactory;

        internal static void RegisterEditorFactory(Func<INativePromptStrategy> factory)
        {
            _editorFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        }
#endif

        internal static INativePromptStrategy CreateForCurrentPlatform()
        {
#if UNITY_EDITOR
            if (_editorFactory == null)
            {
                throw new InvalidOperationException(
                    "The NativePrompt Editor strategy has not been registered.");
            }

            return _editorFactory();
#elif UNITY_IOS
            return new IOSNativePromptStrategy();
#elif UNITY_ANDROID
            return new AndroidNativePromptStrategy();
#else
            throw new PlatformNotSupportedException(
                "NativePrompt supports the Unity Editor, iOS, and Android only.");
#endif
        }
    }
}
