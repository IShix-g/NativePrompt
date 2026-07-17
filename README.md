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

![Native Loading](docs/images/native-loading.jpg)

### Usage

Use a loading overlay while an operation such as a purchase or network request is
running. Loading does not close automatically, so keep the returned `LoadingHandle`
and call `Dismiss()` when the operation finishes. Attach this component to a
GameObject, call `BeginPurchase()` before starting the operation, and call
`EndPurchase()` from every success, failure, and cancellation path.

```csharp
using NativePrompt;
using UnityEngine;

public sealed class NativeLoadingExample : MonoBehaviour
{
    private LoadingHandle _loading;

    public void BeginPurchase()
    {
        // End an older request first if this method can be called more than once.
        EndPurchase();

        _loading = NP.ShowLoading(new LoadingOptions
        {
            BlocksInteraction = true,
            ShowsBackground = true,
            BackgroundColor = Color.white,
            BackgroundOpacity = 0.5f,
            Position = LoadingPosition.Center,
            Size = LoadingSize.Medium,
            SpinnerColor = Color.black,
            Message = "Processing...",
            MessageColor = new Color(0.33f, 0.33f, 0.33f, 1f),
            MessageFontSize = 17f,
            ShowDelaySeconds = 0.25f,
            Tag = "purchase",
            GroupId = "checkout"
        }).AddTo(this);
    }

    public void EndPurchase()
    {
        _loading?.Dismiss();
        _loading = null;
    }
}
```

### Parameters

All properties are optional, but the `LoadingOptions` object passed to
`NP.ShowLoading` must not be `null`.

| Parameter | Required | Description |
| --- | --- | --- |
| `LoadingOptions.BlocksInteraction` | No | Blocks touch and pointer input behind the overlay immediately after `ShowLoading` is called. The default is `false`. This does not block Android system Back or external input devices. |
| `LoadingOptions.ShowsBackground` | No | Shows a full-screen solid-color background with the visual elements. The default is `false`. This setting does not control input blocking. |
| `LoadingOptions.BackgroundColor` | No | Background color as a Unity `Color`. The default is `Color.white`. It is used only when `ShowsBackground` is `true`. |
| `LoadingOptions.BackgroundOpacity` | No | Background opacity from `0f` (transparent) through `1f` (opaque). The default is `0.5f`. The value must be finite and in this range; it does not change spinner or message opacity. |
| `LoadingOptions.Position` | No | Position of the spinner and message group: `Center`, `TopLeft`, `TopRight`, `BottomLeft`, or `BottomRight`. The default is `BottomRight`. Every position respects the platform safe area. |
| `LoadingOptions.Size` | No | Native spinner size: `Small`, `Medium`, or `Large`. The default is `Medium`. This changes only the spinner; on iOS, `Medium` is approximately 25 pt. |
| `LoadingOptions.SpinnerColor` | No | Spinner color as a Unity `Color`, including alpha. The default is `Color.black` on both iOS and Android. |
| `LoadingOptions.Message` | No | Optional text displayed below the spinner. The default is `null`; empty or whitespace-only text is omitted. |
| `LoadingOptions.MessageColor` | No | Message color as a Unity `Color`, including alpha. The default is dark gray: `new Color(0.33f, 0.33f, 0.33f, 1f)`. It is used only when `Message` is present. |
| `LoadingOptions.MessageFontSize` | No | Message font size. The default is `17f`; iOS interprets it as pt and Android as sp. It must be finite and greater than zero. |
| `LoadingOptions.ShowDelaySeconds` | No | Delay before the background, spinner, and message become visible. The default is `0.25f`; use `0f` for immediate display. It must be finite and zero or greater. Input blocking is not delayed. |
| `LoadingOptions.Tag` | No | Caller-defined metadata describing this request, such as `"purchase"`. It is available from the handle and lifecycle events. |
| `LoadingOptions.GroupId` | No | Caller-defined metadata grouping related requests, such as `"checkout"`. It does not automatically dismiss or control requests in the same group. |

### Notes

- `NP.ShowLoading` returns a `LoadingHandle`. Keep it until the operation ends;
  losing the handle makes explicit dismissal difficult.
- `LoadingHandle.Dismiss()` and `Dispose()` both end only that handle's request and
  are safe to call repeatedly. `AddTo(this)` also cleans it up if the owning
  component is destroyed, but normal success and failure paths should still call
  `Dismiss()` promptly.
- `ShowsBackground` and `BlocksInteraction` are independent. You can show a
  background without blocking input, or block input without showing a background.
- Input blocking begins immediately. The background, spinner, and optional message
  appear together after `ShowDelaySeconds`. Ending the request during the delay
  prevents those visual elements from appearing and removes the blocker.
- Multiple handles may coexist. The newest active handle supplies the visible
  options; ending it restores the next-newest request. Ending one handle never ends
  the others.
- Loading has no completion result or per-request callback. Subscribe to
  `NP.LoadingStarted` and `NP.LoadingEnded` when you need to observe request
  lifecycle changes. `LoadingStarted` means the request was accepted, not that a
  delayed spinner is already visible.
- Showing or dismissing only this overlay does not cause `OnApplicationPause` or
  `OnApplicationFocus` changes. Store or purchase UI may cause those events
  independently.

## Prompt Handles & Lifecycle Events

Every `Show*()` method returns a prompt-specific handle: `AlertHandle`,
`BottomSheetHandle`, `ToastHandle`, or `LoadingHandle`. All four implement `IPromptHandle` and
`IDisposable`. Calling `Dismiss()` closes the prompt and reports its normal
type-specific dismissal result to the individual callback and static completion
event. Calling `Dispose()` suppresses those result notifications. Both operations
are idempotent.

Loading has no result notification, so `LoadingHandle.Dismiss()` and `Dispose()`
have the same request-scoped effect. They do differ in the `LoadingEndedEventArgs.Reason`
reported by the Loading lifecycle event.

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

`NP` also provides eight static lifecycle events: `AlertOpened`, `AlertCompleted`,
`BottomSheetOpened`, `BottomSheetCompleted`, `ToastShown`, `ToastDismissed`,
`LoadingStarted`, and `LoadingEnded`.
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

Alert, Bottom Sheet, and Toast opened/shown events fire when the platform UI is
actually displayed. `LoadingStarted` instead reports that the central runtime accepted
the request; a short request may end before its delayed spinner becomes visible.
`LoadingEnded` reports `Dismissed`, `Disposed`, `Cancelled`, or `Reset`, plus the
remaining `ActiveCount`. An active count of zero means no Loading requests remain.
For the full lifecycle, ordering, ownership, and metadata contracts, see the
[Public API](docs/api.md).

## Sample Scene

In Package Manager, select **Native Prompt**, open the **Samples** tab, and import
**Native Prompt Sample**. Open the imported `NativePromptSample.unity` scene and
enter Play Mode.

When working directly in this repository, open
`Assets/Samples/NativePrompt/NativePromptSample.unity`. Its centered 540 x 960
viewport includes controls for Alert, Bottom Sheet, Toast, and Loading configurations,
and displays the latest result. `LoadingStarted` / `LoadingEnded` are shown with
their active-request count in the result panel and logged with the full request ID,
metadata, and end reason for device verification.

Use the sample in the Unity Editor to check API flows and on iOS or Android to
check native appearance. PlayMode tests are in `Assets/Tests/PlayMode`, and plugin
EditMode tests are in `Assets/Tests/Editor`.

## Documentation

- [Public API](docs/api.md)
- [How NativePrompt works](docs/architecture.md)

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
