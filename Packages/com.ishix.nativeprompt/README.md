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

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
