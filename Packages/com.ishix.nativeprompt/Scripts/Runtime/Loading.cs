using System;
using System.Threading;
using UnityEngine;

namespace NativePrompt
{
    /// <summary>Configures a native loading overlay shown by <see cref="NP.ShowLoading"/>.</summary>
    public sealed class LoadingOptions
    {
        /// <summary>Gets or sets whether pointer input is blocked immediately.</summary>
        public bool BlocksInteraction { get; set; }

        /// <summary>Gets or sets whether a full-screen background is shown.</summary>
        public bool ShowsBackground { get; set; }

        /// <summary>Gets or sets the background color.</summary>
        public Color BackgroundColor { get; set; } = Color.white;

        /// <summary>Gets or sets the background opacity from zero through one.</summary>
        public float BackgroundOpacity { get; set; } = 0.5f;

        /// <summary>Gets or sets the spinner and message position.</summary>
        public LoadingPosition Position { get; set; } = LoadingPosition.BottomRight;

        /// <summary>Gets or sets the spinner size.</summary>
        public LoadingSize Size { get; set; } = LoadingSize.Medium;

        /// <summary>Gets or sets the spinner color, including its opacity.</summary>
        public Color SpinnerColor { get; set; } = Color.black;

        /// <summary>Gets or sets an optional message displayed below the spinner.</summary>
        public string Message { get; set; }

        /// <summary>Gets or sets the message color, including its opacity.</summary>
        public Color MessageColor { get; set; } = new Color(0.33f, 0.33f, 0.33f, 1f);

        /// <summary>Gets or sets the message font size in pt on iOS and sp on Android.</summary>
        public float MessageFontSize { get; set; } = 17f;

        /// <summary>Gets or sets the delay before visual elements become visible.</summary>
        public float ShowDelaySeconds { get; set; } = 0.25f;

        /// <summary>Gets or sets optional caller-defined metadata describing this request.</summary>
        public string Tag { get; set; }

        /// <summary>Gets or sets optional caller-defined metadata grouping related requests.</summary>
        public string GroupId { get; set; }
    }

    /// <summary>Identifies the requested loading group position.</summary>
    public enum LoadingPosition
    {
        /// <summary>The center of the safe display area.</summary>
        Center,

        /// <summary>The top-left safe-area corner.</summary>
        TopLeft,

        /// <summary>The top-right safe-area corner.</summary>
        TopRight,

        /// <summary>The bottom-left safe-area corner.</summary>
        BottomLeft,

        /// <summary>The bottom-right safe-area corner.</summary>
        BottomRight
    }

    /// <summary>Identifies the platform-native spinner size.</summary>
    public enum LoadingSize
    {
        /// <summary>A small spinner.</summary>
        Small,

        /// <summary>A medium spinner.</summary>
        Medium,

        /// <summary>A large spinner.</summary>
        Large
    }

    /// <summary>Identifies why a loading request ended.</summary>
    public enum LoadingEndReason
    {
        /// <summary>The request was explicitly ended through <see cref="LoadingHandle.Dismiss"/>.</summary>
        Dismissed,

        /// <summary>The request was ended through <see cref="LoadingHandle.Dispose"/>.</summary>
        Disposed,

        /// <summary>The request's lifetime owner was cancelled.</summary>
        Cancelled,

        /// <summary>The NativePrompt runtime was reset.</summary>
        Reset
    }

    /// <summary>Provides information about a loading request that has started.</summary>
    public sealed class LoadingStartedEventArgs : PromptEventArgs
    {
        internal LoadingStartedEventArgs(
            string requestId,
            string tag,
            string groupId,
            int activeCount)
            : base(requestId, tag, groupId)
        {
            ActiveCount = activeCount;
        }

        /// <summary>Gets the number of active loading requests after this request started.</summary>
        public int ActiveCount { get; }
    }

    /// <summary>Provides information about a loading request that has ended.</summary>
    public sealed class LoadingEndedEventArgs : PromptEventArgs
    {
        internal LoadingEndedEventArgs(
            string requestId,
            string tag,
            string groupId,
            int activeCount,
            LoadingEndReason reason)
            : base(requestId, tag, groupId)
        {
            ActiveCount = activeCount;
            Reason = reason;
        }

        /// <summary>Gets the number of active loading requests after this request ended.</summary>
        public int ActiveCount { get; }

        /// <summary>Gets why this loading request ended.</summary>
        public LoadingEndReason Reason { get; }
    }

    /// <summary>Provides request-scoped control over a loading overlay.</summary>
    public sealed class LoadingHandle : IPromptHandle
    {
        private readonly PromptHandleLifetime _lifetime;

        internal LoadingHandle(
            string requestId,
            string tag,
            string groupId,
            PromptHandleLifetime lifetime)
        {
            RequestId = requestId;
            Tag = tag;
            GroupId = groupId;
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        }

        /// <inheritdoc />
        public string RequestId { get; }

        /// <inheritdoc />
        public string Tag { get; }

        /// <inheritdoc />
        public string GroupId { get; }

        /// <summary>Ends this loading request. Subsequent calls have no effect.</summary>
        public void Dismiss()
        {
            _lifetime.Dismiss();
        }

        /// <summary>Ends this loading request. Subsequent calls have no effect.</summary>
        public void Dispose()
        {
            _lifetime.Dispose();
        }

        internal void AddTo(CancellationToken cancellationToken) =>
            _lifetime.AddTo(cancellationToken);
    }
}
