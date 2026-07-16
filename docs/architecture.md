# NativePrompt architecture

NativePrompt separates its public C# contract, shared request coordination, and
platform UI implementations. This document describes their boundaries and
ownership.

## Assembly and namespace boundaries

| Assembly | Namespace | Responsibility |
| --- | --- | --- |
| `NativePrompt` | `NativePrompt` | Public facade and types, shared runtime coordination, internal strategy contract |
| `NativePrompt.Editor` | `NativePrompt.Editor` | Editor-only strategy and UnityEditor API usage |

`NativePrompt.Editor` references `NativePrompt`. The runtime assembly never
references the Editor assembly or `UnityEditor`, so player builds cannot expose or
pull in Editor-only types. Platform strategies and native bridge types are internal;
only `NP` and the documented option, result, action, enum, and handle types are
public.

## Request flow

```text
Caller
  -> NP facade (validate and normalize)
  -> runtime coordinator (lifetime, queue/replacement, callback-once guard)
  -> internal platform strategy
  -> native UI
  -> native callback receiver (request ID + result payload)
  -> runtime coordinator (match, remove, marshal to Unity main thread)
  -> caller callback exactly once
```

The facade owns the public signature and synchronous argument validation. It does
not contain platform UI behavior. The runtime coordinator owns request lifetime and
selects the internal strategy for Unity Editor, iOS, or Android at compile time.
Unsupported targets throw `PlatformNotSupportedException` directly; an
`OOStrategy` is intentionally not part of the design.

## Strategy boundary

The internal strategy accepts validated, normalized options plus an opaque request
ID. It starts or dismisses platform UI and reports only platform events. It does not
invoke caller callbacks, maintain the shared request registry, decide alert queue
order, or decide toast replacement.

The shared runtime coordinator is responsible for:

- assigning request IDs and matching native results to pending requests;
- retaining each callback only while its request is active;
- ensuring a callback is consumed at most once, including duplicate native events;
- dispatching completion to the Unity main thread;
- clearing pending state safely during reset, Domain Reload, or Play Mode exit;
- allowing the strategy to be replaced internally by tests without making it public.
- retaining loading requests by request ID and applying only the newest active options.

Each public handle owns a cancellation-token registration independently from the
platform strategy. `AddTo(MonoBehaviour)` registers the owner's
`destroyCancellationToken`; cancellation enters the same request-scoped silent
dispose path as `IDisposable.Dispose()`. Normal completion, manual-dismiss delivery,
silent disposal, and runtime reset all unregister the token and release request
callbacks and options.

Silent disposal removes only its request from the active slot, Alert FIFO queue, or
pending main-thread delivery set. It suppresses individual and static completion
notifications. If an active Alert is disposed, the coordinator dismisses its UI
and advances the FIFO queue. A result already claimed for delivery, or a manual
dismissal already sent to the platform, is cancelled without sending a second
platform dismissal. Platform dismissal failures are logged and cannot escape an
owner-destruction cancellation callback or prevent shared-state cleanup.

No Android strategy may add a runtime dependency on Material Components, Compose,
or another external UI library. It uses Android SDK dialogs and views.

## Queue and replacement ownership

Alert presentation is a FIFO queue owned by the shared runtime coordinator. Only
one alert is active. Completion removes the active request and starts the next
queued request, regardless of whether the native layer reports success or closure.

Toast presentation is a single replaceable slot owned by the shared runtime
coordinator. Showing a new toast dismisses the current toast, consumes its callback
with `ToastDismissReason.Replaced`, then installs the new request. A manual dismiss
travels through the handle to the coordinator; repeated dismiss requests are safe.

Bottom sheet action completion and cancellation use the same callback-once and
request-ID rules. NativePrompt does not define a public queue-control API.

Loading presentation is an ordered active-request list owned by the shared runtime.
Every `ShowLoading` adds a request and applies its normalized options to the one
native loading hierarchy. Ending a non-current request only removes that request.
Ending the current request either reapplies the next-newest options or removes the
hierarchy when no requests remain. Loading has no native-to-managed callback path.
Loading defaults, including spinner and message colors, are defined once by
`LoadingOptions`. Platform strategies receive the same normalized values and only
translate them to UIKit or Android view APIs; native implementations do not select
their own fallback colors.

## Native callback contract

Every native presentation receives an opaque request ID. Native code sends the ID
and a small result payload back to one managed callback receiver. The receiver must
not trust duplicate, late, unknown, or already-consumed IDs; those events are
ignored. Result payloads are translated to public result types by shared runtime
code rather than exposed as native string protocols.

Native callbacks may arrive off the Unity main thread. The receiver schedules the
matched completion on the Unity main thread, checks again that it has not already
been consumed, and invokes it once. Platform strategies must not bypass this path.

## Platform-specific behavior

- iOS uses UIKit alerts, action sheets, and view overlays. The action sheet supplies
  a safe popover anchor on iPad.
- Android uses SDK `AlertDialog`, `Dialog`, and standard views. It handles Back,
  backdrop taps, animation, and window insets according to each prompt contract.
- Unity Editor uses a Unity native dialog for alerts. Bottom sheets and toasts log
  their content and still complete rather than leaving requests pending.
- Loading uses an existing window/activity view hierarchy and never presents a new
  window, view controller, activity, or dialog. Its input blocker is active before
  delayed visuals, and native monotonic timers make delay independent of Unity time.

Native UI rendering mechanics remain platform-owned. Public options, results, queue behavior,
replacement behavior, callback threading, and callback cardinality remain shared
runtime responsibilities.
