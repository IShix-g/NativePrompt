# Native Prompt

Native Prompt provides platform-native alerts, bottom sheets, and toasts for Unity.

## Requirements

- Unity 6000.0 or later

## Installation

In Unity, open **Window > Package Management > Package Manager**, select
**Install package from git URL**, and enter:

```text
https://github.com/IShix-g/NativePrompt.git
```

You can also add the package directly to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.ishix.nativeprompt": "https://github.com/IShix-g/NativePrompt.git"
  }
}
```

## Package ID

`com.ishix.nativeprompt`

## Status

The public API contract is defined. Runtime dispatch and platform implementations
are under development.

## Documentation

- [Public API](Documentation~/api.md)
- [Architecture](Documentation~/architecture.md)

## UI Toolkit sample

Open `Assets/Samples/NativePrompt/NativePromptSample.unity` and enter Play Mode.
The centered 540 x 960 logical viewport provides buttons for every Alert, Bottom
Sheet, and Toast configuration, including manual Toast dismissal. The most recent
callback result is displayed in the sample UI.

Use the sample in the Unity Editor for API-flow checks and on iOS or Android for
native appearance checks. The accompanying PlayMode tests are located at
`Assets/Samples/NativePrompt/Tests/PlayMode`.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
