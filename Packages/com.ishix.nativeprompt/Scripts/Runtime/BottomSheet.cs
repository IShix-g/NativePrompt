using System;

namespace NativePrompt
{
    /// <summary>
    /// Configures a bottom sheet shown by <see cref="NP.ShowBottomSheet"/>.
    /// </summary>
    public sealed class BottomSheetOptions
    {
        /// <summary>Gets or sets the optional title.</summary>
        public string Title { get; set; }

        /// <summary>Gets or sets the optional supporting content.</summary>
        public string Content { get; set; }

        /// <summary>Gets or sets the actions to display. Between one and three are required.</summary>
        public BottomSheetAction[] Actions { get; set; } = Array.Empty<BottomSheetAction>();

        /// <summary>Gets or sets the cancel button text.</summary>
        public string CancelButtonText { get; set; } = "Cancel";

        /// <summary>Gets or sets optional caller-defined metadata describing this prompt.</summary>
        public string Tag { get; set; }

        /// <summary>Gets or sets optional caller-defined metadata grouping related prompts.</summary>
        public string GroupId { get; set; }
    }

    /// <summary>
    /// Describes an action displayed in a bottom sheet.
    /// </summary>
    public sealed class BottomSheetAction
    {
        /// <summary>Gets or sets the identifier returned when this action is selected.</summary>
        public string Id { get; set; }

        /// <summary>Gets or sets the text displayed for this action.</summary>
        public string Text { get; set; }

        /// <summary>Gets or sets the visual style of this action.</summary>
        public BottomSheetActionStyle Style { get; set; }

        /// <summary>Gets or sets whether this action can be selected.</summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Identifies the visual intent of a bottom sheet action.
    /// </summary>
    public enum BottomSheetActionStyle
    {
        /// <summary>A normal action.</summary>
        Default,

        /// <summary>An action that performs a destructive operation.</summary>
        Destructive
    }

    /// <summary>
    /// Describes how a bottom sheet was completed.
    /// </summary>
    public readonly struct BottomSheetResult
    {
        internal BottomSheetResult(string actionId, bool isCancelled)
        {
            ActionId = actionId;
            IsCancelled = isCancelled;
        }

        /// <summary>
        /// Gets the selected action identifier, or <see langword="null"/> when cancelled.
        /// </summary>
        public string ActionId { get; }

        /// <summary>Gets whether the bottom sheet was cancelled without selecting an action.</summary>
        public bool IsCancelled { get; }

        internal static BottomSheetResult ForAction(string actionId)
        {
            return new BottomSheetResult(actionId, false);
        }

        internal static BottomSheetResult Cancelled()
        {
            return new BottomSheetResult(null, true);
        }
    }

    /// <summary>Provides manual control and identity information for a bottom sheet.</summary>
    public sealed class BottomSheetHandle : IPromptHandle
    {
        private Action _dismiss;

        internal BottomSheetHandle(string requestId, string tag, string groupId, Action dismiss)
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
            System.Threading.Interlocked.Exchange(ref _dismiss, null)?.Invoke();
        }
    }

    /// <summary>Provides identity information when a bottom sheet is displayed.</summary>
    public sealed class BottomSheetOpenedEventArgs : PromptEventArgs
    {
        internal BottomSheetOpenedEventArgs(string requestId, string tag, string groupId)
            : base(requestId, tag, groupId)
        {
        }
    }

    /// <summary>Provides identity and result information when a bottom sheet completes.</summary>
    public sealed class BottomSheetCompletedEventArgs : PromptEventArgs
    {
        internal BottomSheetCompletedEventArgs(
            string requestId,
            string tag,
            string groupId,
            BottomSheetResult result)
            : base(requestId, tag, groupId)
        {
            Result = result;
        }

        /// <summary>Gets how the bottom sheet completed.</summary>
        public BottomSheetResult Result { get; }
    }
}
