![Unity](https://img.shields.io/badge/Unity-6000.0%2B-black)

# Native Prompt

Native Prompt is a native UI plugin for Unity. It displays platform-native alerts,
bottom sheets, toasts, and loading overlays on iOS and Android through one small C# API.

![Native Prompt Top](docs/images/native-prompt-top.png)

## Table of Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Native Alert](#native-alert)
- [Native Bottom Sheet](#native-bottom-sheet)
- [Native Toast](#native-toast)
- [Native Loading](#native-loading)
- [Prompt Handles & Lifecycle Events](#prompt-handles--lifecycle-events)
- [Sample Scene](#sample-scene)
- [Documentation](#documentation)
- [License](#license)

## Requirements

- Unity 6000.0 or later
- iOS 13 or later
- Android API level 24 or later

The Android implementation uses only Android SDK dialogs and views. It does not
depend on Material Components, Compose, or another external UI library.

## Installation

The package ID is `com.ishix.nativeprompt`.

To install it with Unity Package Manager:

1. Open **Window > Package Management > Package Manager** in Unity.
2. Select **Install package from git URL** from the add menu.
3. Enter the following URL:

```text
https://github.com/IShix-g/NativePrompt.git?path=/Packages/com.ishix.nativeprompt#v1
```

You can instead add the package directly to your project's
`Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.ishix.nativeprompt": "https://github.com/IShix-g/NativePrompt.git?path=/Packages/com.ishix.nativeprompt#v1"
  }
}
```

## Quick Start

Create a C# script named `NativePromptQuickStart.cs`, attach it to a GameObject,
and connect `ShowAlert` to a UI Button's **On Click** event.

```csharp
using NativePrompt;
using UnityEngine;

public sealed class NativePromptQuickStart : MonoBehaviour
{
    public void ShowAlert()
    {
        NP.ShowAlert(
            new AlertOptions
            {
                Title = "Saved",
                Content = "Your changes were saved."
            },
            result => Debug.Log($"Alert result: {result}"))
            .AddTo(this);
    }
}
```

After selecting a button in the alert, read the callback result in the Unity
Console. In the Unity Editor, alerts use a utility window rather than the mobile
native UI. Build to an iOS or Android device to check the native appearance.

## Native Alert

![Native Alert](docs/images/native-alert.jpg)

### Usage

This component shows both a Yes/No confirmation and a one-button alert. Attach it
to a GameObject and connect its public methods to UI Buttons.

```csharp
using NativePrompt;
using UnityEngine;

public sealed class NativeAlertExample : MonoBehaviour
{
    public void ShowConfirmation()
    {
        AlertHandle handle = NP.ShowAlert(
            new AlertOptions
            {
                Title = "Delete save?",
                Content = "This cannot be undone.",
                YesButtonText = "Delete",
                NoButtonText = "Keep"
            },
            result => Debug.Log($"Alert result: {result}"));

        Debug.Log($"Alert request: {handle.RequestId}");
    }

    public void ShowCloseButton()
    {
        NP.ShowAlert(
            new AlertOptions
            {
                Title = "Notice",
                Content = "No action is required.",
                CloseButtonText = "Got it"
            },
            result => Debug.Log($"Alert result: {result}"));
    }
}
```

### Parameters

| Parameter | Required | Description |
| --- | --- | --- |
| `AlertOptions.Title` | No | Text displayed above the message. Empty or whitespace-only text is omitted. |
| `AlertOptions.Content` | Yes | The alert message. It must contain non-whitespace text. |
| `AlertOptions.YesButtonText` | No | Adds an affirmative button when specified. |
| `AlertOptions.NoButtonText` | No | Adds a negative button when specified. |
| `AlertOptions.CloseButtonText` | No | Text for the fallback close button. The default is `"Close"`. |
| `AlertOptions.Tag` | No | Caller-defined metadata describing the request. |
| `AlertOptions.GroupId` | No | Caller-defined metadata grouping related requests. |
| `onCompleted` | No | Called once with the `AlertResult` after the alert finishes. |

### Notes

- `NP.ShowAlert` returns an `AlertHandle`. Ignoring the return value is also valid.
- `AlertResult` is `Yes`, `No`, `Closed`, or `Dismissed`. Calling the handle's
  `Dismiss()` method produces `Dismissed`.
- If neither Yes nor No text is provided, Native Prompt adds the close button.
- Alert requests are displayed in FIFO order. A handle can dismiss its alert even
  while that alert is waiting in the queue.
- On Android, the Back button and backdrop taps do not close an alert.
- In the Unity Editor, alerts appear in a non-blocking utility window. Check an iOS
  or Android build for the platform-native appearance.

## Native Bottom Sheet

![Native Bottom Sheet](docs/images/native-bottom-sheet.jpg)

### Usage

Attach this component to a GameObject and connect `ShowPhotoActions` to a UI Button.

```csharp
using NativePrompt;
using UnityEngine;

public sealed class NativeBottomSheetExample : MonoBehaviour
{
    public void ShowPhotoActions()
    {
        BottomSheetHandle handle = NP.ShowBottomSheet(
            new BottomSheetOptions
            {
                Title = "Photo",
                Content = "Choose an action.",
                Actions = new[]
                {
                    new BottomSheetAction
                    {
                        Id = "share",
                        Text = "Share"
                    },
                    new BottomSheetAction
                    {
                        Id = "delete",
                        Text = "Delete",
                        Style = BottomSheetActionStyle.Destructive
                    },
                    new BottomSheetAction
                    {
                        Id = "archive",
                        Text = "Archive (unavailable)",
                        Enabled = false
                    }
                },
                CancelButtonText = "Cancel"
            },
            result => Debug.Log(
                result.IsCancelled
                    ? "Bottom sheet cancelled"
                    : $"Selected action: {result.ActionId}"));

        Debug.Log($"Bottom sheet request: {handle.RequestId}");
    }
}
```

### Parameters

| Parameter | Required | Description |
| --- | --- | --- |
| `BottomSheetOptions.Title` | No | Optional heading displayed above the actions. |
| `BottomSheetOptions.Content` | No | Optional supporting message. |
| `BottomSheetOptions.Actions` | Yes | One to three actions. Each action must be non-null and have a unique `Id`. |
| `BottomSheetOptions.CancelButtonText` | No | Cancel button text. The default is `"Cancel"`. |
| `BottomSheetOptions.Tag` | No | Caller-defined metadata describing the request. |
| `BottomSheetOptions.GroupId` | No | Caller-defined metadata grouping related requests. |
| `BottomSheetAction.Id` | Yes | Unique value returned in `BottomSheetResult.ActionId` when selected. |
| `BottomSheetAction.Text` | Yes | Non-whitespace action label. |
| `BottomSheetAction.Style` | No | `Default` or `Destructive`; the default is `Default`. |
| `BottomSheetAction.Enabled` | No | Whether the action can be selected; the default is `true`. |
| `onCompleted` | No | Called once with the `BottomSheetResult`. |

### Notes

- `NP.ShowBottomSheet` returns a `BottomSheetHandle`. Ignoring the return value is
  also valid.
- A selected action returns its `ActionId` with `IsCancelled == false`. A backdrop
  tap, Android Back, or `BottomSheetHandle.Dismiss()` returns
  `IsCancelled == true` and `ActionId == null`.
- Use `Destructive` for actions such as deletion. An action with `Enabled = false`
  remains visible but cannot be selected.
- In the Unity Editor, bottom sheets appear in a non-blocking utility window. Check
  an iOS or Android build for the platform-native appearance.

## Native Toast

![Native Toast](docs/images/native-toast.jpg)

### Usage

This component demonstrates an automatic toast and a manually dismissed toast.
Attach it to a GameObject and connect the public methods to UI Buttons.

```csharp
using NativePrompt;
using UnityEngine;

public sealed class NativeToastExample : MonoBehaviour
{
    private ToastHandle _manualToast;

    public void ShowAutomaticToast()
    {
        NP.ShowToast(
            new ToastOptions
            {
                Message = "Saved",
                Duration = 2.5f,
                AutoDismiss = true,
                DismissOnTap = true,
                Position = ToastPosition.Bottom
            },
            reason => Debug.Log($"Toast dismissed: {reason}"));
    }

    public void ShowManualToast()
    {
        _manualToast = NP.ShowToast(
            new ToastOptions
            {
                Message = "Working...",
                AutoDismiss = false,
                DismissOnTap = false,
                Position = ToastPosition.Center
            },
            reason => Debug.Log($"Toast dismissed: {reason}"));
    }

    public void DismissManualToast()
    {
        _manualToast?.Dismiss();
        _manualToast = null;
    }
}
```

### Parameters

| Parameter | Required | Description |
| --- | --- | --- |
| `ToastOptions.Message` | Yes | The toast message. It must contain non-whitespace text. |
| `ToastOptions.Duration` | When auto-dismiss is enabled | Display time in seconds. The default is `2.5`; it must be greater than zero when `AutoDismiss` is `true`. |
| `ToastOptions.AutoDismiss` | No | Dismisses the toast after `Duration`; the default is `true`. |
| `ToastOptions.DismissOnTap` | No | Allows a tap on the toast to dismiss it; the default is `true`. |
| `ToastOptions.Position` | No | `Top`, `Center`, or `Bottom`; the default is `Bottom`. |
| `ToastOptions.Tag` | No | Caller-defined metadata describing the request. |
| `ToastOptions.GroupId` | No | Caller-defined metadata grouping related requests. |
| `onDismissed` | No | Called once with the `ToastDismissReason`. |

### Notes

- `NP.ShowToast` returns a `ToastHandle`. Keep it when you need manual dismissal;
  ignoring it is valid for automatically dismissed toasts.
- `ToastDismissReason` is `TimedOut`, `Tapped`, `ManuallyDismissed`, or `Replaced`.
- Only one toast is displayed at a time. Showing a new toast replaces the previous
  one and completes its callback with `Replaced`.
- Toast positions respect the platform safe area.
- In the Unity Editor, no toast UI is displayed; Native Prompt writes the toast to
  the Console while preserving the completion callback contract. Check an iOS or
  Android build for the native appearance.

## Native Loading

Use a loading overlay for work that has no native completion result. Keep the
returned handle and end the request from every success, failure, and cancellation
path.

```csharp
using NativePrompt;
using UnityEngine;

public sealed class NativeLoadingExample : MonoBehaviour
{
    private LoadingHandle _loading;

    public void BeginPurchase()
    {
        _loading = NP.ShowLoading(new LoadingOptions
        {
            BlocksInteraction = true,
            ShowsBackground = true,
            BackgroundColor = Color.white,
            BackgroundOpacity = 0.5f,
            Position = LoadingPosition.Center,
            Size = LoadingSize.Medium,
            Message = "Processing...",
            ShowDelaySeconds = 0.25f,
            Tag = "purchase"
        }).AddTo(this);
    }

    public void EndPurchase()
    {
        _loading?.Dismiss();
        _loading = null;
    }
}
```

`ShowsBackground` and `BlocksInteraction` are independent. Interaction blocking
begins immediately, while the background, spinner, and optional message appear
together after `ShowDelaySeconds`. Ending a request during the delay prevents its
visual elements from appearing. Five safe-area-aware positions and three native
spinner sizes are available. On iOS, `Medium` renders at approximately 25 pt.
When present, the message is centered below the spinner with native spacing.

Multiple loading handles may coexist. The newest active handle supplies the visible
options; ending it restores the next-newest request. The native layer keeps only one
loading view hierarchy. `Dismiss()` and `Dispose()` both end only their own loading
request and are idempotent. Loading intentionally has no completion callback or
lifecycle event.

## Prompt Handles & Lifecycle Events

Every `Show*()` method returns a prompt-specific handle: `AlertHandle`,
`BottomSheetHandle`, `ToastHandle`, or `LoadingHandle`. All four implement `IPromptHandle` and
`IDisposable`. Calling `Dismiss()` closes the prompt and reports its normal
type-specific dismissal result to the individual callback and static completion
event. Calling `Dispose()` silently removes the prompt without either notification.
Both operations are idempotent.

Loading has no result notification, so `LoadingHandle.Dismiss()` and `Dispose()`
have the same request-scoped effect.

Use `AddTo(this)` when a prompt belongs to a `MonoBehaviour`. It returns the same
handle and silently disposes the prompt when that component or its GameObject is
destroyed, including scene unload:

```csharp
NP.ShowAlert(
    new AlertOptions
    {
        Content = "Continue?",
        YesButtonText = "Yes",
        NoButtonText = "No"
    },
    result => UpdateView(result))
    .AddTo(this);
```

`AddTo` follows destruction only. Disabling the component, setting
`enabled = false`, or deactivating its GameObject does not dispose the prompt. A
destroyed or null owner is rejected with an argument exception.

Each handle exposes a unique, library-generated `RequestId` and the optional `Tag`
and `GroupId` copied from its options. Tags and group IDs may be duplicated; use
them as descriptive metadata, not as access-control or uniqueness guarantees.

`NP` also provides six static lifecycle events: `AlertOpened`, `AlertCompleted`,
`BottomSheetOpened`, `BottomSheetCompleted`, `ToastShown`, and `ToastDismissed`.
Subscribe and unsubscribe with the owning component's lifecycle:

```csharp
using NativePrompt;
using UnityEngine;

public sealed class PromptLifecycleExample : MonoBehaviour
{
    private void OnEnable()
    {
        NP.AlertOpened += OnAlertOpened;
        NP.AlertCompleted += OnAlertCompleted;
    }

    private void OnDisable()
    {
        NP.AlertOpened -= OnAlertOpened;
        NP.AlertCompleted -= OnAlertCompleted;
    }

    public void ShowTrackedAlert()
    {
        AlertHandle handle = NP.ShowAlert(
            new AlertOptions
            {
                Content = "Continue?",
                YesButtonText = "Yes",
                NoButtonText = "No",
                Tag = "continue-confirmation",
                GroupId = "welcome-screen"
            });

        Debug.Log($"Created request: {handle.RequestId}");
    }

    private void OnAlertOpened(object sender, AlertOpenedEventArgs args)
    {
        Debug.Log($"Opened {args.RequestId} ({args.Tag}, {args.GroupId})");
    }

    private void OnAlertCompleted(object sender, AlertCompletedEventArgs args)
    {
        Debug.Log($"Completed {args.RequestId}: {args.Result}");
    }
}
```

Opened or shown events fire when the platform UI is actually displayed. Completed
or dismissed events fire once for every completion path. For the full lifecycle,
ordering, ownership, and metadata contracts, see the [Public API](docs/api.md).

## Sample Scene

In Package Manager, select **Native Prompt**, open the **Samples** tab, and import
**Native Prompt Sample**. Open the imported `NativePromptSample.unity` scene and
enter Play Mode.

When working directly in this repository, open
`Assets/Samples/NativePrompt/NativePromptSample.unity`. Its centered 540 x 960
viewport includes controls for Alert, Bottom Sheet, Toast, and Loading configurations,
including all four Loading background/input combinations, and displays the latest result.

Use the sample in the Unity Editor to check API flows and on iOS or Android to
check native appearance. PlayMode tests are in `Assets/Tests/PlayMode`, and plugin
EditMode tests are in `Assets/Tests/Editor`.

## Documentation

- [Documentation index](docs/index.md)
- [Public API](docs/api.md)
- [Architecture](docs/architecture.md)
- [Release verification](docs/release-verification.md)

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
