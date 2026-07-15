# NativePrompt v0.1 release verification

Verification date: 2026-07-15

## Result

The automated verification scope for Issue #7 passed. Unity Package Manager
resolved the embedded `com.ishix.nativeprompt` package, the Runtime and Editor
assemblies compiled, and all EditMode and PlayMode tests passed.

Android and iOS player builds, device or simulator behavior, and native visual
checks are tracked separately in [Issue #9](https://github.com/IShix-g/NativePrompt/issues/9).
They are not included in the Issue #7 verification result.

## Environment

- Unity: 6000.0.78f1
- Package: `com.ishix.nativeprompt` 0.1.0
- Runtime assembly: `NativePrompt`
- Editor assembly: `NativePrompt.Editor`
- Minimum iOS version: 13
- Minimum Android API level: 24

## Verification matrix

| Check | Result | Evidence |
| --- | --- | --- |
| UPM package resolution | Passed | Unity Package Manager registered the embedded `com.ishix.nativeprompt` package during both test runs. |
| Runtime and Editor assembly compilation | Passed | Unity loaded the Runtime, Editor, and test assemblies without compilation errors. |
| EditMode tests | Passed | 24 passed, 0 failed, 0 skipped, 0 inconclusive. |
| PlayMode tests | Passed | 3 passed, 0 failed, 0 skipped, 0 inconclusive. |
| Public API and documentation consistency | Passed | README, API, architecture, sample instructions, package metadata, and CHANGELOG cover Alert, Bottom Sheet, and Toast. |
| Android external UI dependencies | Passed | The Android implementation uses SDK dialogs and views; no external UI package is declared. |
| Android/iOS player and device verification | Not run | Out of scope for Issue #7 and tracked by Issue #9. |

## Commands

EditMode:

```text
Unity -batchmode -nographics -projectPath <project> -runTests \
  -testPlatform EditMode -testResults <editmode-results.xml> \
  -logFile <editmode.log>
```

PlayMode:

```text
Unity -batchmode -nographics -projectPath <project> -runTests \
  -testPlatform PlayMode -testResults <playmode-results.xml> \
  -logFile <playmode.log>
```

The result counts above were read from the generated NUnit XML `test-run`
attributes. Test logs and XML files were local verification artifacts and are not
included in the package.

## Remaining release checks

Before publishing a release intended for device use, complete Issue #9 and record:

- Android API 24 player build and emulator or device behavior;
- iOS 13-compatible Xcode project generation, build, and simulator or device behavior;
- native appearance and interaction checks for Alert, Bottom Sheet, and Toast.
