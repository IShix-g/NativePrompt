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
        Closed
    }
}
