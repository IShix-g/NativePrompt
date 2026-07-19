# NativePrompt recipes

These short recipes start with a common application goal. For every option and
result value, see the [API reference](api.md).

The snippets assume they are used inside a `MonoBehaviour` with these namespaces:

```csharp
using System.Threading.Tasks;
using NativePrompt;
using UnityEngine;
```

Replace placeholder methods such as `DeleteSave()` with your application's code.
The examples use `AddTo(this)` so a prompt cannot outlive its component or scene.

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
                new BottomSheetAction { Id = "share", Text = "Share" },
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

## Serialize prompt decisions with Awaitable

Use the optional Awaitable API when later prompts or application work depend on an
earlier result. Passing `destroyCancellationToken` silently dismisses the active
prompt and cancels the await if this `MonoBehaviour` is destroyed, including during
a scene unload.

```csharp
public async Awaitable DeleteWithFeedbackAsync()
{
    AlertResult result = await NP.ShowAlertAsync(
        new AlertOptions
        {
            Title = "Delete save?",
            Content = "This cannot be undone.",
            YesButtonText = "Delete",
            NoButtonText = "Keep"
        },
        destroyCancellationToken);

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
        destroyCancellationToken);
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

## Always end Loading after an asynchronous operation

Use `finally` so Loading ends after success, failure, or cancellation. `AddTo(this)`
also covers destruction of the owning component.

```csharp
public async Task SaveWithLoadingAsync()
{
    LoadingHandle loading = NP.ShowLoading(new LoadingOptions
    {
        BlocksInteraction = true,
        ShowsBackground = true,
        Position = LoadingPosition.Center,
        Message = "Saving..."
    }).AddTo(this);

    try
    {
        await SaveToServerAsync();
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
