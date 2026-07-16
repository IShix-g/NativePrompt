# NativePrompt API

The runtime API is in the `NativePrompt` namespace and the `NativePrompt`
assembly. NativePrompt uses callbacks and does not expose a Task-based API. The
shared runtime dispatches requests to the implemented iOS, Android, or Unity Editor
strategy.

## Alert

Use `NP.ShowAlert(AlertOptions, Action<AlertResult>)` to show an alert. `Content`
is required. `Title`, `YesButtonText`, and `NoButtonText` are optional. Null, empty,
or whitespace-only optional text is treated as omitted. When both Yes and No are
omitted, NativePrompt shows one close button using `CloseButtonText` ("Close" by
default).

```csharp
using NativePrompt;
using UnityEngine;

NP.ShowAlert(
    new AlertOptions
    {
        Title = "Delete save?",
        Content = "This cannot be undone.",
        YesButtonText = "Delete",
        NoButtonText = "Keep"
    },
    result => Debug.Log($"Alert result: {result}"));
```

`AlertResult` is `Yes`, `No`, or `Closed`. Alert requests are processed in FIFO
order. On Android, tapping the backdrop or pressing Back does not dismiss an alert.
The iOS implementation uses `UIAlertController` with the alert style, Android uses
the SDK `AlertDialog`, and the Unity Editor uses `EditorUtility.DisplayDialog`.
Each platform keeps its standard theme, typography, and button placement.

## Bottom sheet

Use `NP.ShowBottomSheet(BottomSheetOptions, Action<BottomSheetResult>)` to show an
action sheet. Provide between one and three actions. Every action requires a unique,
non-empty `Id` and non-empty `Text`.

```csharp
using NativePrompt;
using UnityEngine;

NP.ShowBottomSheet(
    new BottomSheetOptions
    {
        Title = "Photo",
        Content = "Choose an action",
        Actions = new[]
        {
            new BottomSheetAction { Id = "share", Text = "Share" },
            new BottomSheetAction
            {
                Id = "delete",
                Text = "Delete",
                Style = BottomSheetActionStyle.Destructive
            }
        },
        CancelButtonText = "Cancel"
    },
    result => Debug.Log(
        result.IsCancelled ? "Cancelled" : $"Selected: {result.ActionId}"));
```

`BottomSheetAction.Enabled` defaults to `true`, and its `Style` defaults to
`Default`. A cancelled result has `IsCancelled == true` and `ActionId == null`.
Background taps and Android Back return a cancelled result.

## Toast

Use `NP.ShowToast(ToastOptions, Action<ToastDismissReason>)` to show a transient
message. `Message` is required. The default duration is 2.5 seconds, auto-dismiss
and tap-to-dismiss are enabled, and the default position is `Bottom`.

```csharp
using NativePrompt;
using UnityEngine;

ToastHandle handle = NP.ShowToast(
    new ToastOptions
    {
        Message = "Saved",
        Position = ToastPosition.Bottom
    },
    reason => Debug.Log($"Toast dismissed: {reason}"));

// The caller can dismiss it before its timeout.
handle.Dismiss();
```

`ToastDismissReason` is `TimedOut`, `Tapped`, `ManuallyDismissed`, or `Replaced`.
Calling `Dismiss()` more than once is safe. Only one toast is visible; showing a new
toast replaces the current one and completes the old callback with `Replaced`.
Toast positions account for the platform safe area.

## Argument validation contract

The runtime implementation validates arguments synchronously before creating a
native request:

- All `options` arguments must be non-null.
- `AlertOptions.Content` and `ToastOptions.Message` must contain non-whitespace text.
- A bottom sheet must contain one to three non-null actions. Action IDs and text must
  contain non-whitespace text, and IDs must be unique within the sheet.
- `ToastOptions.Duration` must be greater than zero when `AutoDismiss` is enabled.
- Invalid required text, action collections, action values, or duration values cause
  an `ArgumentException`; null top-level options cause `ArgumentNullException`.

Callbacks are optional. When supplied, a callback runs exactly once on the Unity
main thread.

## Platform behavior

| Feature | iOS | Android | Unity Editor |
| --- | --- | --- | --- |
| Alert | UIKit `UIAlertController` (alert style) | Android SDK `AlertDialog` (not cancelled by backdrop or Back) | `EditorUtility.DisplayDialog` |
| Bottom sheet | UIKit action sheet | Android SDK `Dialog` and standard views | Logs options, then returns Cancelled |
| Toast | UIKit view overlay | Android standard-view overlay | Logs the message while preserving the callback contract |

Android runtime UI does not depend on Material Components, Compose, or another
external UI library. Unsupported platforms throw `PlatformNotSupportedException`
at the facade; there is no out-of-scope fallback strategy.

See [Architecture](architecture.md) for the runtime ownership and native callback
contract.
