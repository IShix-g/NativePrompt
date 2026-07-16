using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using NativePrompt.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NativePrompt.Tests
{
    public sealed class NativePromptRuntimeTests
    {
        private MockStrategy _strategy;
        private ImmediateDispatcher _dispatcher;

        [SetUp]
        public void SetUp()
        {
            _strategy = new MockStrategy();
            _dispatcher = new ImmediateDispatcher();
            NativePromptRuntime.SetForTesting(_strategy, _dispatcher);
        }

        [TearDown]
        public void TearDown()
        {
            NativePromptRuntime.RestoreDefaultForTesting();
        }

        [Test]
        public void Facade_NormalizesOptionsBeforePassingThemToStrategy()
        {
            NP.ShowAlert(new AlertOptions
            {
                Title = "  Title  ",
                Content = "  Content  ",
                YesButtonText = "   ",
                NoButtonText = null,
                CloseButtonText = "   "
            });

            NP.ShowBottomSheet(new BottomSheetOptions
            {
                Title = "   ",
                Content = "  Details  ",
                CancelButtonText = "   ",
                Actions = new[]
                {
                    new BottomSheetAction
                    {
                        Id = "  save  ",
                        Text = "  Save  ",
                        Style = BottomSheetActionStyle.Destructive,
                        Enabled = false
                    }
                }
            });

            NP.ShowToast(new ToastOptions
            {
                Message = "  Saved  ",
                Duration = 4f,
                AutoDismiss = true,
                DismissOnTap = false,
                Position = ToastPosition.Top
            });

            NP.ShowLoading(new LoadingOptions
            {
                Message = "  Working  ",
                BlocksInteraction = true,
                ShowsBackground = true,
                BackgroundColor = Color.blue,
                BackgroundOpacity = 0.75f,
                Position = LoadingPosition.TopLeft,
                Size = LoadingSize.Large,
                MessageColor = new Color(0.1f, 0.2f, 0.3f, 0.4f),
                MessageFontSize = 21f,
                ShowDelaySeconds = 0.5f
            });

            Assert.That(_strategy.Alerts[0].Options.Title, Is.EqualTo("Title"));
            Assert.That(_strategy.Alerts[0].Options.Content, Is.EqualTo("Content"));
            Assert.That(_strategy.Alerts[0].Options.YesButtonText, Is.Null);
            Assert.That(_strategy.Alerts[0].Options.CloseButtonText, Is.EqualTo("Close"));

            BottomSheetOptions bottomSheet = _strategy.BottomSheets[0].Options;
            Assert.That(bottomSheet.Title, Is.Null);
            Assert.That(bottomSheet.Content, Is.EqualTo("Details"));
            Assert.That(bottomSheet.CancelButtonText, Is.EqualTo("Cancel"));
            Assert.That(bottomSheet.Actions[0].Id, Is.EqualTo("save"));
            Assert.That(bottomSheet.Actions[0].Text, Is.EqualTo("Save"));
            Assert.That(bottomSheet.Actions[0].Style, Is.EqualTo(BottomSheetActionStyle.Destructive));
            Assert.That(bottomSheet.Actions[0].Enabled, Is.False);

            ToastOptions toast = _strategy.Toasts[0].Options;
            Assert.That(toast.Message, Is.EqualTo("Saved"));
            Assert.That(toast.Duration, Is.EqualTo(4f));
            Assert.That(toast.AutoDismiss, Is.True);
            Assert.That(toast.DismissOnTap, Is.False);
            Assert.That(toast.Position, Is.EqualTo(ToastPosition.Top));

            LoadingOptions loading = _strategy.Loadings[0].Options;
            Assert.That(loading.Message, Is.EqualTo("Working"));
            Assert.That(loading.BlocksInteraction, Is.True);
            Assert.That(loading.ShowsBackground, Is.True);
            Assert.That(loading.BackgroundColor, Is.EqualTo(Color.blue));
            Assert.That(loading.BackgroundOpacity, Is.EqualTo(0.75f));
            Assert.That(loading.Position, Is.EqualTo(LoadingPosition.TopLeft));
            Assert.That(loading.Size, Is.EqualTo(LoadingSize.Large));
            Assert.That(loading.MessageColor, Is.EqualTo(new Color(0.1f, 0.2f, 0.3f, 0.4f)));
            Assert.That(loading.MessageFontSize, Is.EqualTo(21f));
            Assert.That(loading.ShowDelaySeconds, Is.EqualTo(0.5f));
        }

        [Test]
        public void Facade_ValidatesArgumentsSynchronously()
        {
            Assert.Throws<ArgumentNullException>(() => NP.ShowAlert(null));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowAlert(new AlertOptions { Content = "  " }));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowBottomSheet(new BottomSheetOptions()));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowBottomSheet(new BottomSheetOptions
                {
                    Actions = new[]
                    {
                        new BottomSheetAction { Id = "same", Text = "First" },
                        new BottomSheetAction { Id = "same", Text = "Second" }
                    }
                }));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowToast(new ToastOptions { Message = "Toast", Duration = 0f }));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowToast(new ToastOptions { Message = null }));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowToast(new ToastOptions { Message = "   " }));
            Assert.Throws<ArgumentNullException>(() => NP.ShowLoading(null));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowLoading(new LoadingOptions { BackgroundOpacity = -0.01f }));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowLoading(new LoadingOptions { BackgroundOpacity = 1.01f }));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowLoading(new LoadingOptions { BackgroundOpacity = float.NaN }));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowLoading(new LoadingOptions { BackgroundOpacity = float.PositiveInfinity }));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowLoading(new LoadingOptions { ShowDelaySeconds = -0.01f }));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowLoading(new LoadingOptions { ShowDelaySeconds = float.PositiveInfinity }));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowLoading(new LoadingOptions { MessageFontSize = 0f }));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowLoading(new LoadingOptions { MessageFontSize = float.NaN }));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowLoading(new LoadingOptions { MessageFontSize = float.PositiveInfinity }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NP.ShowLoading(new LoadingOptions { Position = (LoadingPosition)999 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NP.ShowLoading(new LoadingOptions { Size = (LoadingSize)999 }));
        }

        [Test]
        public void Loading_NormalizesDefaultValuesAndWhitespaceMessage()
        {
            NP.ShowLoading(new LoadingOptions { Message = " \t " });

            LoadingOptions options = _strategy.Loadings[0].Options;
            Assert.That(options.BlocksInteraction, Is.False);
            Assert.That(options.ShowsBackground, Is.False);
            Assert.That(options.BackgroundColor, Is.EqualTo(Color.white));
            Assert.That(options.BackgroundOpacity, Is.EqualTo(0.5f));
            Assert.That(options.Position, Is.EqualTo(LoadingPosition.BottomRight));
            Assert.That(options.Size, Is.EqualTo(LoadingSize.Medium));
            Assert.That(options.Message, Is.Null);
            Assert.That(options.MessageColor, Is.EqualTo(new Color(0.33f, 0.33f, 0.33f, 1f)));
            Assert.That(options.MessageFontSize, Is.EqualTo(17f));
            Assert.That(options.ShowDelaySeconds, Is.EqualTo(0.25f));
        }

        [Test]
        public void LoadingHandle_DismissAndDisposeAreIdempotent()
        {
            LoadingHandle dismissed = NP.ShowLoading(new LoadingOptions());
            dismissed.Dismiss();
            dismissed.Dismiss();
            dismissed.Dispose();

            LoadingHandle disposed = NP.ShowLoading(new LoadingOptions());
            disposed.Dispose();
            disposed.Dispose();
            disposed.Dismiss();

            Assert.That(_strategy.DismissedLoadingIds, Has.Count.EqualTo(2));
            Assert.That(_strategy.DismissedLoadingIds[0], Is.EqualTo(dismissed.RequestId));
            Assert.That(_strategy.DismissedLoadingIds[1], Is.EqualTo(disposed.RequestId));
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
            Assert.That(NativePromptRuntime.ActiveLoadingCountForTesting, Is.Zero);
        }

        [Test]
        public void Loading_LatestRequestWinsAndRestoresPreviousOptions()
        {
            LoadingHandle first = NP.ShowLoading(new LoadingOptions
            {
                Message = "First",
                Position = LoadingPosition.TopLeft,
                Tag = "first-tag",
                GroupId = "loading-group"
            });
            LoadingHandle second = NP.ShowLoading(new LoadingOptions
            {
                Message = "Second",
                Position = LoadingPosition.Center
            });

            second.Dismiss();

            Assert.That(_strategy.Loadings, Has.Count.EqualTo(3));
            Assert.That(_strategy.Loadings[0].RequestId, Is.EqualTo(first.RequestId));
            Assert.That(_strategy.Loadings[1].RequestId, Is.EqualTo(second.RequestId));
            Assert.That(_strategy.Loadings[2].RequestId, Is.EqualTo(first.RequestId));
            Assert.That(_strategy.Loadings[2].Options.Message, Is.EqualTo("First"));
            Assert.That(_strategy.Loadings[2].Options.Position, Is.EqualTo(LoadingPosition.TopLeft));
            Assert.That(first.Tag, Is.EqualTo("first-tag"));
            Assert.That(first.GroupId, Is.EqualTo("loading-group"));

            first.Dispose();
            Assert.That(_strategy.DismissedLoadingIds, Is.EqualTo(new[] { first.RequestId }));
        }

        [Test]
        public void Loading_EndingOlderRequestDoesNotAffectLatestRequest()
        {
            LoadingHandle first = NP.ShowLoading(new LoadingOptions { Message = "First" });
            LoadingHandle second = NP.ShowLoading(new LoadingOptions { Message = "Second" });

            first.Dispose();

            Assert.That(_strategy.Loadings, Has.Count.EqualTo(2));
            Assert.That(_strategy.DismissedLoadingIds, Is.Empty);
            Assert.That(NativePromptRuntime.ActiveLoadingCountForTesting, Is.EqualTo(1));

            second.Dismiss();
            Assert.That(_strategy.DismissedLoadingIds, Is.EqualTo(new[] { second.RequestId }));
        }

        [Test]
        public void Loading_ShowFailureCleansUpAndRestoresPreviousRequest()
        {
            LoadingHandle first = NP.ShowLoading(new LoadingOptions { Message = "First" });
            _strategy.NextShowLoadingException = new InvalidOperationException("show failure");

            Assert.Throws<InvalidOperationException>(() =>
                NP.ShowLoading(new LoadingOptions { Message = "Second" }));

            Assert.That(_strategy.Loadings, Has.Count.EqualTo(3));
            Assert.That(_strategy.Loadings[2].RequestId, Is.EqualTo(first.RequestId));
            Assert.That(_strategy.Loadings[2].Options.Message, Is.EqualTo("First"));
            Assert.That(NativePromptRuntime.ActiveLoadingCountForTesting, Is.EqualTo(1));

            first.Dispose();
            Assert.That(NativePromptRuntime.ActiveLoadingCountForTesting, Is.Zero);

            _strategy.NextShowLoadingException = new InvalidOperationException("first failure");
            Assert.Throws<InvalidOperationException>(() =>
                NP.ShowLoading(new LoadingOptions { Message = "Only" }));
            Assert.That(NativePromptRuntime.ActiveLoadingCountForTesting, Is.Zero);
            Assert.That(_strategy.DismissedLoadingIds, Has.Count.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator EditorLoading_LogsOnlyAfterUnscaledDelay()
        {
            NativePromptRuntime.SetForTesting(new EditorNativePromptStrategy(), _dispatcher);
            LogAssert.Expect(
                LogType.Log,
                new Regex("NativePrompt Loading: position=Center, size=Small, message=Working"));

            NP.ShowLoading(new LoadingOptions
            {
                Message = "Working",
                Position = LoadingPosition.Center,
                Size = LoadingSize.Small,
                ShowDelaySeconds = 0.01f
            });

            double deadline = UnityEditor.EditorApplication.timeSinceStartup + 0.05;
            while (UnityEditor.EditorApplication.timeSinceStartup < deadline)
            {
                yield return null;
            }

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EditorLoading_DismissedDuringDelayDoesNotLog()
        {
            NativePromptRuntime.SetForTesting(new EditorNativePromptStrategy(), _dispatcher);
            LoadingHandle handle = NP.ShowLoading(new LoadingOptions
            {
                Message = "Do not log",
                ShowDelaySeconds = 0.02f
            });

            handle.Dismiss();
            double deadline = UnityEditor.EditorApplication.timeSinceStartup + 0.05;
            while (UnityEditor.EditorApplication.timeSinceStartup < deadline)
            {
                yield return null;
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Toast_NormalizesDefaultValues()
        {
            NP.ShowToast(new ToastOptions { Message = "Toast" });

            ToastOptions options = _strategy.Toasts[0].Options;
            Assert.That(options.Duration, Is.EqualTo(2.5f));
            Assert.That(options.AutoDismiss, Is.True);
            Assert.That(options.DismissOnTap, Is.True);
            Assert.That(options.Position, Is.EqualTo(ToastPosition.Bottom));
        }

        [Test]
        public void BottomSheet_RejectsMissingAndExcessActions()
        {
            Assert.Throws<ArgumentException>(() =>
                NP.ShowBottomSheet(new BottomSheetOptions { Actions = Array.Empty<BottomSheetAction>() }));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowBottomSheet(new BottomSheetOptions
                {
                    Actions = new[]
                    {
                        new BottomSheetAction { Id = "one", Text = "One" },
                        new BottomSheetAction { Id = "two", Text = "Two" },
                        new BottomSheetAction { Id = "three", Text = "Three" },
                        new BottomSheetAction { Id = "four", Text = "Four" }
                    }
                }));
        }

        [Test]
        public void BottomSheet_RejectsDuplicateAndEmptyActionValues()
        {
            Assert.Throws<ArgumentException>(() =>
                NP.ShowBottomSheet(new BottomSheetOptions
                {
                    Actions = new[]
                    {
                        new BottomSheetAction { Id = "same", Text = "First" },
                        new BottomSheetAction { Id = "same", Text = "Second" }
                    }
                }));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowBottomSheet(new BottomSheetOptions
                {
                    Actions = new[] { new BottomSheetAction { Id = " ", Text = "Action" } }
                }));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowBottomSheet(new BottomSheetOptions
                {
                    Actions = new[] { new BottomSheetAction { Id = "action", Text = " " } }
                }));
        }

        [Test]
        public void BottomSheet_PassesStylesAndEnabledStateToStrategy()
        {
            NP.ShowBottomSheet(new BottomSheetOptions
            {
                Actions = new[]
                {
                    new BottomSheetAction
                    {
                        Id = "default",
                        Text = "Default",
                        Style = BottomSheetActionStyle.Default,
                        Enabled = true
                    },
                    new BottomSheetAction
                    {
                        Id = "destructive",
                        Text = "Destructive",
                        Style = BottomSheetActionStyle.Destructive,
                        Enabled = false
                    }
                }
            });

            BottomSheetAction[] actions = _strategy.BottomSheets[0].Options.Actions;
            Assert.That(actions[0].Style, Is.EqualTo(BottomSheetActionStyle.Default));
            Assert.That(actions[0].Enabled, Is.True);
            Assert.That(actions[1].Style, Is.EqualTo(BottomSheetActionStyle.Destructive));
            Assert.That(actions[1].Enabled, Is.False);
        }

        [Test]
        public void BottomSheet_ReturnsSelectionOrCancellationOnce()
        {
            BottomSheetResult selected = default;
            BottomSheetResult cancelled = default;
            int selectedCount = 0;
            int cancelledCount = 0;

            NP.ShowBottomSheet(CreateBottomSheet("select"), result =>
            {
                selected = result;
                selectedCount++;
            });
            NP.ShowBottomSheet(CreateBottomSheet("cancel"), result =>
            {
                cancelled = result;
                cancelledCount++;
            });

            string selectedRequestId = _strategy.BottomSheets[0].RequestId;
            string cancelledRequestId = _strategy.BottomSheets[1].RequestId;
            NativePromptCallbackReceiver.BottomSheetActionSelected(selectedRequestId, "select");
            NativePromptCallbackReceiver.BottomSheetCancelled(selectedRequestId);
            NativePromptCallbackReceiver.BottomSheetCancelled(cancelledRequestId);
            NativePromptCallbackReceiver.BottomSheetActionSelected(cancelledRequestId, "cancel");

            Assert.That(selectedCount, Is.EqualTo(1));
            Assert.That(selected.IsCancelled, Is.False);
            Assert.That(selected.ActionId, Is.EqualTo("select"));
            Assert.That(cancelledCount, Is.EqualTo(1));
            Assert.That(cancelled.IsCancelled, Is.True);
            Assert.That(cancelled.ActionId, Is.Null);
        }

        [Test]
        public void EditorBottomSheet_CanBeDismissedByItsHandle()
        {
            NativePromptRuntime.SetForTesting(new EditorNativePromptStrategy(), _dispatcher);
            _strategy.ClearResetCount();
            BottomSheetResult completed = default;
            int callbackCount = 0;

            BottomSheetHandle handle = NP.ShowBottomSheet(new BottomSheetOptions
            {
                Title = "Choose",
                Content = "Details",
                CancelButtonText = "Close",
                Actions = new[]
                {
                    new BottomSheetAction
                    {
                        Id = "delete",
                        Text = "Delete",
                        Style = BottomSheetActionStyle.Destructive,
                        Enabled = false
                    }
                }
            }, result =>
            {
                completed = result;
                callbackCount++;
            });

            handle.Dismiss();

            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(completed.IsCancelled, Is.True);
            Assert.That(completed.ActionId, Is.Null);
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
        }

        [Test]
        public void Alert_RejectsMissingContent()
        {
            Assert.Throws<ArgumentException>(() =>
                NP.ShowAlert(new AlertOptions { Content = null }));
            Assert.Throws<ArgumentException>(() =>
                NP.ShowAlert(new AlertOptions { Content = "   " }));
        }

        [Test]
        public void Alert_OmitsTitleWhenNotSpecified()
        {
            NP.ShowAlert(new AlertOptions { Content = "Content" });

            Assert.That(_strategy.Alerts[0].Options.Title, Is.Null);
        }

        [Test]
        public void Alert_ContentOnlyAddsDefaultOrCustomCloseButton()
        {
            NP.ShowAlert(new AlertOptions { Content = "Default close" });

            AlertOptions defaultClose = _strategy.Alerts[0].Options;
            Assert.That(defaultClose.YesButtonText, Is.Null);
            Assert.That(defaultClose.NoButtonText, Is.Null);
            Assert.That(defaultClose.CloseButtonText, Is.EqualTo("Close"));

            NativePromptCallbackReceiver.AlertCompleted(
                _strategy.Alerts[0].RequestId,
                AlertResult.Closed);
            NP.ShowAlert(new AlertOptions
            {
                Content = "Custom close",
                CloseButtonText = "Dismiss"
            });

            Assert.That(
                _strategy.Alerts[1].Options.CloseButtonText,
                Is.EqualTo("Dismiss"));
        }

        [Test]
        public void Alert_ContentAndYesOnlyPassesAffirmativeButton()
        {
            NP.ShowAlert(new AlertOptions
            {
                Content = "Continue?",
                YesButtonText = "Continue"
            });

            AlertOptions options = _strategy.Alerts[0].Options;
            Assert.That(options.YesButtonText, Is.EqualTo("Continue"));
            Assert.That(options.NoButtonText, Is.Null);
        }

        [Test]
        public void Alert_ContentAndNoOnlyPassesNegativeButton()
        {
            NP.ShowAlert(new AlertOptions
            {
                Content = "Stop?",
                NoButtonText = "Stop"
            });

            AlertOptions options = _strategy.Alerts[0].Options;
            Assert.That(options.YesButtonText, Is.Null);
            Assert.That(options.NoButtonText, Is.EqualTo("Stop"));
        }

        [Test]
        public void Alert_AllFieldsPassToStrategy()
        {
            NP.ShowAlert(new AlertOptions
            {
                Title = "Question",
                Content = "Continue?",
                YesButtonText = "Yes",
                NoButtonText = "No"
            });

            AlertOptions options = _strategy.Alerts[0].Options;
            Assert.That(options.Title, Is.EqualTo("Question"));
            Assert.That(options.Content, Is.EqualTo("Continue?"));
            Assert.That(options.YesButtonText, Is.EqualTo("Yes"));
            Assert.That(options.NoButtonText, Is.EqualTo("No"));
        }

        [Test]
        public void Alert_WhitespaceButtonTextIsTreatedAsOmitted()
        {
            NP.ShowAlert(new AlertOptions
            {
                Content = "Content",
                YesButtonText = "   ",
                NoButtonText = "\t",
                CloseButtonText = " Done "
            });

            AlertOptions options = _strategy.Alerts[0].Options;
            Assert.That(options.YesButtonText, Is.Null);
            Assert.That(options.NoButtonText, Is.Null);
            Assert.That(options.CloseButtonText, Is.EqualTo("Done"));
        }

        [Test]
        public void Alert_MapsYesNoAndClosedResultsToCallbacks()
        {
            var results = new List<AlertResult>();

            NP.ShowAlert(
                new AlertOptions { Content = "Yes", YesButtonText = "Yes" },
                results.Add);
            NativePromptCallbackReceiver.AlertCompleted(
                _strategy.Alerts[0].RequestId,
                AlertResult.Yes);

            NP.ShowAlert(
                new AlertOptions { Content = "No", NoButtonText = "No" },
                results.Add);
            NativePromptCallbackReceiver.AlertCompleted(
                _strategy.Alerts[1].RequestId,
                AlertResult.No);

            NP.ShowAlert(
                new AlertOptions { Content = "Closed" },
                results.Add);
            NativePromptCallbackReceiver.AlertCompleted(
                _strategy.Alerts[2].RequestId,
                AlertResult.Closed);

            Assert.That(results, Is.EqualTo(new[]
            {
                AlertResult.Yes,
                AlertResult.No,
                AlertResult.Closed
            }));
        }

        [Test]
        public void Alert_ProcessesRequestsInFifoOrder()
        {
            var completed = new List<string>();

            NP.ShowAlert(
                new AlertOptions { Content = "First" },
                _ => completed.Add("first"));
            NP.ShowAlert(
                new AlertOptions { Content = "Second" },
                _ => completed.Add("second"));

            Assert.That(_strategy.Alerts, Has.Count.EqualTo(1));
            Assert.That(_strategy.Alerts[0].Options.Content, Is.EqualTo("First"));

            NativePromptCallbackReceiver.AlertCompleted(
                _strategy.Alerts[0].RequestId,
                AlertResult.Closed);

            Assert.That(completed, Is.EqualTo(new[] { "first" }));
            Assert.That(_strategy.Alerts, Has.Count.EqualTo(2));
            Assert.That(_strategy.Alerts[1].Options.Content, Is.EqualTo("Second"));

            NativePromptCallbackReceiver.AlertCompleted(
                _strategy.Alerts[1].RequestId,
                AlertResult.Closed);

            Assert.That(completed, Is.EqualTo(new[] { "first", "second" }));
        }

        [Test]
        public void CallbackReceiver_MatchesRequestAndIgnoresDuplicates()
        {
            BottomSheetResult firstResult = default;
            BottomSheetResult secondResult = default;
            int firstCount = 0;
            int secondCount = 0;

            NP.ShowBottomSheet(CreateBottomSheet("first"), result =>
            {
                firstResult = result;
                firstCount++;
            });
            NP.ShowBottomSheet(CreateBottomSheet("second"), result =>
            {
                secondResult = result;
                secondCount++;
            });

            string firstId = _strategy.BottomSheets[0].RequestId;
            string secondId = _strategy.BottomSheets[1].RequestId;
            NativePromptCallbackReceiver.BottomSheetActionSelected(secondId, "second");
            NativePromptCallbackReceiver.BottomSheetActionSelected(firstId, "first");
            NativePromptCallbackReceiver.BottomSheetCancelled(firstId);
            NativePromptCallbackReceiver.BottomSheetCancelled("unknown");

            Assert.That(firstCount, Is.EqualTo(1));
            Assert.That(firstResult.ActionId, Is.EqualTo("first"));
            Assert.That(secondCount, Is.EqualTo(1));
            Assert.That(secondResult.ActionId, Is.EqualTo("second"));
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
        }

        [Test]
        public void Callback_IsDispatchedOnceThroughMainThreadDispatcher()
        {
            var queuedDispatcher = new QueuedDispatcher();
            NativePromptRuntime.SetForTesting(_strategy, queuedDispatcher);
            _strategy.ClearResetCount();
            int callbackCount = 0;

            NP.ShowAlert(
                new AlertOptions { Content = "Queued" },
                _ => callbackCount++);
            string requestId = _strategy.Alerts[0].RequestId;

            NativePromptCallbackReceiver.AlertCompleted(requestId, AlertResult.Closed);
            NativePromptCallbackReceiver.AlertCompleted(requestId, AlertResult.Yes);

            Assert.That(callbackCount, Is.Zero);
            Assert.That(queuedDispatcher.Count, Is.EqualTo(1));

            queuedDispatcher.Drain();

            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
        }

        [Test]
        public void HandlesAndLifecycleEvents_ExposeUniqueSnapshotMetadataInOrder()
        {
            var order = new List<string>();
            var requestIds = new HashSet<string>();
            AlertOpenedEventArgs alertOpened = null;
            AlertCompletedEventArgs alertCompleted = null;
            BottomSheetCompletedEventArgs sheetCompleted = null;
            ToastDismissedEventArgs toastDismissed = null;

            EventHandler<AlertOpenedEventArgs> onAlertOpened = (_, args) =>
            {
                alertOpened = args;
                order.Add("alert-opened");
            };
            EventHandler<AlertCompletedEventArgs> onAlertCompleted = (_, args) =>
            {
                alertCompleted = args;
                order.Add("alert-event");
            };
            EventHandler<BottomSheetCompletedEventArgs> onSheetCompleted = (_, args) =>
                sheetCompleted = args;
            EventHandler<ToastDismissedEventArgs> onToastDismissed = (_, args) =>
                toastDismissed = args;

            NP.AlertOpened += onAlertOpened;
            NP.AlertCompleted += onAlertCompleted;
            NP.BottomSheetCompleted += onSheetCompleted;
            NP.ToastDismissed += onToastDismissed;
            try
            {
                var alertOptions = new AlertOptions
                {
                    Content = "Alert",
                    Tag = "alert-tag",
                    GroupId = "screen"
                };
                AlertHandle alert = NP.ShowAlert(alertOptions, _ => order.Add("alert-callback"));
                alertOptions.Tag = "changed";
                NativePromptCallbackReceiver.AlertOpened(alert.RequestId);
                NativePromptCallbackReceiver.AlertCompleted(alert.RequestId, AlertResult.Closed);

                BottomSheetHandle sheet = NP.ShowBottomSheet(new BottomSheetOptions
                {
                    Tag = "sheet-tag",
                    GroupId = "screen",
                    Actions = new[] { new BottomSheetAction { Id = "go", Text = "Go" } }
                });
                NativePromptCallbackReceiver.BottomSheetOpened(sheet.RequestId);
                NativePromptCallbackReceiver.BottomSheetCancelled(sheet.RequestId);

                ToastHandle toast = NP.ShowToast(new ToastOptions
                {
                    Message = "Toast",
                    Tag = "toast-tag",
                    GroupId = "screen"
                });
                NativePromptCallbackReceiver.ToastShown(toast.RequestId);
                NativePromptCallbackReceiver.ToastDismissed(
                    toast.RequestId,
                    ToastDismissReason.TimedOut);

                requestIds.Add(alert.RequestId);
                requestIds.Add(sheet.RequestId);
                requestIds.Add(toast.RequestId);
                Assert.That(requestIds, Has.Count.EqualTo(3));
                Assert.That(alert.Tag, Is.EqualTo("alert-tag"));
                Assert.That(alert.GroupId, Is.EqualTo("screen"));
                Assert.That(alertOpened.Tag, Is.EqualTo(alert.Tag));
                Assert.That(alertOpened.GroupId, Is.EqualTo(alert.GroupId));
                Assert.That(alertCompleted.RequestId, Is.EqualTo(alert.RequestId));
                Assert.That(alertCompleted.Result, Is.EqualTo(AlertResult.Closed));
                Assert.That(sheetCompleted.RequestId, Is.EqualTo(sheet.RequestId));
                Assert.That(sheetCompleted.Result.IsCancelled, Is.True);
                Assert.That(toastDismissed.RequestId, Is.EqualTo(toast.RequestId));
                Assert.That(toastDismissed.Reason, Is.EqualTo(ToastDismissReason.TimedOut));
                Assert.That(order, Is.EqualTo(new[]
                {
                    "alert-opened",
                    "alert-callback",
                    "alert-event"
                }));
            }
            finally
            {
                NP.AlertOpened -= onAlertOpened;
                NP.AlertCompleted -= onAlertCompleted;
                NP.BottomSheetCompleted -= onSheetCompleted;
                NP.ToastDismissed -= onToastDismissed;
            }
        }

        [Test]
        public void AlertHandles_DismissActiveAndQueuedRequestsIndependentlyAndOnce()
        {
            var results = new List<string>();
            AlertHandle active = NP.ShowAlert(
                new AlertOptions { Content = "Active" },
                result => results.Add("active:" + result));
            AlertHandle queued = NP.ShowAlert(
                new AlertOptions { Content = "Queued" },
                result => results.Add("queued:" + result));

            queued.Dismiss();
            queued.Dismiss();
            active.Dismiss();
            active.Dismiss();

            Assert.That(_strategy.Alerts, Has.Count.EqualTo(1));
            Assert.That(_strategy.DismissedAlertIds, Is.EqualTo(new[] { active.RequestId }));
            Assert.That(results, Is.EqualTo(new[]
            {
                "queued:Dismissed",
                "active:Dismissed"
            }));
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
        }

        [Test]
        public void BottomSheetHandle_DismissesOnlyItsRequestAsCancellation()
        {
            int firstCount = 0;
            int secondCount = 0;
            BottomSheetResult firstResult = default;
            BottomSheetHandle first = NP.ShowBottomSheet(CreateBottomSheet("first"), result =>
            {
                firstCount++;
                firstResult = result;
            });
            BottomSheetHandle second = NP.ShowBottomSheet(
                CreateBottomSheet("second"),
                _ => secondCount++);

            first.Dismiss();
            first.Dismiss();
            NativePromptCallbackReceiver.BottomSheetActionSelected(second.RequestId, "second");
            NativePromptCallbackReceiver.BottomSheetCancelled(first.RequestId);

            Assert.That(firstCount, Is.EqualTo(1));
            Assert.That(firstResult.IsCancelled, Is.True);
            Assert.That(secondCount, Is.EqualTo(1));
            Assert.That(_strategy.DismissedBottomSheetIds, Is.EqualTo(new[] { first.RequestId }));
        }

        [Test]
        public void LifecycleCancellation_SilentlyDisposesEachPromptType()
        {
            var cancellation = new CancellationTokenSource();
            int callbackCount = 0;
            int eventCount = 0;
            EventHandler<AlertCompletedEventArgs> onAlert = (_, __) => eventCount++;
            EventHandler<BottomSheetCompletedEventArgs> onSheet = (_, __) => eventCount++;
            EventHandler<ToastDismissedEventArgs> onToast = (_, __) => eventCount++;
            NP.AlertCompleted += onAlert;
            NP.BottomSheetCompleted += onSheet;
            NP.ToastDismissed += onToast;
            try
            {
                AlertHandle alert = NP.ShowAlert(
                    new AlertOptions { Content = "Alert" },
                    _ => callbackCount++);
                BottomSheetHandle sheet = NP.ShowBottomSheet(
                    CreateBottomSheet("sheet"),
                    _ => callbackCount++);
                ToastHandle toast = NP.ShowToast(
                    new ToastOptions { Message = "Toast" },
                    _ => callbackCount++);
                LoadingHandle loading = NP.ShowLoading(new LoadingOptions());

                alert.AddTo(cancellation.Token);
                sheet.AddTo(cancellation.Token);
                toast.AddTo(cancellation.Token);
                loading.AddTo(cancellation.Token);
                cancellation.Cancel();

                Assert.That(_strategy.DismissedAlertIds, Is.EqualTo(new[] { alert.RequestId }));
                Assert.That(
                    _strategy.DismissedBottomSheetIds,
                    Is.EqualTo(new[] { sheet.RequestId }));
                Assert.That(_strategy.DismissedToastIds, Is.EqualTo(new[] { toast.RequestId }));
                Assert.That(
                    _strategy.DismissedLoadingIds,
                    Is.EqualTo(new[] { loading.RequestId }));
                Assert.That(callbackCount, Is.Zero);
                Assert.That(eventCount, Is.Zero);
                Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
                Assert.That(NativePromptRuntime.ActiveLoadingCountForTesting, Is.Zero);

                NativePromptCallbackReceiver.AlertCompleted(alert.RequestId, AlertResult.Yes);
                NativePromptCallbackReceiver.BottomSheetCancelled(sheet.RequestId);
                NativePromptCallbackReceiver.ToastDismissed(
                    toast.RequestId,
                    ToastDismissReason.TimedOut);
                alert.Dismiss();
                sheet.Dispose();
                toast.Dispose();
                loading.Dismiss();

                Assert.That(callbackCount, Is.Zero);
                Assert.That(eventCount, Is.Zero);
            }
            finally
            {
                NP.AlertCompleted -= onAlert;
                NP.BottomSheetCompleted -= onSheet;
                NP.ToastDismissed -= onToast;
                cancellation.Dispose();
            }
        }

        [Test]
        public void AlertDispose_RemovesQueuedRequestAndStartsNextAfterActiveRequest()
        {
            int disposedCallbackCount = 0;
            int remainingCallbackCount = 0;
            AlertHandle active = NP.ShowAlert(
                new AlertOptions { Content = "Active" },
                _ => disposedCallbackCount++);
            AlertHandle queued = NP.ShowAlert(
                new AlertOptions { Content = "Dispose while queued" },
                _ => disposedCallbackCount++);
            AlertHandle remaining = NP.ShowAlert(
                new AlertOptions { Content = "Remaining" },
                _ => remainingCallbackCount++);

            queued.Dispose();
            active.Dispose();

            Assert.That(_strategy.Alerts, Has.Count.EqualTo(2));
            Assert.That(_strategy.Alerts[1].RequestId, Is.EqualTo(remaining.RequestId));
            Assert.That(_strategy.DismissedAlertIds, Is.EqualTo(new[] { active.RequestId }));
            Assert.That(disposedCallbackCount, Is.Zero);

            NativePromptCallbackReceiver.AlertCompleted(remaining.RequestId, AlertResult.Closed);

            Assert.That(remainingCallbackCount, Is.EqualTo(1));
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
        }

        [Test]
        public void BottomSheetDispose_DoesNotAffectAnotherRequest()
        {
            int disposedCallbackCount = 0;
            int remainingCallbackCount = 0;
            BottomSheetHandle disposed = NP.ShowBottomSheet(
                CreateBottomSheet("disposed"),
                _ => disposedCallbackCount++);
            BottomSheetHandle remaining = NP.ShowBottomSheet(
                CreateBottomSheet("remaining"),
                _ => remainingCallbackCount++);

            disposed.Dispose();
            NativePromptCallbackReceiver.BottomSheetActionSelected(
                remaining.RequestId,
                "remaining");

            Assert.That(disposedCallbackCount, Is.Zero);
            Assert.That(remainingCallbackCount, Is.EqualTo(1));
            Assert.That(
                _strategy.DismissedBottomSheetIds,
                Is.EqualTo(new[] { disposed.RequestId }));
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
        }

        [Test]
        public void ToastDispose_DuringReplacementDoesNotAffectCurrentRequest()
        {
            var queuedDispatcher = new QueuedDispatcher();
            NativePromptRuntime.SetForTesting(_strategy, queuedDispatcher);
            _strategy.ClearResetCount();
            int disposedCallbackCount = 0;
            int currentCallbackCount = 0;
            ToastHandle disposed = NP.ShowToast(
                new ToastOptions { Message = "Disposed" },
                _ => disposedCallbackCount++);
            ToastHandle current = NP.ShowToast(
                new ToastOptions { Message = "Current" },
                _ => currentCallbackCount++);

            disposed.Dispose();
            NativePromptCallbackReceiver.ToastDismissed(
                current.RequestId,
                ToastDismissReason.Tapped);
            queuedDispatcher.Drain();

            Assert.That(disposedCallbackCount, Is.Zero);
            Assert.That(currentCallbackCount, Is.EqualTo(1));
            Assert.That(_strategy.DismissedToastIds, Is.EqualTo(new[] { disposed.RequestId }));
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
        }

        [Test]
        public void Dispose_AfterNativeResultBeforeDispatchSuppressesCallbacksAndEvents()
        {
            var queuedDispatcher = new QueuedDispatcher();
            NativePromptRuntime.SetForTesting(_strategy, queuedDispatcher);
            _strategy.ClearResetCount();
            int callbackCount = 0;
            int eventCount = 0;
            EventHandler<AlertCompletedEventArgs> onAlert = (_, __) => eventCount++;
            EventHandler<BottomSheetCompletedEventArgs> onSheet = (_, __) => eventCount++;
            EventHandler<ToastDismissedEventArgs> onToast = (_, __) => eventCount++;
            NP.AlertCompleted += onAlert;
            NP.BottomSheetCompleted += onSheet;
            NP.ToastDismissed += onToast;
            try
            {
                AlertHandle alert = NP.ShowAlert(
                    new AlertOptions { Content = "Alert" },
                    _ => callbackCount++);
                BottomSheetHandle sheet = NP.ShowBottomSheet(
                    CreateBottomSheet("sheet"),
                    _ => callbackCount++);
                ToastHandle toast = NP.ShowToast(
                    new ToastOptions { Message = "Toast" },
                    _ => callbackCount++);

                NativePromptCallbackReceiver.AlertCompleted(alert.RequestId, AlertResult.Yes);
                NativePromptCallbackReceiver.BottomSheetCancelled(sheet.RequestId);
                NativePromptCallbackReceiver.ToastDismissed(
                    toast.RequestId,
                    ToastDismissReason.TimedOut);

                alert.Dispose();
                sheet.Dispose();
                toast.Dispose();
                queuedDispatcher.Drain();

                Assert.That(callbackCount, Is.Zero);
                Assert.That(eventCount, Is.Zero);
                Assert.That(_strategy.DismissedAlertIds, Is.Empty);
                Assert.That(_strategy.DismissedBottomSheetIds, Is.Empty);
                Assert.That(_strategy.DismissedToastIds, Is.Empty);
                Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
            }
            finally
            {
                NP.AlertCompleted -= onAlert;
                NP.BottomSheetCompleted -= onSheet;
                NP.ToastDismissed -= onToast;
            }
        }

        [Test]
        public void Dispose_AfterDismissBeforeDispatchDoesNotDismissTwiceOrNotify()
        {
            var queuedDispatcher = new QueuedDispatcher();
            NativePromptRuntime.SetForTesting(_strategy, queuedDispatcher);
            _strategy.ClearResetCount();
            int callbackCount = 0;
            int eventCount = 0;
            EventHandler<AlertCompletedEventArgs> onCompleted = (_, __) => eventCount++;
            NP.AlertCompleted += onCompleted;
            try
            {
                AlertHandle handle = NP.ShowAlert(
                    new AlertOptions { Content = "Alert" },
                    _ => callbackCount++);

                handle.Dismiss();
                handle.Dispose();
                queuedDispatcher.Drain();

                Assert.That(_strategy.DismissedAlertIds, Is.EqualTo(new[] { handle.RequestId }));
                Assert.That(callbackCount, Is.Zero);
                Assert.That(eventCount, Is.Zero);
                Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
            }
            finally
            {
                NP.AlertCompleted -= onCompleted;
            }
        }

        [Test]
        public void Dispose_DuringPlatformDismissAdvancesAlertOnlyAfterDismissReturns()
        {
            AlertHandle active = NP.ShowAlert(new AlertOptions { Content = "Active" });
            AlertHandle queued = NP.ShowAlert(new AlertOptions { Content = "Queued" });
            _strategy.OnDismissAlert = () =>
            {
                active.Dispose();
                Assert.That(_strategy.Alerts, Has.Count.EqualTo(1));
            };

            active.Dismiss();
            _strategy.OnDismissAlert = null;

            Assert.That(_strategy.DismissedAlertIds, Is.EqualTo(new[] { active.RequestId }));
            Assert.That(_strategy.Alerts, Has.Count.EqualTo(2));
            Assert.That(_strategy.Alerts[1].RequestId, Is.EqualTo(queued.RequestId));
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.EqualTo(1));

            queued.Dispose();
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
        }

        [Test]
        public void Dispose_AfterIndividualCallbackStartsDoesNotInterruptCompletionEvent()
        {
            var queuedDispatcher = new QueuedDispatcher();
            NativePromptRuntime.SetForTesting(_strategy, queuedDispatcher);
            _strategy.ClearResetCount();
            AlertHandle handle = null;
            int callbackCount = 0;
            int eventCount = 0;
            EventHandler<AlertCompletedEventArgs> onCompleted = (_, __) => eventCount++;
            NP.AlertCompleted += onCompleted;
            try
            {
                handle = NP.ShowAlert(
                    new AlertOptions { Content = "Alert" },
                    _ =>
                    {
                        callbackCount++;
                        handle.Dispose();
                    });

                NativePromptCallbackReceiver.AlertCompleted(handle.RequestId, AlertResult.Yes);
                queuedDispatcher.Drain();

                Assert.That(callbackCount, Is.EqualTo(1));
                Assert.That(eventCount, Is.EqualTo(1));
                Assert.That(_strategy.DismissedAlertIds, Is.Empty);
            }
            finally
            {
                NP.AlertCompleted -= onCompleted;
            }
        }

        [Test]
        public void AddTo_RejectsDestroyedOwnerAndIgnoresDisable()
        {
            var ownerObject = new GameObject("Prompt owner");
            PromptHandleTestOwner owner = ownerObject.AddComponent<PromptHandleTestOwner>();
            AlertHandle active = NP.ShowAlert(new AlertOptions { Content = "Active" });

            Assert.Throws<ArgumentNullException>(() => active.AddTo(null));
            Assert.That(active.AddTo(owner), Is.SameAs(active));
            owner.enabled = false;
            ownerObject.SetActive(false);

            Assert.That(_strategy.DismissedAlertIds, Is.Empty);

            var destroyedObject = new GameObject("Destroyed owner");
            PromptHandleTestOwner destroyedOwner =
                destroyedObject.AddComponent<PromptHandleTestOwner>();
            UnityEngine.Object.DestroyImmediate(destroyedObject);
            Assert.Throws<ArgumentNullException>(() => active.AddTo(destroyedOwner));

            active.Dispose();
            UnityEngine.Object.DestroyImmediate(ownerObject);
        }

        [Test]
        public void CompletedHandle_ReleasesOwnerRegistration()
        {
            var ownerObject = new GameObject("Prompt owner");
            PromptHandleTestOwner owner = ownerObject.AddComponent<PromptHandleTestOwner>();
            int callbackCount = 0;
            ToastHandle handle = NP.ShowToast(
                new ToastOptions { Message = "Toast" },
                _ => callbackCount++).AddTo(owner);

            NativePromptCallbackReceiver.ToastDismissed(
                handle.RequestId,
                ToastDismissReason.TimedOut);
            UnityEngine.Object.DestroyImmediate(ownerObject);
            handle.Dispose();

            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(_strategy.DismissedToastIds, Is.Empty);
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
        }

        [Test]
        public void LifecycleCancellation_DismissFailureIsLoggedAndDoesNotEscape()
        {
            var cancellation = new CancellationTokenSource();
            _strategy.DismissAlertException = new InvalidOperationException("dismiss failure");
            NP.ShowAlert(new AlertOptions { Content = "Alert" })
                .AddTo(cancellation.Token);
            LogAssert.Expect(LogType.Exception, new Regex("dismiss failure"));

            Assert.DoesNotThrow(cancellation.Cancel);
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
            cancellation.Dispose();
        }

        [Test]
        public void CallbackAndEventExceptions_DoNotStopLaterNotificationsOrAlertQueue()
        {
            int laterSubscriberCount = 0;
            int secondAlertCount = 0;
            EventHandler<AlertCompletedEventArgs> throwing = (_, __) =>
                throw new InvalidOperationException("event failure");
            EventHandler<AlertCompletedEventArgs> later = (_, __) => laterSubscriberCount++;
            NP.AlertCompleted += throwing;
            NP.AlertCompleted += later;
            LogAssert.Expect(LogType.Exception, new Regex("callback failure"));
            LogAssert.Expect(LogType.Exception, new Regex("event failure"));
            LogAssert.Expect(LogType.Exception, new Regex("event failure"));
            try
            {
                NP.ShowAlert(
                    new AlertOptions { Content = "First" },
                    _ => throw new InvalidOperationException("callback failure"));
                NP.ShowAlert(
                    new AlertOptions { Content = "Second" },
                    _ => secondAlertCount++);

                NativePromptCallbackReceiver.AlertCompleted(
                    _strategy.Alerts[0].RequestId,
                    AlertResult.Closed);
                NativePromptCallbackReceiver.AlertCompleted(
                    _strategy.Alerts[1].RequestId,
                    AlertResult.Closed);

                Assert.That(laterSubscriberCount, Is.EqualTo(2));
                Assert.That(secondAlertCount, Is.EqualTo(1));
            }
            finally
            {
                NP.AlertCompleted -= throwing;
                NP.AlertCompleted -= later;
            }
        }

        [Test]
        public void Toast_NewRequestReplacesCurrentToast()
        {
            var reasons = new List<ToastDismissReason>();

            ToastHandle firstHandle = NP.ShowToast(
                new ToastOptions { Message = "First" },
                reasons.Add);
            NP.ShowToast(
                new ToastOptions { Message = "Second" },
                reasons.Add);

            Assert.That(_strategy.Toasts, Has.Count.EqualTo(2));
            Assert.That(_strategy.DismissedToastIds, Is.EqualTo(new[]
            {
                _strategy.Toasts[0].RequestId
            }));
            Assert.That(reasons, Is.EqualTo(new[] { ToastDismissReason.Replaced }));

            firstHandle.Dismiss();
            NativePromptCallbackReceiver.ToastDismissed(
                _strategy.Toasts[0].RequestId,
                ToastDismissReason.TimedOut);

            Assert.That(reasons, Is.EqualTo(new[] { ToastDismissReason.Replaced }));
        }

        [Test]
        public void ToastHandle_DismissesOnlyOnce()
        {
            int callbackCount = 0;
            ToastDismissReason reason = default;
            ToastHandle handle = NP.ShowToast(
                new ToastOptions { Message = "Toast" },
                value =>
                {
                    callbackCount++;
                    reason = value;
                });

            handle.Dismiss();
            handle.Dismiss();

            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(reason, Is.EqualTo(ToastDismissReason.ManuallyDismissed));
            Assert.That(_strategy.DismissedToastIds, Has.Count.EqualTo(1));
        }

        [Test]
        public void Toast_ReturnsTimedOutAndTappedOnlyOnce()
        {
            var reasons = new List<ToastDismissReason>();

            NP.ShowToast(new ToastOptions { Message = "Timeout" }, reasons.Add);
            string timedOutId = _strategy.Toasts[0].RequestId;
            NativePromptCallbackReceiver.ToastDismissed(
                timedOutId,
                ToastDismissReason.TimedOut);
            NativePromptCallbackReceiver.ToastDismissed(
                timedOutId,
                ToastDismissReason.Tapped);

            NP.ShowToast(new ToastOptions { Message = "Tap" }, reasons.Add);
            string tappedId = _strategy.Toasts[1].RequestId;
            NativePromptCallbackReceiver.ToastDismissed(
                tappedId,
                ToastDismissReason.Tapped);
            NativePromptCallbackReceiver.ToastDismissed(
                tappedId,
                ToastDismissReason.TimedOut);

            Assert.That(reasons, Is.EqualTo(new[]
            {
                ToastDismissReason.TimedOut,
                ToastDismissReason.Tapped
            }));
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
        }

        [UnityTest]
        public IEnumerator EditorToast_LogsAndCompletesAfterTimeout()
        {
            NativePromptRuntime.SetForTesting(new EditorNativePromptStrategy(), _dispatcher);
            LogAssert.Expect(LogType.Log, "NativePrompt Toast: Saved");
            int callbackCount = 0;
            ToastDismissReason reason = default;

            NP.ShowToast(
                new ToastOptions { Message = "Saved", Duration = 0.01f },
                value =>
                {
                    callbackCount++;
                    reason = value;
                });

            double deadline = UnityEditor.EditorApplication.timeSinceStartup + 1.0;
            while (callbackCount == 0 &&
                   UnityEditor.EditorApplication.timeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(reason, Is.EqualTo(ToastDismissReason.TimedOut));
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
        }

        [Test]
        public void Reset_DropsActiveQueuedAndScheduledCallbacks()
        {
            var queuedDispatcher = new QueuedDispatcher();
            NativePromptRuntime.SetForTesting(_strategy, queuedDispatcher);
            _strategy.ClearResetCount();
            var ownerObject = new GameObject("Prompt owner");
            PromptHandleTestOwner owner = ownerObject.AddComponent<PromptHandleTestOwner>();
            int callbackCount = 0;

            NP.ShowAlert(new AlertOptions { Content = "Active" }, _ => callbackCount++)
                .AddTo(owner);
            NP.ShowAlert(new AlertOptions { Content = "Queued" }, _ => callbackCount++);
            NP.ShowToast(new ToastOptions { Message = "Toast" }, _ => callbackCount++);
            LoadingHandle loading = NP.ShowLoading(new LoadingOptions());

            string alertId = _strategy.Alerts[0].RequestId;
            string toastId = _strategy.Toasts[0].RequestId;
            NativePromptCallbackReceiver.AlertCompleted(alertId, AlertResult.Closed);

            NativePromptRuntime.Reset();
            loading.Dismiss();
            UnityEngine.Object.DestroyImmediate(ownerObject);
            NativePromptCallbackReceiver.ToastDismissed(toastId, ToastDismissReason.TimedOut);
            queuedDispatcher.Drain();

            Assert.That(callbackCount, Is.Zero);
            Assert.That(_strategy.Alerts, Has.Count.EqualTo(1));
            Assert.That(_strategy.DismissedAlertIds, Is.Empty);
            Assert.That(_strategy.ResetCount, Is.EqualTo(1));
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
            Assert.That(NativePromptRuntime.ActiveLoadingCountForTesting, Is.Zero);
        }

        private static BottomSheetOptions CreateBottomSheet(string id)
        {
            return new BottomSheetOptions
            {
                Actions = new[]
                {
                    new BottomSheetAction { Id = id, Text = id }
                }
            };
        }

        private sealed class MockStrategy : INativePromptStrategy
        {
            internal List<AlertCall> Alerts { get; } = new List<AlertCall>();

            internal List<BottomSheetCall> BottomSheets { get; } = new List<BottomSheetCall>();

            internal List<ToastCall> Toasts { get; } = new List<ToastCall>();

            internal List<LoadingCall> Loadings { get; } = new List<LoadingCall>();

            internal List<string> DismissedToastIds { get; } = new List<string>();

            internal List<string> DismissedAlertIds { get; } = new List<string>();

            internal List<string> DismissedBottomSheetIds { get; } = new List<string>();

            internal List<string> DismissedLoadingIds { get; } = new List<string>();

            internal int ResetCount { get; private set; }

            internal Exception DismissAlertException { get; set; }

            internal Action OnDismissAlert { get; set; }

            internal Exception NextShowLoadingException { get; set; }

            internal void ClearResetCount()
            {
                ResetCount = 0;
            }

            public void ShowAlert(string requestId, AlertOptions options)
            {
                Alerts.Add(new AlertCall(requestId, options));
            }

            public void DismissAlert(string requestId)
            {
                DismissedAlertIds.Add(requestId);
                OnDismissAlert?.Invoke();
                if (DismissAlertException != null)
                {
                    throw DismissAlertException;
                }
            }

            public void ShowBottomSheet(string requestId, BottomSheetOptions options)
            {
                BottomSheets.Add(new BottomSheetCall(requestId, options));
            }

            public void DismissBottomSheet(string requestId)
            {
                DismissedBottomSheetIds.Add(requestId);
            }

            public void ShowToast(string requestId, ToastOptions options)
            {
                Toasts.Add(new ToastCall(requestId, options));
            }

            public void DismissToast(string requestId)
            {
                DismissedToastIds.Add(requestId);
            }

            public void ShowLoading(string requestId, LoadingOptions options)
            {
                Loadings.Add(new LoadingCall(requestId, options));
                Exception exception = NextShowLoadingException;
                NextShowLoadingException = null;
                if (exception != null)
                {
                    throw exception;
                }
            }

            public void DismissLoading(string requestId)
            {
                DismissedLoadingIds.Add(requestId);
            }

            public void Reset()
            {
                ResetCount++;
            }
        }

        private sealed class ImmediateDispatcher : IMainThreadDispatcher
        {
            public void Post(Action action)
            {
                action();
            }
        }

        private sealed class QueuedDispatcher : IMainThreadDispatcher
        {
            private readonly Queue<Action> _actions = new Queue<Action>();

            internal int Count => _actions.Count;

            public void Post(Action action)
            {
                _actions.Enqueue(action);
            }

            internal void Drain()
            {
                while (_actions.Count > 0)
                {
                    _actions.Dequeue().Invoke();
                }
            }
        }

        private sealed class AlertCall
        {
            internal AlertCall(string requestId, AlertOptions options)
            {
                RequestId = requestId;
                Options = options;
            }

            internal string RequestId { get; }

            internal AlertOptions Options { get; }
        }

        private sealed class BottomSheetCall
        {
            internal BottomSheetCall(string requestId, BottomSheetOptions options)
            {
                RequestId = requestId;
                Options = options;
            }

            internal string RequestId { get; }

            internal BottomSheetOptions Options { get; }
        }

        private sealed class ToastCall
        {
            internal ToastCall(string requestId, ToastOptions options)
            {
                RequestId = requestId;
                Options = options;
            }

            internal string RequestId { get; }

            internal ToastOptions Options { get; }
        }

        private sealed class LoadingCall
        {
            internal LoadingCall(string requestId, LoadingOptions options)
            {
                RequestId = requestId;
                Options = options;
            }

            internal string RequestId { get; }

            internal LoadingOptions Options { get; }
        }
    }

    internal sealed class PromptHandleTestOwner : MonoBehaviour
    {
    }
}
