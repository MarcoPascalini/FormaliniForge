# VS Code: funcții populare și direcția Formalini Forge

Analiză din 10 octombrie 2026. Numărul de instalări al extensiilor este un indicator de adopție, nu o măsură directă a utilizării zilnice.

## Ce folosesc dezvoltatorii

- [Stack Overflow Developer Survey 2025](https://survey.stackoverflow.co/2025/) indică VS Code drept unul dintre cele mai folosite medii de dezvoltare în rândul respondenților.
- [Visual Studio Marketplace](https://marketplace.visualstudio.com/VSCode/) afișează printre extensiile foarte instalate Python, Pylance, Python Debugger, Jupyter și C/C++. Utilizatorii caută completare contextuală, erori în editor, depanare și unelte specifice limbajului. [Python](https://marketplace.visualstudio.com/items?itemName=ms-python.python), [Pylance](https://marketplace.visualstudio.com/items?itemName=ms-python.vscode-pylance) și [C/C++](https://marketplace.visualstudio.com/items?itemName=ms-vscode.cpptools) descriu aceste funcții; extensia C/C++ nu furnizează compilatorul.
- [Prettier](https://marketplace.visualstudio.com/items?itemName=esbenp.prettier-vscode) formatează cod și fișiere de configurare; [ESLint](https://marketplace.visualstudio.com/items?itemName=dbaeumer.vscode-eslint) marchează și poate repara probleme JavaScript/TypeScript. [GitLens](https://marketplace.visualstudio.com/items?itemName=eamodio.gitlens) dezvoltă navigarea prin istoricul Git. [Live Server](https://marketplace.visualstudio.com/items?itemName=ritwickdey.LiveServer) oferă previzualizare locală pentru site-uri.
- [Documentația VS Code](https://code.visualstudio.com/docs/configure/extensions/extension-marketplace) arată că descoperirea, instalarea, dezactivarea și recomandările de extensii sunt părți centrale ale fluxului de lucru.

## Implementat acum în Formalini Forge

- Editor cu taburi, arbore de fișiere, căutare și înlocuire, colorare pentru limbajele deja suportate, completare simplă, Git local, Build/Run și jurnal de depanare.
- Paletă de comenzi (`Ctrl+Shift+P`) pentru acțiunile principale și comenzile aduse de extensii.
- Formatare locală JSON, XML și XAML (`Shift+Alt+F`).
- Manager de extensii în meniul **Extensii**: instalare din ZIP, activare, dezactivare și eliminare. Extensiile v1 pot adăuga comenzi care introduc text și fragmente de cod cu declanșator, filtrate după extensia fișierului. Nu execută cod extern.
- Contor local pentru cereri și tokenuri, cu buget opțional de tokenuri. Este citit din metadatele răspunsurilor API; nu reprezintă soldul financiar al contului.

## Formatul extensiilor v1

Pachet ZIP cu `manifest.json` la rădăcină. `apiVersion` trebuie să fie `1`. Modelul complet se găsește în [`examples/hello-extension/manifest.json`](../examples/hello-extension/manifest.json). Arhiva este verificată înainte de instalare: număr de intrări, mărime și căi în afara folderului. Extensiile sunt dezactivate după instalare până când utilizatorul le activează. Folderul local este `%AppData%\FormaliniForge\Extensions`.

Extensiile VS Code/VSIX nu sunt compatibile direct: ele folosesc [API-ul și hostul propriu VS Code](https://code.visualstudio.com/api/get-started/extension-anatomy). Pentru suport real la mai multe limbaje și depanare, etapele următoare ar trebui să integreze protocoalele LSP și DAP, analiza proiectelor, test discovery și un terminal. [Ghidul Language Server](https://code.visualstudio.com/api/language-extensions/language-server-extension-guide) descrie baza pentru completare și diagnosticare între editoare.
