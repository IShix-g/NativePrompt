# Release verification

Complete these checks before publishing a release. Record results in the release
or pull request rather than in this reusable guide.

## Automated checks

- Confirm Unity Package Manager resolves the embedded `com.ishix.nativeprompt`
  package.
- Compile the Runtime, Editor, and test assemblies without errors.
- Run all EditMode and PlayMode tests.
- Confirm the package sample matches its source under `Assets/Samples`.
- Build an Android player with API level 24 as the minimum SDK.
- Generate an iOS Xcode project with iOS 13 as the deployment target.
- Check the public API, README, detailed documentation, package metadata, and
  changelog for consistency.

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

Read test counts from the generated NUnit XML `test-run` attributes. Keep test
logs and XML files as local or CI artifacts rather than adding them to the package.

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

Keep the local APK, Xcode project, build logs, and device screenshots as
verification artifacts rather than adding them to the package.

## Manual device checks

- Install and launch the Android build on a physical device.
- Verify native Alert, Bottom Sheet, and Toast appearance and interaction on
  Android.
- Build and launch on an iOS simulator or physical device.
- Verify native Alert, Bottom Sheet, and Toast appearance and interaction on iOS.
