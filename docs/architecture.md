# How NativePrompt works

This page explains the parts of NativePrompt that affect application code. For
method signatures, options, results, and events, see the [API reference](api.md).

## From request to result

Every prompt follows the same basic flow:

1. Your code calls an `NP.Show*()` method.
2. NativePrompt validates the options immediately. Invalid input throws before any
   platform UI is created.
3. NativePrompt creates a request ID, returns a handle, and forwards the request to
   the Unity Editor, iOS, or Android implementation.
4. The platform reports when the UI opens and how it finishes.
5. NativePrompt matches the result to its request and delivers the callback and
   lifecycle event on the Unity main thread.

Late, duplicate, or unknown platform results are ignored. A request callback and
its completion event are delivered at most once.

## What NativePrompt manages

NativePrompt keeps behavior that should be identical on every platform in its
shared C# runtime:

- option validation and normalization;
- request IDs, metadata, and handle lifetime;
- alert queueing, toast replacement, and loading-request ordering;
- callback and lifecycle-event delivery;
- cleanup when a request is dismissed, disposed, cancelled, or reset.

The platform implementation is responsible for presenting and removing native UI,
handling platform interaction, and reporting the outcome. As a result, prompts use
the platform's native appearance while preserving the same public behavior.

## Handle lifetime

Every `Show*()` method returns a handle for that specific request.

| Action | Effect |
| --- | --- |
| Keep the handle | Dismiss or inspect that request later |
| `handle.Dismiss()` | End the request and deliver its normal dismissal result |
| `handle.Dispose()` | Silently remove Alert, Bottom Sheet, or Toast without a result callback |
| `handle.AddTo(owner)` | Dispose or cancel the request when the `MonoBehaviour` is destroyed |

Loading has no per-request result callback. Both `Dismiss()` and `Dispose()` end the
request, and `LoadingEnded` reports why it ended.

Calls to `Dismiss()` and `Dispose()` are idempotent. Once a request has finished,
later calls do nothing. Prefer `AddTo(this)` when the prompt should not outlive a
component or scene.

## When requests overlap

Each UI type has an explicit ownership rule:

| UI | Behavior |
| --- | --- |
| Alert | One alert is active; later alerts wait in first-in, first-out order |
| Bottom sheet | Each request is tracked separately; no shared queue or replacement rule is added |
| Toast | One toast is active; a new toast replaces the previous one with `Replaced` |
| Loading | Every call adds a request; the newest active request controls the one shared loading view |

Ending the newest Loading request restores the next-newest request. Ending an older
Loading request does not change the currently visible configuration.

NativePrompt intentionally has no public `DismissAll()`, `DismissByTag()`, or
`DismissGroup()` API. Keep the handles that your code owns.

## Callbacks and events

Use a callback for the result of one request. Use the static events on `NP` to
observe all requests of a UI type.

- Callbacks and events run on the Unity main thread.
- The per-request callback runs before its completion event.
- An exception from one callback or event subscriber is logged without blocking
  later notifications.
- Disposing Alert, Bottom Sheet, or Toast suppresses its result callback and
  completion event.
- `LoadingStarted` means the loading request was accepted. It does not guarantee
  that delayed visual elements are already visible.
- `IsLoading` exposes whether any Loading request is active, and
  `LoadingStateChanged` reports only the first-start and last-end transitions.

Because `NP` events are static, listeners must unsubscribe with their own lifecycle.
Event names, arguments, and examples are listed under
[Lifecycle events](api.md#lifecycle-events).

## Supported environments

NativePrompt selects its implementation automatically at compile time.

| Environment | Implementation |
| --- | --- |
| iOS | UIKit alerts, action sheets, and view overlays |
| Android | Android SDK dialogs and views; no Material Components or Compose dependency |
| Unity Editor | Utility windows for Alert and Bottom Sheet; log-based Toast and Loading behavior |

The runtime assembly does not reference `UnityEditor`, so Editor-only types are not
included in player code. Other build targets throw `PlatformNotSupportedException`;
there is no fallback UI.

Editor behavior is intended for API-flow testing, not visual approval. Check final
appearance and interaction on the target iOS and Android devices.
