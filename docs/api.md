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

AlertHandle alert = NP.ShowAlert(
    new AlertOptions
    {
        Title = "Delete save?",
        Content = "This cannot be undone.",
        YesButtonText = "Delete",
        NoButtonText = "Keep"
    },
    result => Debug.Log($"Alert result: {result}"));

// Safe to call more than once; only the first call has an effect.
alert.Dismiss();
```

`AlertResult` is `Yes`, `No`, `Closed`, or `Dismissed`. Alert requests are processed in FIFO
order. On Android, tapping the backdrop or pressing Back does not dismiss an alert.
The iOS implementation uses `UIAlertController` with the alert style, Android uses
the SDK `AlertDialog`, and the Unity Editor uses a non-blocking utility window.
Each platform keeps its standard theme, typography, and button placement.
An `AlertHandle` can dismiss either the displayed alert or that specific alert while
it is waiting in the FIFO queue. A waiting alert dismissed before display does not
raise `NP.AlertOpened`.

## Bottom sheet

Use `NP.ShowBottomSheet(BottomSheetOptions, Action<BottomSheetResult>)` to show an
action sheet. Provide between one and three actions. Every action requires a unique,
non-empty `Id` and non-empty `Text`.

```csharp
using NativePrompt;
using UnityEngine;

BottomSheetHandle sheet = NP.ShowBottomSheet(
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

sheet.Dismiss();
```

`BottomSheetAction.Enabled` defaults to `true`, and its `Style` defaults to
`Default`. A cancelled result has `IsCancelled == true` and `ActionId == null`.
Background taps and Android Back return a cancelled result.
Calling `BottomSheetHandle.Dismiss()` also returns the existing cancelled result.

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

## Loading

Use `NP.ShowLoading(LoadingOptions)` to start a request-scoped native loading
overlay. The API returns immediately with a `LoadingHandle`; loading has no callback,
completion result, or static lifecycle event.

```csharp
LoadingHandle loading = NP.ShowLoading(new LoadingOptions
{
    BlocksInteraction = true,
    ShowsBackground = true,
    BackgroundColor = Color.white,
    BackgroundOpacity = 0.5f,
    Position = LoadingPosition.BottomRight,
    Size = LoadingSize.Medium,
    Message = "Processing...",
    ShowDelaySeconds = 0.25f,
    Tag = "purchase",
    GroupId = "checkout"
});

// Safe to call repeatedly. Dispose() has the same effect for Loading.
loading.Dismiss();
```

Defaults are `BlocksInteraction = false`, `ShowsBackground = false`, a white
background with `0.5` opacity, `BottomRight`, `Medium`, no message, and a `0.25`
second visual delay. `LoadingPosition` supports `Center`, `TopLeft`, `TopRight`,
`BottomLeft`, and `BottomRight`; `LoadingSize` supports `Small`, `Medium`, and
`Large`. Whitespace-only messages are omitted.

Background visibility and pointer-input blocking are independent. If blocking is
enabled, a transparent blocker starts immediately. The background, spinner, and
message become visible together after the configured delay, using an OS monotonic
clock rather than Unity `Time.timeScale`. Ending the request first cancels visual
presentation and removes the blocker.

Each call creates a distinct managed request, but the native implementation owns
one loading hierarchy. The newest active request controls the hierarchy. Ending an
older request leaves the newest untouched; ending the newest reapplies the
next-newest active request. Loading is not dismissed on `OnApplicationPause`.

## Handles and identity metadata

`AlertHandle`, `BottomSheetHandle`, `ToastHandle`, and `LoadingHandle` implement `IPromptHandle` and
`IDisposable`. Every handle exposes a library-generated `RequestId`, the optional
`Tag` and `GroupId` supplied in its options, and two idempotent completion paths:

- `Dismiss()` closes the prompt and delivers the existing type-specific dismissal
  result to the individual callback and static completion event.
- `Dispose()` silently removes the prompt or waiting request. It does not invoke
  the individual callback or static completion event.

For `LoadingHandle`, both methods simply end that handle's request because Loading
has no callback or completion event.

```csharp
AlertOptions options = new AlertOptions
{
    Content = "Delete this item?",
    YesButtonText = "Delete",
    NoButtonText = "Keep",
    Tag = "delete-confirmation",
    GroupId = "inventory-screen"
};

AlertHandle alert = NP.ShowAlert(options);
Debug.Log($"Request: {alert.RequestId}, tag: {alert.Tag}, group: {alert.GroupId}");
```

For automatic ownership, call `AddTo(MonoBehaviour owner)`. The extension binds
the handle to `owner.destroyCancellationToken` and returns the same concrete handle:

```csharp
NP.ShowAlert(options, result => UpdateView(result)).AddTo(this);
```

Destroying the component or GameObject, or unloading its scene, silently disposes
the prompt. `AddTo` does not react to `OnDisable`, `enabled = false`, or GameObject
deactivation. Passing a null or already-destroyed owner throws an argument
exception instead of silently leaving the handle unmanaged.

After a handle completes or is disposed, later `Dismiss()` and `Dispose()` calls
are no-ops. Late platform callbacks are ignored. If a platform result or manual
dismissal is waiting for main-thread delivery, `Dispose()` still suppresses its
individual callback and static completion event; it does not issue a duplicate
platform dismissal.

`RequestId` is unique per prompt request and cannot be supplied through options.
`Tag` and `GroupId` may be duplicated; they are descriptive metadata, not access
control tokens or uniqueness guarantees. Values are captured by `Show*()`, so later
changes to the options object do not affect an in-progress prompt.

NativePrompt intentionally has no `DismissAll()`, `DismissByTag()`, or
`DismissGroup()` API. Keep handles for explicit manual management, or bind each
handle to its owning component with `AddTo`. `GroupId` can describe ownership, but
does not itself grant control of other prompts.

## Lifecycle events

`NP` exposes type-specific static lifecycle events:

- `AlertOpened` and `AlertCompleted`
- `BottomSheetOpened` and `BottomSheetCompleted`
- `ToastShown` and `ToastDismissed`

Opened/shown events occur only after the platform UI is actually displayed.
Completed/dismissed events cover user interaction, manual dismissal, Toast timeout,
tap, and replacement. Event args expose the same `RequestId`, `Tag`, and `GroupId`
as the handle, plus `Result` or `Reason` for completion events.

```csharp
private void OnEnable()
{
    NP.AlertCompleted += OnAlertCompleted;
}

private void OnDisable()
{
    // NP events are static: always unsubscribe with the owner's lifecycle.
    NP.AlertCompleted -= OnAlertCompleted;
}

private void OnAlertCompleted(object sender, AlertCompletedEventArgs args)
{
    Debug.Log($"{args.RequestId}: {args.Result}");
}
```

Individual callbacks and lifecycle events run on the Unity main thread. For a
completion, NativePrompt invokes the individual callback first and then the static
event. An exception from one callback or subscriber is logged and does not prevent
later subscribers, queue advancement, or other completion notifications.

## Argument validation contract

The runtime implementation validates arguments synchronously before creating a
native request:

- All `options` arguments must be non-null.
- `AlertOptions.Content` and `ToastOptions.Message` must contain non-whitespace text.
- A bottom sheet must contain one to three non-null actions. Action IDs and text must
  contain non-whitespace text, and IDs must be unique within the sheet.
- `ToastOptions.Duration` must be greater than zero when `AutoDismiss` is enabled.
- `LoadingOptions.BackgroundOpacity` must be finite and between zero and one.
- `LoadingOptions.ShowDelaySeconds` must be finite and zero or greater.
- Invalid required text, action collections, action values, or duration values cause
  an `ArgumentException`; null top-level options cause `ArgumentNullException`.

Callbacks are optional. When supplied, a callback runs exactly once on the Unity
main thread.

## Platform behavior

| Feature | iOS | Android | Unity Editor |
| --- | --- | --- | --- |
| Alert | UIKit `UIAlertController` (alert style) | Android SDK `AlertDialog` (not cancelled by backdrop or Back) | Non-blocking utility window |
| Bottom sheet | UIKit action sheet | Android SDK `Dialog` and standard views | Non-blocking utility window |
| Toast | UIKit view overlay | Android standard-view overlay | Logs the message while preserving the callback contract |
| Loading | Existing-window `UIView`, `UIActivityIndicatorView`, optional `UILabel` | Existing-activity `FrameLayout`, indeterminate `ProgressBar`, optional `TextView` | Logs when visual presentation begins |

Android runtime UI does not depend on Material Components, Compose, or another
external UI library. Unsupported platforms throw `PlatformNotSupportedException`
at the facade; there is no out-of-scope fallback strategy.

See [Architecture](architecture.md) for the runtime ownership and native callback
contract.
