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

See [Alert](api.md#alert) for all results and queue behavior.

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

See [Bottom sheet](api.md#bottom-sheet) for action validation and cancellation.

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

See [Toast](api.md#toast) for automatic dismissal and replacement behavior.

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

See [Loading](api.md#loading) for delayed display and overlapping requests.

## React when Loading starts and ends

Use `ActiveCount` to react only when the application's overall Loading state
changes. A start count of `1` means the first request became active. An end count of
`0` means the last active request ended.

```csharp
private void OnEnable()
{
    NP.LoadingStarted += OnLoadingStarted;
    NP.LoadingEnded += OnLoadingEnded;
}

private void OnDisable()
{
    NP.LoadingStarted -= OnLoadingStarted;
    NP.LoadingEnded -= OnLoadingEnded;
}

private void OnLoadingStarted(object _, LoadingStartedEventArgs args)
{
    if (args.ActiveCount == 1)
    {
        Debug.Log("Loading started");
    }
}

private void OnLoadingEnded(object _, LoadingEndedEventArgs args)
{
    if (args.ActiveCount == 0)
    {
        Debug.Log("Loading ended");
    }
}
```

Register this observer before creating Loading requests so it receives their start
events. Replace the logs with logic that should run when the overall Loading state
starts or ends. These events follow the request lifecycle, so the start event may
occur before delayed visual elements are visible.

See [Lifecycle events](api.md#lifecycle-events) for `ActiveCount` and event delivery
rules.

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

private void OnAlertCompleted(object _, AlertCompletedEventArgs args)
{
    Debug.Log(
        $"{args.GroupId} / {args.Tag} / {args.RequestId}: {args.Result}");
}
```

`Tag` and `GroupId` are optional descriptive strings. They do not need to be unique
and cannot be used to dismiss a group of prompts.

See [Lifecycle events](api.md#lifecycle-events) and
[Optional request metadata](api.md#optional-request-metadata) for the full contract.
