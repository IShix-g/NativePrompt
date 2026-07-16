using System;
using System.Threading;

namespace NativePrompt
{
    /// <summary>
    /// Configures an alert shown by <see cref="NP.ShowAlert"/>.
    /// </summary>
    public sealed class AlertOptions
    {
        /// <summary>
        /// Gets or sets the optional alert title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the required alert message.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the optional affirmative button text.
        /// </summary>
        public string YesButtonText { get; set; }

        /// <summary>
        /// Gets or sets the optional negative button text.
        /// </summary>
        public string NoButtonText { get; set; }

        /// <summary>
        /// Gets or sets the fallback button text used when neither Yes nor No is specified.
        /// </summary>
        public string CloseButtonText { get; set; } = "Close";

        /// <summary>Gets or sets optional caller-defined metadata describing this prompt.</summary>
        public string Tag { get; set; }

        /// <summary>Gets or sets optional caller-defined metadata grouping related prompts.</summary>
        public string GroupId { get; set; }
    }

    /// <summary>
    /// Identifies how an alert was completed.
    /// </summary>
    public enum AlertResult
    {
        /// <summary>The affirmative button was selected.</summary>
        Yes,

        /// <summary>The negative button was selected.</summary>
        No,

        /// <summary>The alert was closed with its fallback close button.</summary>
        Closed,

        /// <summary><see cref="AlertHandle.Dismiss"/> was called.</summary>
        Dismissed
    }

    /// <summary>Provides manual control and identity information for an alert.</summary>
    public sealed class AlertHandle : IPromptHandle
    {
        private Action _dismiss;

        internal AlertHandle(string requestId, string tag, string groupId, Action dismiss)
        {
            RequestId = requestId;
            Tag = tag;
            GroupId = groupId;
            _dismiss = dismiss ?? throw new ArgumentNullException(nameof(dismiss));
        }

        /// <inheritdoc />
        public string RequestId { get; }

        /// <inheritdoc />
        public string Tag { get; }

        /// <inheritdoc />
        public string GroupId { get; }

        /// <inheritdoc />
        public void Dismiss()
        {
            Interlocked.Exchange(ref _dismiss, null)?.Invoke();
        }
    }

    /// <summary>Provides identity information when an alert is displayed.</summary>
    public sealed class AlertOpenedEventArgs : PromptEventArgs
    {
        internal AlertOpenedEventArgs(string requestId, string tag, string groupId)
            : base(requestId, tag, groupId)
        {
        }
    }

    /// <summary>Provides identity and result information when an alert completes.</summary>
    public sealed class AlertCompletedEventArgs : PromptEventArgs
    {
        internal AlertCompletedEventArgs(
            string requestId,
            string tag,
            string groupId,
            AlertResult result)
            : base(requestId, tag, groupId)
        {
            Result = result;
        }

        /// <summary>Gets how the alert completed.</summary>
        public AlertResult Result { get; }
    }
}
