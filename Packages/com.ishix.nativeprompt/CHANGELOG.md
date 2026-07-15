# Changelog

All notable changes to this package are documented in this file.

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
