# Formalini Forge

Formalini Forge is a Windows desktop prototype for building applications through a visual designer, code editor, and AI conversation. It is not yet a complete replacement for Visual Studio or Delphi. The editor, designer, and build tools work without an AI key. For AI features, you can provide an OpenAI or Gemini API key; keys are stored locally with Windows DPAPI.

The application starts in English. The Language menu retains an optional Romanian translation. Existing language preferences from earlier builds are reset to English once, then new selections are saved.

**Author:** Marco Irinel Pascalini · [formalini.com](https://formalini.com)

**License:** [MIT](LICENSE)

## Support and contact

PayPal donations: **marco@pascalini.com**.

For IT job offers, feature requests, changes, and other project inquiries, use the [contact form at formalini.com](https://formalini.com) or email **contact@formalini.com**. See [SUPPORT.md](SUPPORT.md).

## Build and run

Requirements: Windows, .NET 8 SDK, and the compiler or runtime required by the project you want to create. C/C++ projects require Visual Studio Build Tools, GCC, or Clang; Pascal projects require Free Pascal. Web projects require Node.js and npm. Compiler copies and published binaries from the development workspace are not part of this repository.

```powershell
dotnet build FormaliniForge/FormaliniForge.csproj -c Release -p:UseAppHost=false
dotnet publish FormaliniForge/FormaliniForge.csproj -c Release -o publish
```

Run `publish/FormaliniForge.exe` from its folder alongside the generated DLLs and support files. Copying the EXE alone is insufficient.

## What is implemented

- Create and open projects for C#/WPF, VB.NET/Windows Forms, native C, C++, and Pascal, plus Vue, React, and React Native/Expo projects. The designer offers controls appropriate to each project type.
- Create a Hello World application from a plain-language request without code or an AI key. General requests use the configured AI provider and should be reviewed before publishing.
- Edit files in tabs with syntax highlighting, basic autocomplete, search and replace, project-wide search, and local formatting for HTML, JSON, XML, and XAML. HTML files offer Code, Visual, and Split views.
- Drag, move, resize, copy, duplicate, and delete designer components. Edit form and component properties, including colors, and undo changes from project history.
- Use a command palette, dockable tool panels, a project file tree, a component library, an inspector, SQLite table browsing and DataGrid binding for C#/WPF, and Windows speech dictation where a recognition engine is installed.
- Build, run, and view logs inside the editor. Vue and React previews open in the integrated browser. React Native/Expo builds produce a source ZIP for a later Android or iOS build flow, not an APK or IPA.
- Add a C DLL module to a C#/WPF project with a generated P/Invoke bridge. Declare external installers for C#/WPF applications; generated applications check for dependencies, request consent, verify the installer SHA-256, and confirm installation.
- Use local Git actions and GitHub remotes through Git installed on the machine. Install v1 extensions from ZIP files to add insertion commands and snippets. See [the VS Code ecosystem notes](docs/vscode-ecosystem.md).
- Track AI requests and provider-reported token usage with an optional local token budget. This is not the provider's billing balance.

## Current limits

The designer supports the project types above, but SQLite binding, guided control actions, multiple windows, and advanced diagnostics are currently strongest in C#/WPF. C/C++/Pascal designer code is regenerated after visual changes, so custom logic belongs in separate files. The implemented hybrid path is C# plus C through a DLL.

Full import of manually edited XAML into the designer is not implemented. Debug mode provides symbols and process logs, but no breakpoints or step-by-step execution. GitHub support uses the machine's existing Git credentials; it does not provide OAuth, issues, or pull requests. Dictation requires a Windows recognition engine and has not been tested with every microphone.

Release installers are not digitally signed or validated on every Windows edition. Antivirus compatibility cannot be guaranteed. Sign and scan release artifacts, verify their provenance, and test on clean systems before distribution. AI requests can send project files to the selected provider; review sensitive files and generated code before use.

## Verification

The internal smoke test builds WPF, C, C++, Pascal, and VB examples, checks the C hybrid DLL, and generates Vue, React, and React Native projects. Vite builds and an Expo web export were verified in the development workspace. When npm encounters a local issuer certificate error, the editor retries with the Windows trusted certificate store without disabling TLS verification.

The earlier `Smoke.exe` antivirus report requires the antivirus product name, detection label, and exact flagged path to investigate. The current internal test was run through `Smoke.dll` without launching a new `Smoke.exe`.
