# VS Code usage and the Formalini Forge direction

Research recorded on 10 October 2026. Extension installation counts indicate adoption, not daily use.

## Common developer needs

- The [Stack Overflow Developer Survey 2025](https://survey.stackoverflow.co/2025/) lists VS Code among the most used development environments by respondents.
- The [Visual Studio Marketplace](https://marketplace.visualstudio.com/VSCode/) prominently features Python, Pylance, Python Debugger, Jupyter, and C/C++ extensions. Developers seek contextual completion, inline diagnostics, debugging, and language-specific tools. The [Python](https://marketplace.visualstudio.com/items?itemName=ms-python.python), [Pylance](https://marketplace.visualstudio.com/items?itemName=ms-python.vscode-pylance), and [C/C++](https://marketplace.visualstudio.com/items?itemName=ms-vscode.cpptools) listings describe these features; the C/C++ extension does not include a compiler.
- [Prettier](https://marketplace.visualstudio.com/items?itemName=esbenp.prettier-vscode) formats code and configuration files. [ESLint](https://marketplace.visualstudio.com/items?itemName=dbaeumer.vscode-eslint) reports and can fix JavaScript and TypeScript problems. [GitLens](https://marketplace.visualstudio.com/items?itemName=eamodio.gitlens) improves Git history navigation. [Live Server](https://marketplace.visualstudio.com/items?itemName=ritwickdey.LiveServer) provides local website previews.
- [VS Code documentation](https://code.visualstudio.com/docs/configure/extensions/extension-marketplace) treats extension discovery, installation, disabling, and recommendations as core workflows.

## Available in Formalini Forge

- Tabbed editor, file tree, search and replace, syntax highlighting for supported languages, basic completion, local Git, build and run actions, and diagnostic logs.
- Command palette (`Ctrl+Shift+P`) for primary actions and extension commands.
- Local JSON, XML, XAML, and HTML formatting (`Shift+Alt+F`).
- Extensions manager: install ZIP packages, enable, disable, and remove them. V1 extensions add text-insertion commands and file-extension-filtered snippets; they do not execute external code.
- Local AI request and token counter with an optional budget. It uses API response metadata and does not represent the account's financial balance.

## V1 extension format

Use a ZIP package with `manifest.json` at its root and `apiVersion` set to `1`. See the [example manifest](../examples/hello-extension/manifest.json). The installer checks the number of entries, size limits, and paths that would escape the extension folder. Extensions start disabled until the user enables them. The local directory is `%AppData%\FormaliniForge\Extensions`.

VS Code/VSIX packages are not directly compatible because they use [VS Code's own API and extension host](https://code.visualstudio.com/api/get-started/extension-anatomy). Broader language support and debugging would require LSP and DAP integration, project analysis, test discovery, and a terminal. The [Language Server guide](https://code.visualstudio.com/api/language-extensions/language-server-extension-guide) describes cross-editor completion and diagnostics.
