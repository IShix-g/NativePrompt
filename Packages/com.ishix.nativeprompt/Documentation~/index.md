# NativePrompt

NativePrompt provides platform-native alerts, bottom sheets, and toasts for Unity.

## Requirements

- Unity 6000.0 or later
- iOS 13 or later
- Android API level 24 or later

The Android implementation uses Android SDK dialogs and standard views without
Material Components, Compose, or another external UI library.

## Installation

Install the package from a Git URL in Unity Package Manager:

```text
https://github.com/IShix-g/NativePrompt.git?path=/Packages/com.ishix.nativeprompt
```

## Quick start

```csharp
using NativePrompt;

NP.ShowAlert(
    new AlertOptions { Content = "NativePrompt is ready." },
    result => UnityEngine.Debug.Log($"Alert result: {result}"));
```

## Documentation

- [Public API](api.md)
- [Architecture](architecture.md)
- [Release verification](release-verification.md)

The public APIs, shared runtime coordination, and iOS, Android, and Unity Editor
strategies are implemented for v0.1.

## Sample

In Package Manager, select **Native Prompt**, open the **Samples** tab, and import
**Native Prompt Sample**. Open the imported `NativePromptSample.unity` scene and
enter Play Mode. The sample contains Alert, Bottom Sheet, and Toast configurations
and displays the latest callback result. Run it on iOS or Android to verify native
appearance.
