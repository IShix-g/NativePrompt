using System;
using System.Threading;

namespace NativePrompt
{
    /// <summary>
    /// Configures a toast shown by <see cref="NP.ShowToast"/>.
    /// </summary>
    public sealed class ToastOptions
    {
        /// <summary>Gets or sets the required message.</summary>
        public string Message { get; set; }

        /// <summary>Gets or sets the display duration in seconds.</summary>
        public float Duration { get; set; } = 2.5f;

        /// <summary>Gets or sets whether the toast is dismissed after <see cref="Duration"/>.</summary>
        public bool AutoDismiss { get; set; } = true;

        /// <summary>Gets or sets whether tapping the toast dismisses it.</summary>
        public bool DismissOnTap { get; set; } = true;

        /// <summary>Gets or sets the screen position of the toast.</summary>
        public ToastPosition Position { get; set; } = ToastPosition.Bottom;
    }

    /// <summary>
    /// Identifies the requested screen position of a toast.
    /// </summary>
    public enum ToastPosition
    {
        /// <summary>The top safe-area edge.</summary>
        Top,

        /// <summary>The center of the screen.</summary>
        Center,

        /// <summary>The bottom safe-area edge.</summary>
        Bottom
    }

    /// <summary>
    /// Identifies why a toast was dismissed.
    /// </summary>
    public enum ToastDismissReason
    {
        /// <summary>The configured duration elapsed.</summary>
        TimedOut,

        /// <summary>The toast was tapped.</summary>
        Tapped,

        /// <summary><see cref="ToastHandle.Dismiss"/> was called.</summary>
        ManuallyDismissed,

        /// <summary>A newer toast replaced this toast.</summary>
        Replaced
    }

    /// <summary>
    /// Provides manual control over a displayed toast.
    /// </summary>
    public sealed class ToastHandle
    {
        private Action _dismiss;

        internal ToastHandle(Action dismiss)
        {
            _dismiss = dismiss ?? throw new ArgumentNullException(nameof(dismiss));
        }

        /// <summary>
        /// Requests dismissal of the toast. Subsequent calls have no effect.
        /// </summary>
        public void Dismiss()
        {
            Interlocked.Exchange(ref _dismiss, null)?.Invoke();
        }
    }
}
