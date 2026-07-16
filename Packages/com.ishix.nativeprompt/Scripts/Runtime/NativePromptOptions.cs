using System;
using System.Collections.Generic;

namespace NativePrompt
{
    internal static class NativePromptOptions
    {
        internal static AlertOptions Normalize(AlertOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            string yesButtonText = OptionalText(options.YesButtonText);
            string noButtonText = OptionalText(options.NoButtonText);

            return new AlertOptions
            {
                Title = OptionalText(options.Title),
                Content = RequiredText(options.Content, nameof(options.Content)),
                YesButtonText = yesButtonText,
                NoButtonText = noButtonText,
                CloseButtonText = yesButtonText == null && noButtonText == null
                    ? OptionalText(options.CloseButtonText) ?? "Close"
                    : OptionalText(options.CloseButtonText),
                Tag = options.Tag,
                GroupId = options.GroupId
            };
        }

        internal static BottomSheetOptions Normalize(BottomSheetOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            BottomSheetAction[] sourceActions = options.Actions;
            if (sourceActions == null || sourceActions.Length < 1 || sourceActions.Length > 3)
            {
                throw new ArgumentException(
                    "Bottom sheet options must contain between one and three actions.",
                    nameof(options.Actions));
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var actions = new BottomSheetAction[sourceActions.Length];
            for (int index = 0; index < sourceActions.Length; index++)
            {
                BottomSheetAction sourceAction = sourceActions[index];
                if (sourceAction == null)
                {
                    throw new ArgumentException(
                        "Bottom sheet actions cannot contain null.",
                        nameof(options.Actions));
                }

                string id = RequiredText(sourceAction.Id, nameof(sourceAction.Id));
                if (!ids.Add(id))
                {
                    throw new ArgumentException(
                        $"Bottom sheet action IDs must be unique. Duplicate ID: {id}",
                        nameof(options.Actions));
                }

                actions[index] = new BottomSheetAction
                {
                    Id = id,
                    Text = RequiredText(sourceAction.Text, nameof(sourceAction.Text)),
                    Style = sourceAction.Style,
                    Enabled = sourceAction.Enabled
                };
            }

            return new BottomSheetOptions
            {
                Title = OptionalText(options.Title),
                Content = OptionalText(options.Content),
                Actions = actions,
                CancelButtonText = OptionalText(options.CancelButtonText) ?? "Cancel",
                Tag = options.Tag,
                GroupId = options.GroupId
            };
        }

        internal static ToastOptions Normalize(ToastOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.AutoDismiss &&
                (options.Duration <= 0f || float.IsNaN(options.Duration) || float.IsInfinity(options.Duration)))
            {
                throw new ArgumentException(
                    "Toast duration must be finite and greater than zero when auto-dismiss is enabled.",
                    nameof(options.Duration));
            }

            return new ToastOptions
            {
                Message = RequiredText(options.Message, nameof(options.Message)),
                Duration = options.Duration,
                AutoDismiss = options.AutoDismiss,
                DismissOnTap = options.DismissOnTap,
                Position = options.Position,
                Tag = options.Tag,
                GroupId = options.GroupId
            };
        }

        internal static LoadingOptions Normalize(LoadingOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (float.IsNaN(options.BackgroundOpacity) ||
                float.IsInfinity(options.BackgroundOpacity) ||
                options.BackgroundOpacity < 0f || options.BackgroundOpacity > 1f)
            {
                throw new ArgumentException(
                    "Loading background opacity must be finite and between zero and one.",
                    nameof(options.BackgroundOpacity));
            }
            if (float.IsNaN(options.ShowDelaySeconds) ||
                float.IsInfinity(options.ShowDelaySeconds) ||
                options.ShowDelaySeconds < 0f)
            {
                throw new ArgumentException(
                    "Loading show delay must be finite and zero or greater.",
                    nameof(options.ShowDelaySeconds));
            }
            if (float.IsNaN(options.MessageFontSize) ||
                float.IsInfinity(options.MessageFontSize) ||
                options.MessageFontSize <= 0f)
            {
                throw new ArgumentException(
                    "Loading message font size must be finite and greater than zero.",
                    nameof(options.MessageFontSize));
            }
            if (!Enum.IsDefined(typeof(LoadingPosition), options.Position))
            {
                throw new ArgumentOutOfRangeException(nameof(options.Position));
            }
            if (!Enum.IsDefined(typeof(LoadingSize), options.Size))
            {
                throw new ArgumentOutOfRangeException(nameof(options.Size));
            }

            return new LoadingOptions
            {
                BlocksInteraction = options.BlocksInteraction,
                ShowsBackground = options.ShowsBackground,
                BackgroundColor = options.BackgroundColor,
                BackgroundOpacity = options.BackgroundOpacity,
                Position = options.Position,
                Size = options.Size,
                SpinnerColor = options.SpinnerColor,
                Message = OptionalText(options.Message),
                MessageColor = options.MessageColor,
                MessageFontSize = options.MessageFontSize,
                ShowDelaySeconds = options.ShowDelaySeconds,
                Tag = options.Tag,
                GroupId = options.GroupId
            };
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string RequiredText(string value, string parameterName)
        {
            string normalized = OptionalText(value);
            if (normalized == null)
            {
                throw new ArgumentException("A non-whitespace value is required.", parameterName);
            }

            return normalized;
        }
    }
}
