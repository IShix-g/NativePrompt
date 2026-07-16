# Changelog

All notable changes to this package are documented in this file.

## [Unreleased]

### Added

- `NP.ShowLoading`, `LoadingOptions`, `LoadingHandle`, five safe-area-aware
  positions, three native spinner sizes, optional message/background, independent
  interaction blocking, and unscaled visual delay on iOS, Android, and Editor.
- Request-ID-based Loading ownership with newest-options presentation, restoration
  of the next-newest active request, reset cleanup, and EditMode coverage.
- Loading sample controls plus Unity Test Runner and device verification guidance.
- Configurable Loading message color (including alpha) and font size, with native
  pt/sp unit handling on iOS and Android.
- Shared configurable Loading spinner color with a black default on iOS and Android.

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

- Alert and Bottom Sheet facade methods now return handles while remaining source
  compatible with callers that ignore their return values.
- Completion callbacks and lifecycle subscribers are isolated so an exception does
  not stop later notifications or Alert FIFO processing.
- Unity Editor Alert and Bottom Sheet presentation now uses dismissible,
  non-blocking utility windows.

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
