# NativePrompt recipes

These short recipes start with a common application goal. For every option and
result value, see the [API reference](api.md).

The snippets assume they are used inside a `MonoBehaviour` with these namespaces:

```csharp
using System.Threading;
using System.Threading.Tasks;
using NativePrompt;
using UnityEngine;
```

Replace placeholder methods such as `DeleteSave()` with your application's code.
The handle-based examples use `AddTo(this)` so a prompt cannot outlive its
component or scene.

## Choose an API style

Use the smallest lifetime model that fits the application flow.

| Need | API style |
| --- | --- |
| React to one result without suspending the caller | Pass a callback to `Show*()` |
| Continue a sequence after the user's decision | Await `Show*Async()` |
| Dismiss one specific prompt later | Keep the handle returned by `Show*()` |
| Observe every prompt of a type | Subscribe to a static lifecycle event |
| React while any Loading request is active | Use `NP.IsLoading` and `NP.LoadingStateChanged` |

Callback-based prompts should normally use `AddTo(this)`. Awaitable prompts should
receive a caller-provided `CancellationToken` and link it to the owning
`MonoBehaviour` as shown below. Loading always remains handle-based.

## Show a one-button notice

Omit both Yes and No to show the fallback close button. Use `CloseButtonText` when
the default `"Close"` label does not fit the message.

```csharp
public void ShowOfflineNotice()
{
    NP.ShowAlert(new AlertOptions
    {
        Title = "Offline",
        Content = "Check your connection and try again.",
        CloseButtonText = "OK"
    }).AddTo(this);
}
```

See [Native Alert](api.md#native-alert) for button combinations and result values.

## Confirm a destructive action

Only continue when the user selects the affirmative button.

```csharp
public void ConfirmDelete()
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
```

See [Native Alert](api.md#native-alert) for all results and queue behavior.

## Let the user choose an action

Use stable action IDs in application logic instead of comparing the displayed
text.

```csharp
public void ShowPhotoActions()
{
    NP.ShowBottomSheet(
        new BottomSheetOptions
        {
            Title = "Photo",
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
                }
            }
        },
        result =>
        {
            if (result.IsCancelled)
            {
                return;
            }

            if (result.ActionId == "share")
            {
                SharePhoto();
            }
            else if (result.ActionId == "delete")
            {
                DeletePhoto();
            }
        })
        .AddTo(this);
}
```

See [Native Bottom Sheet](api.md#native-bottom-sheet) for action validation and
cancellation.

## Keep an unavailable action visible

Set `Enabled` to `false` when an action should remain visible but cannot currently
be selected. Keep the action ID stable when its availability changes.

```csharp
public void ShowExportActions(bool canShare)
{
    NP.ShowBottomSheet(new BottomSheetOptions
    {
        Title = "Export",
        Content = canShare
            ? "Choose where to send the file."
            : "Sharing becomes available after the file is saved.",
        Actions = new[]
        {
            new BottomSheetAction
            {
                Id = "share",
                Text = "Share",
                Enabled = canShare
            },
            new BottomSheetAction
            {
                Id = "save",
                Text = "Save to device"
            }
        }
    }, result =>
    {
        if (!result.IsCancelled)
        {
            RunExportAction(result.ActionId);
        }
    }).AddTo(this);
}
```

The cancel action remains available when other actions are disabled. See [Native
Bottom Sheet](api.md#native-bottom-sheet) for validation rules.

## Serialize prompt decisions with Awaitable

Use the optional Awaitable API when later prompts or application work depend on an
earlier result. Link the caller's token to `destroyCancellationToken` so either one
silently dismisses the active prompt and cancels the await. This also covers the
destruction of the `MonoBehaviour` during a scene unload.

```csharp
public async Awaitable DeleteWithFeedbackAsync(
    CancellationToken cancellationToken = default)
{
    // Link caller cancellation to this GameObject's lifecycle.
    using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationToken,
        destroyCancellationToken);

    AlertResult result = await NP.ShowAlertAsync(
        new AlertOptions
        {
            Title = "Delete save?",
            Content = "This cannot be undone.",
            YesButtonText = "Delete",
            NoButtonText = "Keep"
        },
        linkedCancellation.Token);

    if (result != AlertResult.Yes)
    {
        return;
    }

    DeleteSave();

    await NP.ShowToastAsync(
        new ToastOptions
        {
            Message = "Save deleted",
            Position = ToastPosition.Bottom
        },
        linkedCancellation.Token);
}
```

Each Unity `Awaitable` instance can be awaited only once. Call `Show*Async()` again
when a new request is needed. Cancellation, including destruction of the owner,
throws `OperationCanceledException`; see [Awaitable lifetime and
cancellation](api.md#awaitable-lifetime-and-cancellation) for the full contract.

## Keep a toast visible until work finishes

Disable automatic and tap dismissal, keep the handle, and close that specific toast
when the operation ends.

```csharp
private ToastHandle _workingToast;

public void BeginWork()
{
    _workingToast?.Dismiss();
    _workingToast = NP.ShowToast(new ToastOptions
    {
        Message = "Working...",
        AutoDismiss = false,
        DismissOnTap = false,
        Position = ToastPosition.Center
    }).AddTo(this);
}

public void EndWork()
{
    _workingToast?.Dismiss();
    _workingToast = null;
}
```

See [Native Toast](api.md#native-toast) for automatic dismissal and replacement
behavior.

## React when a toast is tapped

Inspect the dismissal reason when tapping the toast should open a related screen.
Automatic dismissal and replacement need no action.

```csharp
public void ShowDownloadReadyToast()
{
    NP.ShowToast(
        new ToastOptions
        {
            Message = "Download complete. Tap to open.",
            Duration = 5f,
            Position = ToastPosition.Bottom
        },
        reason =>
        {
            if (reason == ToastDismissReason.Tapped)
            {
                OpenDownloads();
            }
        })
        .AddTo(this);
}
```

See [Native Toast](api.md#native-toast) for every dismissal reason.

## Always end Loading after an asynchronous operation

Use `finally` so Loading ends after success, failure, or cancellation. `AddTo(this)`
also covers destruction of the owning component.

```csharp
public async Task SaveWithLoadingAsync(
    CancellationToken cancellationToken = default)
{
    // Cancel the operation if its caller cancels or this GameObject is destroyed.
    using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationToken,
        destroyCancellationToken);

    LoadingHandle loading = NP.ShowLoading(new LoadingOptions
    {
        BlocksInteraction = true,
        ShowsBackground = true,
        Position = LoadingPosition.Center,
        Message = "Saving..."
    }).AddTo(this);

    try
    {
        await SaveToServerAsync(linkedCancellation.Token);
    }
    finally
    {
        loading.Dismiss();
    }
}
```

See [Native Loading](api.md#native-loading) for delayed display and overlapping
requests.

## Pause application behavior while Loading is active

Use `LoadingStateChanged` for behavior such as muting audio or pausing selected
application events. It runs only when the first Loading request starts or the last
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
    // Forward this state to your audio or application event service.
    Debug.Log(isLoading ? "Pause app behavior" : "Resume app behavior");
}

private void OnSpecialEvent()
{
    if (NP.IsLoading)
    {
        return;
    }

    Debug.Log("Handle the event");
}
```

Calling the handler once from `OnEnable()` also covers a Loading request that was
already active. `IsLoading` and `LoadingStateChanged` follow request state, so they
may become active before delayed visual elements are visible.

See [Lifecycle events](api.md#lifecycle-events) for event delivery rules and the
request-specific `LoadingStarted` and `LoadingEnded` events.

## Observe prompts across the application

Use a per-request callback for local behavior. Use static lifecycle events for
cross-cutting concerns such as logs or analytics. Add optional `Tag` and `GroupId`
values when those observers need application context.

```csharp
private void OnEnable()
{
    NP.AlertCompleted += OnAlertCompleted;
}

private void OnDisable()
{
    NP.AlertCompleted -= OnAlertCompleted;
}

public void ShowTrackedAlert()
{
    NP.ShowAlert(new AlertOptions
    {
        Content = "Delete this item?",
        YesButtonText = "Delete",
        NoButtonText = "Keep",
        Tag = "delete-confirmation",
        GroupId = "inventory-screen"
    }).AddTo(this);
}

private void OnAlertCompleted(AlertCompletedEventArgs args)
{
    Debug.Log(
        $"{args.GroupId} / {args.Tag} / {args.RequestId}: {args.Result}");
}
```

`Tag` and `GroupId` are optional descriptive strings. They do not need to be unique
and cannot be used to dismiss a group of prompts.

See [Lifecycle events](api.md#lifecycle-events) and
[Optional request metadata](api.md#optional-request-metadata) for the full contract.

## Request a store review after a meaningful success

Ask at a moment when the user has enough experience to judge the application. Keep
your own eligibility rule because NativePrompt does not add a counter or cooldown.

```csharp
public void OnLevelCompleted(PlayerProgress progress)
{
    if (progress.CompletedLevelCount == 10 && !progress.ReviewRequested)
    {
        progress.ReviewRequested = true;
        SaveProgress(progress);
        NP.RequestReview();
    }
}
```

The platform may suppress the review dialog, and NativePrompt does not report
whether it appeared or a review was submitted. Do not branch application behavior
on the result. See [Store Review](api.md#store-review) for platform testing and
quota guidance.
