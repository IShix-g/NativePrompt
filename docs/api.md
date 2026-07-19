# NativePrompt API reference

NativePrompt provides native alerts, bottom sheets, toasts, and loading overlays for
iOS and Android. The same API also works in the Unity Editor for development and
testing.

All public types are in the `NativePrompt` namespace and the `NativePrompt`
assembly.

```csharp
using NativePrompt;
```

NativePrompt keeps callbacks and handles as its primary API. Alert, Bottom Sheet,
and Toast also provide optional Unity `Awaitable` methods; there is no `Task`-based
API. Loading remains handle-based because its lifetime is owned explicitly by the
caller.

For application-flow examples, see [Recipes](recipes.md).

## API at a glance

| UI | Show method | Optional Awaitable method | Per-request callback | Handle | Lifecycle events |
| --- | --- | --- | --- | --- | --- |
| Native Alert | `NP.ShowAlert(...)` | `NP.ShowAlertAsync(...)` | `Action<AlertResult>` | `AlertHandle` | `AlertOpened`, `AlertCompleted` |
| Native Bottom Sheet | `NP.ShowBottomSheet(...)` | `NP.ShowBottomSheetAsync(...)` | `Action<BottomSheetResult>` | `BottomSheetHandle` | `BottomSheetOpened`, `BottomSheetCompleted` |
| Native Toast | `NP.ShowToast(...)` | `NP.ShowToastAsync(...)` | `Action<ToastDismissReason>` | `ToastHandle` | `ToastShown`, `ToastDismissed` |
| Native Loading | `NP.ShowLoading(...)` | None | None | `LoadingHandle` | `LoadingStarted`, `LoadingEnded`, `LoadingStateChanged` |

Use the callback passed to `Show*()` when only the caller needs the result. Use a
static lifecycle event when another part of the application needs to observe all
prompts of that type, for example for analytics or application-wide state.

Every callback-based `Show*()` call returns a handle. Keep the handle if you need to
dismiss the prompt later, or bind it to a `MonoBehaviour` with `AddTo(this)` so it
is cleaned up when its owner is destroyed.

### Awaitable lifetime and cancellation

Each `Show*Async()` call returns a new Unity `Awaitable<T>` and does not return a
handle or accept a callback. Unity `Awaitable` instances can be awaited only once;
do not cache one for multiple awaits or await the same instance from multiple
callers.

If the supplied `CancellationToken` is already cancelled, the prompt is not shown.
If it is cancelled later, NativePrompt silently removes or dismisses that request.
In both cases, awaiting the result throws `OperationCanceledException`. Runtime
Reset also removes every active or queued Awaitable request and completes each await
by throwing `OperationCanceledException`. Use a `MonoBehaviour`'s
`destroyCancellationToken` to prevent a prompt from outliving its owner.

## Quick start

```csharp
using NativePrompt;
using UnityEngine;

public sealed class DeleteButton : MonoBehaviour
{
    public void AskForConfirmation()
    {
        NP.ShowAlert(
            new AlertOptions
            {
                Title = "Delete save?",
                Content = "This cannot be undone.",
                YesButtonText = "Delete",
                NoButtonText = "Keep"
            },
            result =>
            {
                if (result == AlertResult.Yes)
                {
                    DeleteSave();
                }
            })
            .AddTo(this);
    }

    private void DeleteSave()
    {
        // Delete the save here.
    }
}
```

`AddTo(this)` silently disposes the alert if this component or its GameObject is
destroyed, including during scene unload. See [Handle lifetime](#handle-lifetime)
for the difference between dismissing and disposing.

## Lifecycle events

The callback supplied to a `Show*()` method belongs to one request. Most events on
`NP` are static `Action<TEventArgs>` events and observe every request of that UI
type. Subscribers receive the event arguments directly; there is no `sender`
parameter. `LoadingStateChanged` is an `Action<bool>` for the application-wide
Loading state.

### Event list

| Event | Event arguments | Raised when | Event-specific data |
| --- | --- | --- | --- |
| `NP.AlertOpened` | `AlertOpenedEventArgs` | The platform alert is actually displayed | None |
| `NP.AlertCompleted` | `AlertCompletedEventArgs` | The alert finishes through a button or `Dismiss()` | `Result` |
| `NP.BottomSheetOpened` | `BottomSheetOpenedEventArgs` | The platform bottom sheet is actually displayed | None |
| `NP.BottomSheetCompleted` | `BottomSheetCompletedEventArgs` | An action is selected, the sheet is cancelled, or `Dismiss()` is called | `Result` |
| `NP.ToastShown` | `ToastShownEventArgs` | The platform toast is actually displayed | None |
| `NP.ToastDismissed` | `ToastDismissedEventArgs` | The toast times out, is tapped, is replaced, or `Dismiss()` is called | `Reason` |
| `NP.LoadingStarted` | `LoadingStartedEventArgs` | The loading request is accepted | `ActiveCount` |
| `NP.LoadingEnded` | `LoadingEndedEventArgs` | The loading request is removed | `ActiveCount`, `Reason` |
| `NP.LoadingStateChanged` | `bool` | The overall Loading state changes between inactive and active | `true` while one or more requests are active |

All request-specific event argument types inherit from `PromptEventArgs` and
provide:

| Property | Meaning |
| --- | --- |
| `RequestId` | Library-generated ID for this request |
| `Tag` | Optional caller-defined metadata captured from the options |
| `GroupId` | Optional caller-defined grouping metadata captured from the options |

Completion events add either a `Result` or `Reason` property. Loading events also
include `ActiveCount`, the number of active loading requests after that request was
added or removed.

### Subscribe and unsubscribe

`NP` events are static. Always unsubscribe when the listener is disabled or
destroyed so that it does not keep receiving notifications.

```csharp
using NativePrompt;
using UnityEngine;

public sealed class PromptObserver : MonoBehaviour
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

    private void OnAlertOpened(AlertOpenedEventArgs args)
    {
        Debug.Log($"Opened alert {args.RequestId} (tag: {args.Tag})");
    }

    private void OnAlertCompleted(AlertCompletedEventArgs args)
    {
        Debug.Log($"Completed alert {args.RequestId}: {args.Result}");
    }
}
```

Identify a request with `args.RequestId`, `args.Tag`, or `args.GroupId`.

### Observe the overall Loading state

Use `NP.IsLoading` when code only needs the current state. Use
`NP.LoadingStateChanged` to react when the first Loading request starts or the last
one ends.

```csharp
private void OnEnable()
{
    NP.LoadingStateChanged += OnLoadingStateChanged;
    OnLoadingStateChanged(NP.IsLoading);
}

private void OnDisable()
{
    NP.LoadingStateChanged -= OnLoadingStateChanged;
}

private void OnLoadingStateChanged(bool isLoading)
{
    Debug.Log(isLoading ? "Loading started" : "Loading ended");
}
```

Calling the handler once from `OnEnable()` applies the current state even when the
listener becomes enabled after Loading has already started. This state describes
active requests, not whether delayed visual elements are visible.

### Delivery rules

- Callbacks and events run on the Unity main thread.
- On completion, the per-request callback runs before the corresponding static
  event.
- Each completion callback and event is delivered at most once. Late or duplicate
  platform callbacks are ignored.
- An exception from one callback or event subscriber is logged. It does not prevent
  later subscribers, queue advancement, or other completion notifications.
- Calling `Dispose()` suppresses result callbacks and result-oriented events. The
  exception is Loading: it has no result callback, and `LoadingEnded` reports the
  disposal with `Reason == LoadingEndReason.Disposed`.

`LoadingStarted` describes the request lifecycle, not visual visibility. It is
raised after the native strategy accepts the request, even if the spinner is still
waiting for `ShowDelaySeconds`. A request that ends during that delay still produces
one `LoadingStarted` event and one `LoadingEnded` event. If native startup throws,
neither event is raised. `LoadingStateChanged` is raised after the corresponding
request event and only for the `0 -> 1` and `1 -> 0` active-request transitions.

## Native Alert

```csharp
AlertHandle ShowAlert(
    AlertOptions options,
    Action<AlertResult> onCompleted = null)
```

Shows a native alert. `Content` is required. If neither a Yes nor No button is
provided, NativePrompt shows one close button.

The optional Awaitable form has the following signature:

```csharp
Awaitable<AlertResult> ShowAlertAsync(
    AlertOptions options,
    CancellationToken cancellationToken = default)
```

It returns the same `AlertResult` values as the callback form. Passing `null` for
`options` throws `ArgumentNullException`; missing or whitespace-only `Content`
throws `ArgumentException`. Cancellation and Runtime Reset throw
`OperationCanceledException` when the result is awaited. If the native prompt fails
to start, that exception is propagated by the await.

```csharp
AlertHandle alert = NP.ShowAlert(
    new AlertOptions
    {
        Title = "Delete save?",
        Content = "This cannot be undone.",
        YesButtonText = "Delete",
        NoButtonText = "Keep",
        Tag = "delete-confirmation"
    },
    result => Debug.Log($"Alert result: {result}"));
```

<details>
<summary>Optional: view the Awaitable example</summary>

```csharp
AlertResult result = await NP.ShowAlertAsync(
    new AlertOptions
    {
        Title = "Delete save?",
        Content = "This cannot be undone.",
        YesButtonText = "Delete",
        NoButtonText = "Keep",
        Tag = "delete-confirmation"
    },
    destroyCancellationToken);

Debug.Log($"Alert result: {result}");
```

</details>

### `AlertOptions`

| Property | Required | Default | Description |
| --- | --- | --- | --- |
| `Content` | Yes | — | Main alert message; must contain non-whitespace text |
| `Title` | No | None | Alert title |
| `YesButtonText` | No | None | Affirmative button text |
| `NoButtonText` | No | None | Negative button text |
| `CloseButtonText` | No | `"Close"` | Used when both Yes and No are omitted |
| `Tag` | No | `null` | Caller-defined request metadata |
| `GroupId` | No | `null` | Caller-defined grouping metadata |

Null, empty, or whitespace-only optional text is treated as omitted. Text values
are trimmed when the request is created.

### `AlertResult`

| Value | Meaning |
| --- | --- |
| `Yes` | The affirmative button was selected |
| `No` | The negative button was selected |
| `Closed` | The fallback close button was selected |
| `Dismissed` | `AlertHandle.Dismiss()` was called |

Alert requests are displayed in first-in, first-out order. A handle can dismiss
either the displayed alert or its specific waiting request. A waiting request that
is dismissed completes with `Dismissed` but does not raise `AlertOpened`.

On Android, tapping the backdrop or pressing Back does not dismiss an alert.

## Native Bottom Sheet

```csharp
BottomSheetHandle ShowBottomSheet(
    BottomSheetOptions options,
    Action<BottomSheetResult> onCompleted = null)
```

Shows an action sheet with one to three actions. Each action needs a unique ID and
display text.

The optional Awaitable form has the following signature:

```csharp
Awaitable<BottomSheetResult> ShowBottomSheetAsync(
    BottomSheetOptions options,
    CancellationToken cancellationToken = default)
```

It returns the same `BottomSheetResult` as the callback form. Passing `null` for
`options` throws `ArgumentNullException`. An action count outside one to three, a
null action, a missing or whitespace-only action ID or text, or duplicate action
IDs throws `ArgumentException`. Cancellation and Runtime Reset throw
`OperationCanceledException` when the result is awaited. If the native prompt fails
to start, that exception is propagated by the await.

```csharp
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
    result =>
    {
        if (result.IsCancelled)
        {
            Debug.Log("Cancelled");
            return;
        }

        Debug.Log($"Selected: {result.ActionId}");
    });
```

<details>
<summary>Optional: view the Awaitable example</summary>

```csharp
BottomSheetResult result = await NP.ShowBottomSheetAsync(
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
    destroyCancellationToken);

Debug.Log(result.IsCancelled ? "Cancelled" : $"Selected: {result.ActionId}");
```

</details>

### `BottomSheetOptions`

| Property | Required | Default | Description |
| --- | --- | --- | --- |
| `Actions` | Yes | Empty array | One to three non-null actions |
| `Title` | No | None | Sheet title |
| `Content` | No | None | Supporting text |
| `CancelButtonText` | No | `"Cancel"` | Cancel button text |
| `Tag` | No | `null` | Caller-defined request metadata |
| `GroupId` | No | `null` | Caller-defined grouping metadata |

### `BottomSheetAction`

| Property | Required | Default | Description |
| --- | --- | --- | --- |
| `Id` | Yes | — | Unique, non-whitespace ID returned in the result |
| `Text` | Yes | — | Non-whitespace display text |
| `Style` | No | `Default` | `Default` or `Destructive` |
| `Enabled` | No | `true` | Whether the user can select this action |

### `BottomSheetResult`

| Property | Action selected | Cancelled |
| --- | --- | --- |
| `ActionId` | ID of the selected action | `null` |
| `IsCancelled` | `false` | `true` |

The result is cancelled when the user taps the background, presses Android Back,
selects Cancel, or when `BottomSheetHandle.Dismiss()` is called.

## Native Toast

```csharp
ToastHandle ShowToast(
    ToastOptions options,
    Action<ToastDismissReason> onDismissed = null)
```

Shows a transient message. Only one toast can be visible at a time; showing a new
toast replaces the current toast.

The optional Awaitable form has the following signature:

```csharp
Awaitable<ToastDismissReason> ShowToastAsync(
    ToastOptions options,
    CancellationToken cancellationToken = default)
```

It returns the same `ToastDismissReason` values as the callback form. Passing
`null` for `options` throws `ArgumentNullException`. A missing or whitespace-only
`Message`, or a non-finite or non-positive `Duration` while `AutoDismiss` is
enabled, throws `ArgumentException`. Cancellation and Runtime Reset throw
`OperationCanceledException` when the result is awaited. If the native prompt fails
to start, that exception is propagated by the await.

```csharp
ToastHandle toast = NP.ShowToast(
    new ToastOptions
    {
        Message = "Saved",
        Position = ToastPosition.Bottom
    },
    reason => Debug.Log($"Toast dismissed: {reason}"));
```

<details>
<summary>Optional: view the Awaitable example</summary>

```csharp
ToastDismissReason reason = await NP.ShowToastAsync(
    new ToastOptions
    {
        Message = "Saved",
        Position = ToastPosition.Bottom
    },
    destroyCancellationToken);

Debug.Log($"Toast dismissed: {reason}");
```

</details>

### `ToastOptions`

| Property | Required | Default | Description |
| --- | --- | --- | --- |
| `Message` | Yes | — | Message; must contain non-whitespace text |
| `Duration` | No | `2.5f` | Display duration in seconds |
| `AutoDismiss` | No | `true` | Dismiss after `Duration` |
| `DismissOnTap` | No | `true` | Dismiss when the toast is tapped |
| `Position` | No | `Bottom` | `Top`, `Center`, or `Bottom` |
| `Tag` | No | `null` | Caller-defined request metadata |
| `GroupId` | No | `null` | Caller-defined grouping metadata |

Toast positions account for the platform safe area.

### `ToastDismissReason`

| Value | Meaning |
| --- | --- |
| `TimedOut` | `Duration` elapsed |
| `Tapped` | The user tapped the toast |
| `ManuallyDismissed` | `ToastHandle.Dismiss()` was called |
| `Replaced` | A newer toast replaced this toast |

Replacing a toast completes the old callback and raises `ToastDismissed` with
`Reason == ToastDismissReason.Replaced`.

## Native Loading

```csharp
LoadingHandle ShowLoading(LoadingOptions options)
```

Starts a request-scoped loading overlay and returns immediately. Loading has no
per-request callback; observe `LoadingStarted` and `LoadingEnded` when lifecycle
details are needed. Use `IsLoading` and `LoadingStateChanged` for the overall state.

```csharp
LoadingHandle loading = NP.ShowLoading(new LoadingOptions
{
    BlocksInteraction = true,
    ShowsBackground = true,
    BackgroundColor = Color.white,
    BackgroundOpacity = 0.5f,
    Position = LoadingPosition.BottomRight,
    Size = LoadingSize.Medium,
    SpinnerColor = Color.black,
    Message = "Processing...",
    MessageColor = new Color(0.33f, 0.33f, 0.33f, 1f),
    MessageFontSize = 17f,
    ShowDelaySeconds = 0.25f,
    Tag = "purchase",
    GroupId = "checkout"
});

try
{
    // Perform the operation.
}
finally
{
    loading.Dismiss();
}
```

### `LoadingOptions`

| Property | Default | Description |
| --- | --- | --- |
| `BlocksInteraction` | `false` | Block pointer input immediately |
| `ShowsBackground` | `false` | Show a full-screen background with the spinner |
| `BackgroundColor` | `Color.white` | Background color |
| `BackgroundOpacity` | `0.5f` | Background opacity from `0` through `1` |
| `Position` | `BottomRight` | `Center`, `TopLeft`, `TopRight`, `BottomLeft`, or `BottomRight` |
| `Size` | `Medium` | `Small`, `Medium`, or `Large` |
| `SpinnerColor` | `Color.black` | Spinner color, including alpha |
| `Message` | None | Optional text shown beside a corner spinner or below a centered spinner |
| `MessageColor` | Dark gray | Message color, including alpha |
| `MessageFontSize` | `17f` | Font size in pt on iOS and sp on Android |
| `ShowDelaySeconds` | `0.25f` | Delay before the visual elements appear |
| `Tag` | `null` | Caller-defined request metadata |
| `GroupId` | `null` | Caller-defined grouping metadata |

Whitespace-only messages are omitted. With a message, `TopLeft` and `BottomLeft`
place the spinner before the text, while `TopRight` and `BottomRight` place the text
before the spinner. `Center` places the message below the spinner. The spacing is 8
pt/dp.

Corner messages use at most two lines, and centered messages use at most four.
Longer text ends with an ellipsis on both iOS and Android. With no message, only the
spinner is created and its position is unchanged. On iOS, a medium spinner is
approximately 25 pt.

### Interaction and visual delay

Background visibility and input blocking are independent. When
`BlocksInteraction` is `true`, a transparent input blocker starts immediately. The
background, spinner, and message appear together after `ShowDelaySeconds`. The delay
uses an OS monotonic clock and is not affected by Unity `Time.timeScale`.

Ending the request during the delay prevents the visual elements from appearing
and removes the input blocker. Loading is not automatically dismissed by
`OnApplicationPause`.

### Multiple loading requests

Each call creates a separate managed request, while the native side owns one shared
loading hierarchy. The newest active request controls its appearance and interaction
settings.

- Ending an older request does not change the newest request.
- Ending the newest request restores the next-newest active request.
- Restoring an older request does not raise another `LoadingStarted` event.
- `LoadingEndedEventArgs.ActiveCount == 0` means that no loading requests remain.
- `NP.IsLoading` is `true` whenever one or more loading requests are active.

`LoadingEndedEventArgs.Reason` is one of:

| Value | Meaning |
| --- | --- |
| `Dismissed` | `LoadingHandle.Dismiss()` was called |
| `Disposed` | `LoadingHandle.Dispose()` was called |
| `Cancelled` | The owner bound through `AddTo(...)` was destroyed |
| `Reset` | The NativePrompt runtime was reset |

## Handle lifetime

`AlertHandle`, `BottomSheetHandle`, `ToastHandle`, and `LoadingHandle` implement
`IPromptHandle` and `IDisposable`.

### Common handle properties

| Property | Description |
| --- | --- |
| `RequestId` | Unique ID generated by NativePrompt for this request |
| `Tag` | Optional metadata copied from the options |
| `GroupId` | Optional grouping metadata copied from the options |

`RequestId` cannot be supplied by the caller. See
[Optional request metadata](#optional-request-metadata) for `Tag` and `GroupId`.

### `Dismiss()` compared with `Dispose()`

| Method | Alert, bottom sheet, and toast | Loading |
| --- | --- | --- |
| `Dismiss()` | Closes or removes the request, then delivers its normal dismissal callback and completion event | Ends the request and raises `LoadingEnded` with `Dismissed` |
| `Dispose()` | Silently removes the request; no result callback or result-oriented completion event | Ends the request and raises `LoadingEnded` with `Disposed` |

Both methods are idempotent: after the first effective call or normal completion,
later calls do nothing. If a platform result is already waiting for main-thread
delivery, `Dispose()` still suppresses that result and does not request a duplicate
platform dismissal.

### Bind a handle to a `MonoBehaviour`

Call `AddTo(owner)` to dispose a prompt automatically with its owner. The extension
returns the same concrete handle, so it can be chained after `Show*()`.

```csharp
AlertHandle alert = NP.ShowAlert(
        options,
        result => UpdateView(result))
    .AddTo(this);
```

`AddTo` uses `owner.destroyCancellationToken`. It reacts when the component or
GameObject is destroyed, including during scene unload. It does not react to
`OnDisable`, `enabled = false`, or GameObject deactivation. Passing a null or
already-destroyed owner throws an argument exception.

NativePrompt intentionally has no `DismissAll()`, `DismissByTag()`, or
`DismissGroup()` API. Keep the relevant handles or bind each handle to its owner.

## Optional request metadata

Every options type provides `Tag` and `GroupId`. Both are optional; leave them
unset when your application does not need request metadata.

| Property | Suggested use | Example |
| --- | --- | --- |
| `Tag` | Describe the purpose of one request | `"delete-confirmation"`, `"purchase-loading"` |
| `GroupId` | Label a screen or application flow shared by related requests | `"inventory-screen"`, `"checkout"` |

NativePrompt copies these values to the returned handle and to lifecycle event
arguments. This makes them useful for logs, analytics, or correlating a global
event with application state.

```csharp
AlertHandle alert = NP.ShowAlert(new AlertOptions
{
    Content = "Delete this item?",
    YesButtonText = "Delete",
    NoButtonText = "Keep",
    Tag = "delete-confirmation",
    GroupId = "inventory-screen"
});

Debug.Log($"{alert.RequestId}: {alert.Tag} / {alert.GroupId}");
```

`Tag` and `GroupId` are descriptive strings only. They do not need to be unique,
and NativePrompt does not filter, dismiss, or authorize requests with them. Values
are captured by `Show*()`, so changing the options object afterward does not change
an active request.

## Argument validation

Arguments are validated synchronously, before a native request is created.

| API | Invalid input |
| --- | --- |
| All `Show*()` methods | `options` is `null` |
| Alert | `Content` is null, empty, or whitespace-only |
| Bottom sheet | Fewer than one or more than three actions; a null action; missing action ID or text; duplicate action IDs |
| Toast | Missing `Message`; non-finite or non-positive `Duration` while `AutoDismiss` is enabled |
| Loading | `BackgroundOpacity` outside `0`–`1`; non-finite values; negative `ShowDelaySeconds`; non-positive `MessageFontSize`; undefined `Position` or `Size` enum value |

Null top-level options throw `ArgumentNullException`. Invalid values otherwise
throw `ArgumentException` or, for undefined Loading enum values,
`ArgumentOutOfRangeException`.

Callbacks are optional. When supplied, a completion callback runs at most once on
the Unity main thread.

## Platform behavior

| Feature | iOS | Android | Unity Editor |
| --- | --- | --- | --- |
| Alert | UIKit `UIAlertController` using alert style | SDK `AlertDialog`; backdrop and Back do not cancel | iOS-inspired Game view card |
| Bottom sheet | UIKit action sheet | SDK `Dialog` and standard views | iOS-inspired Game view action sheet |
| Toast | UIKit view overlay | Standard-view overlay | iOS-inspired Game view banner |
| Loading | Existing-window `UIView`, `UIActivityIndicatorView`, optional `UILabel` | Existing-activity `FrameLayout`, indeterminate `ProgressBar`, optional `TextView` | Game view overlay with position, size, delay, and blocking behavior |

Android runtime UI does not require Material Components, Compose, or another
external UI library. Unsupported platforms throw `PlatformNotSupportedException`;
there is no fallback UI.

See [How NativePrompt works](architecture.md) for request flow, lifetime, and
overlapping prompt behavior.
