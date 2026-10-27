<p align="center"><img src="assets/SpokenFlow.png" width="112" alt="SpokenFlow bilingual speech bubbles"></p>

# SpokenFlow

See natural, concise translations as you type on Windows. Keep your existing input method, choose two of seven languages, and translate in either direction while typing or selecting text.

[中文](README.md) · [Usage](docs/USAGE.md) · [Compatibility](docs/COMPATIBILITY.md) · [Contributing](CONTRIBUTING.md) · [Help](SUPPORT.md)

Version 0.1.0-alpha.1 is an early preview with incomplete application compatibility. Its source-available license prohibits commercial use; it is not GPL or an OSI open-source license.

## Features

- Choose two of Chinese, English, French, German, Russian, Korean and Japanese. Typing and selection translation share the pair and infer the direction automatically.
- Stream a translation after text is committed and typing briefly pauses. Previous text remains visible during updates, but stale results cannot replace input.
- Move, resize and scroll the popup, adjust opacity and background color, or collapse it to a small icon. Collapsing pauses requests; restoring reads the latest input.
- Use `Ctrl+Alt+E` to replace a verified input scope with a completed translation. Use `Ctrl+Alt+Z` to attempt restoration.
- Show a copyable translation near selected text. If accessibility capture fails, an optional copy fallback attempts to preserve and restore the clipboard.

The seven languages apply to translation. The UI supports the same seven languages, defaults to English and remembers the selection. Language lists use native names. UI language and translation pair are independent. Short words, names and mixed-language text can be ambiguous; see [language settings](docs/LANGUAGES.md).

## Getting started

Requires Windows 10/11 x64, .NET Framework 4.8 and access to a model service. This release targets Windows only.

1. Download the Windows archive from the [release page](https://github.com/ShWenjun/SpokenFlow/releases/tag/v0.1.0-alpha.1), extract it and run `SpokenFlow.exe`, or build from source.
2. Use the compatibility inspection button in the playground or a nonsensitive document to check the captured scope. Inspection makes no API calls.
3. Enter the service URL, model and matching API key, select two languages, then save and enable translation.
4. Type in a supported field. Enable the separate selection toggle when needed.

This is a portable, unsigned application without an installer. DeepSeek with `deepseek-flash` is the default. Other OpenAI Chat Completions-compatible services and local endpoints can be configured; see [model services](docs/PROVIDERS.md). Model access and charges depend on the provider.

Online models require your own API key. Translation sends captured input or selected text to the configured service. Read the [privacy policy](PRIVACY.md), and keep credentials and private messages out of issues.

## Compatibility

OCR is not available. Text capture depends on both the input method and the target application, so support for every application cannot be guaranteed. See the [compatibility matrix](docs/COMPATIBILITY.md) for each application's scope and evidence.

## Build from source

Run in Windows PowerShell from the repository root.

```powershell
.\build.ps1
.\scripts\test.ps1 -NoBuild
.\artifacts\bin\SpokenFlow.exe
```

The build uses the installed .NET Framework C# compiler. It needs no Python, Node.js, NuGet, API key or network access. See the [development guide](docs/DEVELOPMENT.md) for structure and debugging.

```powershell
# Create local binary/source archives and SHA-256 checksums without uploading
.\scripts\package.ps1
```

Offline checks cover response parsing, stale-result rejection, selection capture, clipboard protection and language settings. See [testing](docs/TESTING.md) for scope and gaps.

## Contributing

Reproducible compatibility reports, translation issues, tests and focused fixes are welcome. Discuss larger changes first. See the [roadmap](docs/ROADMAP.md) and [contribution guide](CONTRIBUTING.md).

## License and branding

The [LICENSE](LICENSE) permits personal noncommercial use, noncommercial teaching and noncommercial research. It prohibits commercial use, including internal business use, employer work and paid individual work. Modified distributions must provide recipients with complete corresponding source and retain the noncommercial restrictions. The full license terms control.

The personal bird mark and application icon are reserved separately under the [branding policy](BRANDING.md). This project is not affiliated with or endorsed by Microsoft, Tencent or DeepSeek.
