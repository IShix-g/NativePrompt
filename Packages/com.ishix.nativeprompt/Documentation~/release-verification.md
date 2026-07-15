# NativePrompt v0.1 release verification

Verification date: 2026-07-15

## Result

The automated verification scope for Issue #7 passed. Unity Package Manager
resolved the embedded `com.ishix.nativeprompt` package, the Runtime and Editor
assemblies compiled, and all EditMode and PlayMode tests passed.

The Issue #9 Android APK build and iOS Xcode project generation also passed.
The Android APK was installed and launched on a connected physical device, and
the native content-only Alert was displayed. Bottom Sheet and Toast interaction,
and iOS simulator or device behavior, were not run as described below.

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
| Android API 24 player build | Passed | Unity generated an APK; `aapt dump badging` reported `sdkVersion:'24'` and `targetSdkVersion:'36'`. `ProjectSettings.asset` already contained `AndroidMinSdkVersion: 24`, so no settings change was required. |
| Android external UI dependencies | Passed | The Android implementation declares no external UI package. The APK contained no Google Material Components or Jetpack Compose artifact; AndroidX AppCompat resources supplied by the Unity Android player were present. |
| Android physical-device launch | Passed | The APK installed and the NativePrompt verification scene became the resumed activity on a connected Android device. |
| Android native Alert | Passed | The content-only Alert opened as a native Android dialog and displayed its content and Close action. |
| Android Bottom Sheet and Toast | Not run | Build and launch were confirmed, but these interactions were not exercised during the available physical-device session. |
| iOS 13 Xcode project generation | Passed | Unity generated the Xcode project and its application and UnityFramework targets use `IPHONEOS_DEPLOYMENT_TARGET = 13.0`. |
| iOS simulator or physical-device behavior | Not run | No iOS Simulator was booted and no iOS device was used during this verification. |

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

Android player build:

```text
NATIVEPROMPT_ANDROID_BUILD_PATH=<output.apk> Unity -batchmode -nographics \
  -quit -projectPath <project> \
  -executeMethod NativePrompt.Editor.NativePromptBuild.BuildAndroid \
  -logFile <android-build.log>
```

iOS Xcode project generation:

```text
NATIVEPROMPT_IOS_BUILD_PATH=<output-directory> Unity -batchmode -nographics \
  -quit -projectPath <project> \
  -executeMethod NativePrompt.Editor.NativePromptBuild.BuildIos \
  -logFile <ios-build.log>
```

The local APK, Xcode project, build logs, and device screenshots are verification
artifacts and are not included in the package.

## Remaining release checks

Before publishing a release intended for device use, complete the remaining
manual checks:

- Android Bottom Sheet and Toast appearance and interaction;
- iOS simulator or device build, launch, and native Alert, Bottom Sheet, and
  Toast appearance and interaction.
