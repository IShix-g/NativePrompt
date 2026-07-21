# How NativePrompt works

This page explains the parts of NativePrompt that affect application code. For
method signatures, options, results, and events, see the [API reference](api.md).

## From request to result

Alert, Bottom Sheet, and Toast follow the same tracked-result flow:

1. Your code calls a callback-based `NP.Show*()` method or its optional
   `NP.Show*Async()` counterpart.
2. NativePrompt validates the options immediately. Invalid input throws before any
   platform UI is created.
3. NativePrompt creates a request ID and either returns a handle or creates a Unity
   `Awaitable<T>` controlled by a `CancellationToken`.
4. NativePrompt applies the UI type's overlap rule and forwards the request to the
   Unity Editor, iOS, or Android implementation.
5. The platform reports when the UI opens and how the user finishes it; NativePrompt
   also finishes the request when application code dismisses it.
6. NativePrompt matches the result to its request, then delivers the callback or
   completes the Awaitable before raising the lifecycle event on the Unity main
   thread.

Late, duplicate, or unknown platform results are ignored. A request callback and
its completion event are delivered at most once; an Awaitable also completes at
most once.

Loading uses the same validation, request IDs, and shared coordination, but it does
not wait for a platform result. `LoadingStarted` is raised after the platform
strategy accepts the request, which may be before delayed visuals appear. The
caller ends the request through its handle, owner lifetime, or Runtime Reset.

## What NativePrompt manages

NativePrompt keeps behavior that should be identical on every platform in its
shared C# runtime:

- option validation and normalization;
- request IDs, metadata, and handle or Awaitable lifetime;
- alert queueing, toast replacement, and loading-request ordering;
- callback, Awaitable, and lifecycle-event delivery;
- cleanup when a request is dismissed, disposed, cancelled, or reset.

The platform implementation is responsible for presenting and removing native UI,
handling platform interaction, and reporting the outcome. As a result, prompts use
the platform's native appearance while preserving the same public behavior.

## Handle lifetime

`ShowAlert()`, `ShowBottomSheet()`, `ShowToast()`, and `ShowLoading()` each return a
handle for that specific request.

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

## Awaitable lifetime

Alert, Bottom Sheet, and Toast also provide optional `Show*Async()` methods. Each
returns a new Unity `Awaitable<T>` instead of a handle and does not accept a result
callback. A Unity Awaitable can be awaited only once.

If its `CancellationToken` is already cancelled, no platform UI is created. If the
token is cancelled later, NativePrompt silently removes or dismisses that request.
In both cases, awaiting the result throws `OperationCanceledException`, and no
completion lifecycle event is raised. Link the token to a `MonoBehaviour`'s
`destroyCancellationToken` when the prompt must not outlive its owner.

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
`DismissGroup()` API. Keep the handles that your code owns, and use cancellation
tokens to control Awaitable requests.

## Callbacks, Awaitables, and events

Use a callback or Awaitable for the result of one request. Use the static events on
`NP` to observe all requests of a UI type.

- Callbacks and events run on the Unity main thread, and NativePrompt completes
  Awaitables there as well.
- The per-request callback or Awaitable completion occurs before its completion
  event.
- An exception from one callback or event subscriber is logged without blocking
  later notifications.
- Disposing Alert, Bottom Sheet, or Toast suppresses its result callback and
  completion event.
- Cancelling an Awaitable request cancels the await and suppresses its completion
  event.
- `LoadingStarted` means the loading request was accepted. It does not guarantee
  that delayed visual elements are already visible.
- `IsLoading` exposes whether any Loading request is active, and
  `LoadingStateChanged` reports only the first-start and last-end transitions.

Because `NP` events are static, listeners must unsubscribe with their own lifecycle.
Event names, arguments, and examples are listed under
[Lifecycle events](api.md#lifecycle-events).

## Runtime Reset

Unity can reset NativePrompt during runtime lifecycle transitions. Reset removes
all active, queued, and pending tracked requests and tells the selected platform
strategy to remove its UI.

- Callback-based Alert, Bottom Sheet, and Toast requests end silently.
- Active and queued Awaitable requests complete by throwing
  `OperationCanceledException`.
- Every active Loading request raises `LoadingEnded` with
  `LoadingEndReason.Reset`; the final event is followed by
  `LoadingStateChanged(false)`.
- Platform results received after Reset are ignored.

## Store Review is request-only

`NP.RequestReview()` is intentionally separate from the tracked prompt flow. It
returns immediately and has no options, request ID, handle, callback, Awaitable, or
lifecycle event. NativePrompt forwards the call to the selected platform strategy,
and the operating system or store decides whether to display anything. NativePrompt
cannot report whether a dialog appeared or a review was submitted.

Store Review is not subject to Alert queueing, Toast replacement, or Loading
ordering. The application owns when and how often it sends the request. See
[Store Review](api.md#store-review) for platform quotas, testing, and other usage
constraints.

## Supported environments

NativePrompt selects its implementation automatically at compile time.

| Environment | Prompt UI | Store Review |
| --- | --- | --- |
| iOS | UIKit alerts, action sheets, and view overlays | StoreKit through `Device.RequestStoreReview()` |
| Android | Android SDK dialogs and views; no Material Components or Compose dependency | Google Play In-App Review `2.0.2` |
| Unity Editor | iOS-inspired Game view previews loaded from Editor-only assets | Image-free simulated review card and test log |

The runtime assembly does not reference `UnityEditor`, so Editor-only types are not
included in player code. Other build targets throw `PlatformNotSupportedException`;
there is no fallback UI.

Editor previews are intended for API-flow and layout testing. They are not the
actual UIKit, StoreKit, or Google Play UI, so check final appearance and interaction
on the target iOS and Android devices. The Editor Store Review simulation also does
not report whether a platform would display or accept a review.
