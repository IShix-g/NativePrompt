using System;
using System.Collections;
using System.Collections.Generic;
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
        public void EditorBottomSheet_LogsOptionsAndCompletesAsCancelled()
        {
            NativePromptRuntime.SetForTesting(new EditorNativePromptStrategy(), _dispatcher);
            _strategy.ClearResetCount();
            const string expectedLog =
                "NativePrompt Bottom Sheet\n" +
                "Title: Choose\n" +
                "Content: Details\n" +
                "Cancel: Close\n" +
                "Actions:\n" +
                "- delete: Delete [Destructive, Enabled=False]";
            LogAssert.Expect(LogType.Log, expectedLog);
            BottomSheetResult completed = default;
            int callbackCount = 0;

            NP.ShowBottomSheet(new BottomSheetOptions
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

            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(completed.IsCancelled, Is.True);
            Assert.That(completed.ActionId, Is.Null);
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
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
            int callbackCount = 0;

            NP.ShowAlert(new AlertOptions { Content = "Active" }, _ => callbackCount++);
            NP.ShowAlert(new AlertOptions { Content = "Queued" }, _ => callbackCount++);
            NP.ShowToast(new ToastOptions { Message = "Toast" }, _ => callbackCount++);

            string alertId = _strategy.Alerts[0].RequestId;
            string toastId = _strategy.Toasts[0].RequestId;
            NativePromptCallbackReceiver.AlertCompleted(alertId, AlertResult.Closed);

            NativePromptRuntime.Reset();
            NativePromptCallbackReceiver.ToastDismissed(toastId, ToastDismissReason.TimedOut);
            queuedDispatcher.Drain();

            Assert.That(callbackCount, Is.Zero);
            Assert.That(_strategy.Alerts, Has.Count.EqualTo(1));
            Assert.That(_strategy.ResetCount, Is.EqualTo(1));
            Assert.That(NativePromptRuntime.PendingCallbackCountForTesting, Is.Zero);
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

            internal List<string> DismissedToastIds { get; } = new List<string>();

            internal int ResetCount { get; private set; }

            internal void ClearResetCount()
            {
                ResetCount = 0;
            }

            public void ShowAlert(string requestId, AlertOptions options)
            {
                Alerts.Add(new AlertCall(requestId, options));
            }

            public void ShowBottomSheet(string requestId, BottomSheetOptions options)
            {
                BottomSheets.Add(new BottomSheetCall(requestId, options));
            }

            public void ShowToast(string requestId, ToastOptions options)
            {
                Toasts.Add(new ToastCall(requestId, options));
            }

            public void DismissToast(string requestId)
            {
                DismissedToastIds.Add(requestId);
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
    }
}
