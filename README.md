# Formalini Forge — prototip Windows

Editor desktop WPF pentru proiecte Windows conduse prin conversație. Această versiune este un prototip, nu un înlocuitor complet pentru Visual Studio sau Delphi.

La prima pornire este selectat modul **Fără AI**. Nu este necesară nicio cheie pentru a crea și deschide proiecte, a edita codul sau interfața și a compila. Pentru conversația cu agentul, selectați OpenAI sau Gemini și adăugați cheia API corespunzătoare.

## Starea verificării

Pachetul local verificat este în folderul `release-2026-10-11-unsaved-guard` din workspace-ul de dezvoltare; repository-ul GitHub conține sursele, nu binarele publicate. Pentru a crea un pachet propriu, folosiți `dotnet publish FormaliniForge/FormaliniForge.csproj -c Release -o publish` și porniți `FormaliniForge.exe` **din folderul `publish`**, împreună cu DLL-urile și celelalte fișiere alăturate; copierea doar a EXE-ului nu este suficientă. Testul intern construiește proiecte WPF, C, C++, Pascal și VB, verifică modulul hibrid C până la publicarea DLL-ului și generează proiecte Vue, React și React Native. Buildurile Vite pentru Vue/React și exportul web Expo pentru proiectul React Native generat au fost verificate. Dacă npm raportează `UNABLE_TO_GET_ISSUER_CERT_LOCALLY` sau erori similare, editorul reîncearcă folosind certificatele de încredere Windows.

Pentru verificarea sursei fără publicare sau lansare de EXE:

```powershell
dotnet build FormaliniForge/FormaliniForge.csproj -c Release -p:UseAppHost=false
```

## Funcții implementate

- Paletă de comenzi (`Ctrl+Shift+P`), formatare JSON/XML/XAML (`Shift+Alt+F`) și extensii v1 instalate din ZIP pentru comenzi de inserare și fragmente de cod. Detalii și surse în [analiza ecosistemului VS Code](docs/vscode-ecosystem.md).
- Contor AI pentru cereri și tokenuri raportate de API, separat pentru OpenAI și Gemini. Poți introduce un buget de tokenuri; acesta nu este soldul real al furnizorului.

- Proiecte C#/WPF, C, C++, Pascal și VB.NET, structură de fișiere și editor în taburi cu colorare sintactică, completare locală, căutare și înlocuire. Designerul creează ferestre Windows native pentru C/C++/Pascal, formulare Windows Forms pentru VB și ferestre WPF pentru C#.
- La închiderea editorului, fișierele modificate trebuie salvate: dialogul oferă salvare sau revenire în aplicație. Închiderea este anulată dacă salvarea eșuează.
- Fișierele HTML generate au structură și indentare lizibile. HTML-ul existent pe un singur rând este formatat la deschidere. Taburile HTML au modurile Cod, Vizual și Împărțit; poți edita textul vizual și insera titluri, paragrafe, butoane, tabele, linkuri și imagini. Pentru shell-ul React/Vue, interfața aplicației se editează în Designer, iar HTML-ul de intrare în modul Cod.
- Proprietățile de culoare au selector vizual; fundalul și culorile componentelor apar în Designer imediat după aplicare.
- Proiecte Vue și React cu componente vizuale specifice și build Vite către `publish/web`; proiecte React Native/Expo cu componente mobile și pachet sursă ZIP în `publish`. Codul personalizat se scrie în `ForgeScreen`, iar designerul regenerează separat `ForgeLayout`. **Run** instalează dependențele necesare, pornește serverul local și deschide automat previzualizarea în tabul Browser integrat; **Stop** îl oprește. Dacă npm semnalează o eroare de certificat, editorul reîncearcă folosind certificatele de încredere Windows, fără a dezactiva verificarea TLS. Pentru buildurile web este necesar Node.js/npm cu acces la registrul de pachete. Pachetul React Native este sursă pentru un flux ulterior de build Android/iOS, nu un APK sau IPA.
- Proiecte hibride C#/WPF + C: meniul Construire adaugă un modul C exportat ca DLL și o punte P/Invoke. Buildul compilează DLL-ul și îl include în publicarea aplicației.
- Designer vizual C#/WPF cu mai multe ferestre, tragerea componentelor, 24 de controale WPF uzuale, proprietăți pentru dimensiuni, culori, font și tooltip, și istoric pentru revenire.
- Componentele de pe formular au mânere de redimensionare și pot fi mutate. Clic dreapta oferă copiere, decupare, lipire, duplicare, ordinea suprapunerii, aliniere la grilă, proprietăți și ștergere. Delete, Ctrl+C/X/V/D și săgețile funcționează în designer.
- Exploratorul de proiect, componentele, proprietățile, datele și asistentul sunt panouri mobile în interiorul editorului. Pot fi minimizate, maximizate, redimensionate și readuse la poziția inițială din meniul Ferestre.
- Panoul Proprietăți urmărește selecția: fundalul designerului arată setările ferestrei, iar fiecare control arată proprietățile sale generale, specifice și alte proprietăți simple editabile. Modificările sunt salvate în proiect și scrise în XAML-ul generat.
- Meniul principal include scurtături: Ctrl+N proiect nou, Ctrl+O deschide, Ctrl+S salvează, Ctrl+F caută, Ctrl+H înlocuiește, Ctrl+Shift+F caută în proiect, F5 rulează, F6 construiește, F7 pornește modul Debug.
- Ctrl+Shift+N creează un fișier cu orice extensie permisă în proiect. Meniul Ajutor > Despre afișează `formalini.com` și `Developer: Marco PASCALINI`. Paletele de componente se schimbă după tipul proiectului.
- Agent AI prin cheie API OpenAI sau Gemini pentru generare și editare de cod și componente. Cheile sunt păstrate local prin Windows DPAPI.
- „Creează din descriere” generează o aplicație Hello World fără cod și fără cheie AI; opțional o construiește și o pornește. Cererile generale folosesc agentul AI configurat. În tabul Cod, câmpul „Generează în fișierul deschis” tratează Hello World local sau trimite cererea agentului, limitând modificarea la fișierul selectat.
- Explorare vizuală a unei baze SQLite și legarea unui tabel la un `DataGrid` selectat. Baza este copiată în proiect.
- Dictare prin motorul de recunoaștere vocală disponibil în Windows.
- Git local: inițializare, status, remote GitHub, commit, push și pull prin Git instalat pe sistem.
- Compilare/publicare Windows. Aplicațiile C#/WPF includ runtime .NET și produc un EXE împreună cu fișierele necesare. Pentru C/C++ se folosesc GCC/Clang sau Visual Studio Build Tools; pentru Pascal se folosește Free Pascal. Compilatoarele trebuie instalate separat după clonarea repository-ului; copiile locale din `tools/fpc` nu sunt incluse în Git. Compilatorul Microsoft C/C++ existent în workspace-ul de dezvoltare a fost verificat prin construirea de exemple native.
- Dependențe externe declarate de utilizator: aplicația generată verifică existența unui fișier, cere acord înainte de pornirea installerului inclus, verifică SHA-256 și confirmă instalarea. Folosiți numai installere de origine sigură și cu drept de redistribuire.
- Mod Debug cu simboluri și jurnalul procesului; nu există încă depanare cu breakpoint și execuție pas cu pas.

## Limite importante

- Designerul vizual funcționează pentru C#/WPF, VB/Windows Forms, ferestre Windows native C/C++/Pascal și ecrane Vue/React/React Native. Legarea SQLite, acțiunile ghidate ale controalelor, ferestrele multiple și diagnosticele avansate rămân disponibile doar în C#/WPF. Pentru celelalte limbaje Windows, codul `forge_ui.*` este regenerat după schimbarea formularului; salvați logica proprie în fișiere separate. Integrarea hibridă implementată acum este C# + C prin DLL.
- Biblioteca de controale acoperă elemente WPF frecvente; nu include toate controalele Windows și nici configurarea avansată a fiecăruia. Se pot edita direct fișierele XAML pentru scenarii avansate.
- Importul complet al XAML editat manual în designer nu este implementat. Regenerarea vizuală poate înlocui modificările manuale ale fișierului XAML; istoricul permite recuperarea.
- GitHub folosește configurarea Git existentă. Nu există conectare OAuth, interfață pentru issue-uri, pull request-uri sau gestionarea contului.
- Dictarea necesită un motor de recunoaștere instalat și nu a fost testată cu un microfon în această sesiune.
- Nu există încă pachet de instalare semnat digital, testare pe toate edițiile Windows sau garanție că un antivirus nu va semnala un fișier. Semnarea, proveniența verificabilă, scanarea release-ului și testarea pe sisteme curate sunt necesare înainte de distribuire.
- Agentul poate trimite fișiere din proiect către furnizorul AI ales. Verificați proiectul dacă conține date sensibile și revizuiți codul generat înainte de publicare.
- O cheie OpenAI API este diferită de abonamentul ChatGPT.
- Generarea locală din text recunoaște în prezent Hello World / Salut, lume. Pentru cereri arbitrare este necesară o cheie API OpenAI sau Gemini; rezultatul AI trebuie revizuit.

## Incidentul `Smoke.exe`

Proiectul `tests/Smoke` este un test intern care publică un proiect exemplu. Testul actual a fost rulat prin `Smoke.dll`, fără lansarea unui nou `Smoke.exe`; aplicația exemplu a fost publicată în format cu mai multe fișiere. Pentru analiza alertei anterioare sunt necesare numele antivirusului, denumirea exactă a detecției și calea fișierului semnalat.




