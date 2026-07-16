# Native Prompt

Native Prompt provides platform-native alerts, bottom sheets, and toasts for Unity.

## Requirements

- Unity 6000.0 or later
- iOS 13 or later
- Android API level 24 or later

The Android implementation uses only Android SDK dialogs and views. It does not
depend on Material Components, Compose, or another external UI library.

## Installation

In Unity, open **Window > Package Management > Package Manager**, select
**Install package from git URL**, and enter:

```text
https://github.com/IShix-g/NativePrompt.git?path=/Packages/com.ishix.nativeprompt
```

You can also add the package directly to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.ishix.nativeprompt": "https://github.com/IShix-g/NativePrompt.git?path=/Packages/com.ishix.nativeprompt"
  }
}
```

## Quick start

```csharp
using NativePrompt;

NP.ShowAlert(
    new AlertOptions
    {
        Title = "Saved",
        Content = "Your changes were saved."
    },
    result => UnityEngine.Debug.Log($"Alert result: {result}"));
```

See the [Public API](Documentation~/api.md) for Bottom Sheet and Toast examples.

## Package ID

`com.ishix.nativeprompt`

## Status

The Alert, Bottom Sheet, and Toast APIs, shared runtime coordination, and iOS,
Android, and Unity Editor strategies are implemented for v0.1. See
[Release verification](Documentation~/release-verification.md) for the automated
test results and remaining platform-build verification tracked separately.

## Documentation

- [Public API](Documentation~/api.md)
- [Architecture](Documentation~/architecture.md)
- [Release verification](Documentation~/release-verification.md)

## UI Toolkit sample

Open `Assets/Samples/NativePrompt/NativePromptSample.unity` and enter Play Mode.
The centered 540 x 960 logical viewport provides buttons for every Alert, Bottom
Sheet, and Toast configuration, including manual Toast dismissal. The most recent
callback result is displayed in the sample UI.

Use the sample in the Unity Editor for API-flow checks and on iOS or Android for
native appearance checks. The accompanying PlayMode tests are located at
`Assets/Samples/NativePrompt/Tests/PlayMode`.

The plugin EditMode tests are located at `Assets/Tests/Editor` so test-only code
is kept outside the distributable package.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
