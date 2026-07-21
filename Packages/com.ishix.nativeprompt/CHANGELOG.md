# Changelog

All notable changes to this package are documented in this file.

## [Unreleased]

### Added

- `NP.RequestReview()` for iOS, Android, and the Unity Editor, using StoreKit on
  iOS and Google Play In-App Review `2.0.2` on Android without application-side
  manifest or Gradle-template setup.
- A Store Review test control in the synchronized UI Toolkit samples, plus
  EditMode facade wiring and PlayMode sample layout/wiring coverage.
- Optional Unity Awaitable APIs for Alert, Bottom Sheet, and Toast, including
  cancellation and Runtime Reset behavior, folded README examples, API reference,
  and a sequential-use recipe.

- `NP.ShowLoading`, `LoadingOptions`, `LoadingHandle`, five safe-area-aware
  positions, three native spinner sizes, optional message/background, independent
  interaction blocking, and unscaled visual delay on iOS, Android, and Editor.
- Request-ID-based Loading ownership with newest-options presentation, restoration
  of the next-newest active request, reset cleanup, and EditMode coverage.
- Loading sample controls plus Unity Test Runner and device verification guidance.
- Configurable Loading message color (including alpha) and font size, with native
  pt/sp unit handling on iOS and Android.
- Shared configurable Loading spinner color with a black default on iOS and Android.
- `NP.LoadingStarted` and `NP.LoadingEnded` request-lifecycle events with request
  metadata, active-request counts, and `Dismissed`, `Disposed`, `Cancelled`, or
  `Reset` end reasons.
- `NP.IsLoading` and `NP.LoadingStateChanged` for observing the overall Loading
  state without tracking individual request counts.
- Loading sample lifecycle output in the result panel and device Console, including
  request metadata, end reason, and active-request count.

- `AlertHandle`, `BottomSheetHandle`, and the shared `IPromptHandle` contract, with
  request-scoped, idempotent dismissal on iOS, Android, and the Unity Editor.
- Unique public `RequestId` values and caller-defined `Tag` / `GroupId` metadata on
  every prompt handle and lifecycle event.
- `NP.AlertOpened`, `AlertCompleted`, `BottomSheetOpened`,
  `BottomSheetCompleted`, `ToastShown`, and `ToastDismissed` lifecycle events.
- Coverage for active and queued Alert dismissal, request isolation, metadata
  snapshots, lifecycle ordering, and exception isolation.
- `IDisposable` support for every prompt handle and `AddTo(MonoBehaviour)` lifetime
  binding through `destroyCancellationToken`.
- Request-scoped silent disposal for active, queued, and pending-delivery prompts,
  including lifecycle-registration cleanup and callback suppression coverage.

### Changed

- Store Review requests in the Unity Editor now show an interactive, image-free,
  iOS-inspired review card while retaining the request-only API semantics.
- Loading messages now appear beside corner spinners and below centered spinners,
  with consistent two-line/four-line truncation on iOS and Android.
- The sample now includes a `Now Loading...` message preset and keeps its combined
  background/input-blocking preset visible for five seconds.
- Lifecycle events now use `Action<TEventArgs>` so subscribers receive event
  arguments directly without an unused `sender` parameter.
- Alert and Bottom Sheet facade methods now return handles while remaining source
  compatible with callers that ignore their return values.
- Completion callbacks and lifecycle subscribers are isolated so an exception does
  not stop later notifications or Alert FIFO processing.
- Unity Editor presentation now uses iOS-inspired Game view previews with white
  surfaces and a dark pill-shaped Toast, loaded only from Editor assets.

## [0.1.0] - 2026-07-15

### Added

- Initial UPM package structure.
- Public `NP` facade and Alert, Bottom Sheet, and Toast API contracts.
- Runtime and Editor assembly boundaries for the `NativePrompt` and
  `NativePrompt.Editor` namespaces.
- Native Alert presentation for iOS, Android, and the Unity Editor, including
  result callbacks, configurable buttons, and non-cancellable Android dialogs.
- Native Bottom Sheet presentation for iOS and Android, plus the Editor
  cancellation behavior.
- Native Toast presentation for iOS and Android, including timeout, tap, manual
  dismissal, replacement, and safe-area positioning behavior.
- Shared FIFO Alert coordination, single-slot Toast replacement, callback-once
  handling, and main-thread callback dispatch.
- UI Toolkit sample scene and EditMode/PlayMode automated test coverage.
- API, architecture, sample, and release-verification documentation.
