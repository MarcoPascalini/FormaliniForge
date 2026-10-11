using System.IO;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;

namespace FormaliniForge;

public partial class MainWindow
{
    private readonly Dictionary<object, string> originalLabels = new();
    private bool english = true;
    private string LanguagePreference => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FormaliniForge", "language.txt");
    private static readonly Dictionary<string, string> englishLabels = new(StringComparer.Ordinal)
    {
        ["Înapoi"] = "Back", ["Înainte"] = "Forward", ["Reîncarcă"] = "Reload", ["Oprește încărcarea"] = "Stop loading",
        ["Pagina de pornire"] = "Home", ["Tab nou"] = "New tab", ["Sursă"] = "Source", ["Vezi sursa paginii"] = "View page source",
        ["Instrumente dezvoltator"] = "Developer tools", ["Mai multe acțiuni"] = "More actions", ["Găsește în pagină"] = "Find in page",
        ["Mărește"] = "Zoom in", ["Micșorează"] = "Zoom out", ["Dimensiune normală"] = "Actual size", ["Printează"] = "Print",
        ["Copiază adresa"] = "Copy address", ["Deschide în browser extern"] = "Open in external browser", ["Adresă web"] = "Web address",
        ["Închide tabul"] = "Close tab", ["Previzualizare"] = "Preview", ["Se încarcă..."] = "Loading...",
        ["Eroare de navigare"] = "Navigation error", ["Previzualizare pregătită"] = "Preview ready",
        ["Sursa paginii"] = "Page source", ["Text de căutat în pagină"] = "Text to find in page",
        ["Închide instrumentele"] = "Close tools", ["DevTools complet"] = "Full DevTools",
        ["Deschide instrumentele complete într-o fereastră"] = "Open full tools in a window",
        ["Consolă"] = "Console", ["Elemente"] = "Elements", ["Rețea"] = "Network",
        ["Execută"] = "Run", ["Expresie JavaScript"] = "JavaScript expression",
        ["Vizual"] = "Visual", ["Împărțit"] = "Split", ["Reîncarcă previzualizarea"] = "Refresh preview",
        ["Titlu H1"] = "Heading H1", ["Paragraf"] = "Paragraph", ["Buton"] = "Button",
        ["Tabel"] = "Table", ["Aldin"] = "Bold", ["Cursiv"] = "Italic", ["Link"] = "Link",
        ["Imagine"] = "Image", ["Adresa linkului"] = "Link address", ["Adresa imaginii"] = "Image address",
        ["Pagina este construită din componente React/Vue"] = "This page is built from React/Vue components",
        ["Editează interfața în Designer; aici poți modifica structura HTML în modul Cod."] = "Edit the interface in Designer; use Code mode here to change the HTML shell.",
        ["Alege culoarea"] = "Choose color",
        ["Fișiere nesalvate"] = "Unsaved files",
        ["Limbă"] = "Language", ["Fișier"] = "File", ["Creează din descriere..."] = "Create from description...", ["Proiect nou"] = "New project",
        ["Fișier nou..."] = "New file...", ["Ajutor"] = "Help", ["Despre Formalini Forge"] = "About Formalini Forge",
        ["Deschide proiect"] = "Open project", ["Salvează"] = "Save", ["Istoric / Revino"] = "History / Undo",
        ["Editare"] = "Edit", ["Caută în fișier"] = "Find in file", ["Înlocuiește"] = "Replace",
        ["Caută în proiect"] = "Find in project", ["Construire"] = "Build", ["Generează EXE"] = "Build EXE",
        ["Rulează"] = "Run", ["Depanare"] = "Debug", ["Testează"] = "Test", ["Dependențe"] = "Dependencies",
        ["Ferestre"] = "Windows", ["Restabilește poziția panourilor"] = "Reset panel layout",
        ["Status / Inițializează"] = "Status / Initialize", ["Conectează repository"] = "Connect repository",
        ["Înregistrează modificări"] = "Commit changes", ["Trimite modificări"] = "Push changes", ["Preia modificări"] = "Pull changes",
        ["Adaugă modul C hibrid..."] = "Add hybrid C module...",
        ["DESIGN · COD · COMPILARE"] = "DESIGN · CODE · BUILD", ["◫ Designer vizual"] = "◫ Designer", ["⌁ Construire"] = "⌁ Build",
        ["EXPLORATOR PROIECT"] = "PROJECT EXPLORER", ["Fișierele se deschid în taburi"] = "Files open in tabs",
        ["Fereastră activă"] = "Active window", ["＋ Fereastră nouă"] = "＋ New window",
        ["COMPONENTE"] = "COMPONENTS", ["Trage pe formular sau dublu clic"] = "Drag onto form or double click",
        ["SPAȚIU DE LUCRU"] = "WORKSPACE", ["Designer vizual  ·  Editor de cod  ·  Rezultat"] = "Visual designer  ·  Code editor  ·  Output",
        ["Suprafața formularului · trage componente din stânga și editează proprietățile din dreapta"] = "Form canvas · drag components from the left and edit properties on the right",
        ["Cod"] = "Code", ["Găsește"] = "Find", ["Tot"] = "All", ["În proiect"] = "In project",
        ["Caută text"] = "Find text", ["Înlocuiește cu"] = "Replace with",
        ["DATE ȘI SETĂRI"] = "DATA & SETTINGS", ["Bază SQLite din proiect"] = "Project SQLite database",
        ["Alege bază"] = "Choose database", ["Explorează"] = "Explore", ["Leagă tabelul la DataGrid"] = "Bind table to DataGrid",
        ["Fără AI"] = "No AI", ["Editorul și compilarea funcționează fără cheie."] = "Editing and building work without an AI key.",
        ["Cheie API"] = "API key", ["Salvează cheia local"] = "Save key locally",
        ["Niciun proiect deschis"] = "No project open", ["Pregătit"] = "Ready",
        ["Explorator proiect"] = "Project explorer", ["Componente"] = "Components", ["Asistent AI"] = "AI assistant",
        ["Proprietăți"] = "Properties", ["Date și setări"] = "Data & settings",
        ["Aplică proprietățile"] = "Apply properties", ["Șterge componenta"] = "Delete component",
        ["Identitate și poziționare"] = "Identity and position", ["Titlu"] = "Title", ["Lățime"] = "Width",
        ["Înălțime"] = "Height", ["Lățime minimă"] = "Minimum width", ["Înălțime minimă"] = "Minimum height",
        ["Poziție X"] = "X position", ["Poziție Y"] = "Y position", ["Poziție la pornire"] = "Startup position",
        ["Stare inițială"] = "Initial state", ["Redimensionare"] = "Resize mode", ["Dimensiune după conținut"] = "Size to content",
        ["Stilul ferestrei"] = "Window style", ["Mereu deasupra"] = "Always on top", ["În bara de activități"] = "Show in taskbar",
        ["Activează la pornire"] = "Activate on startup", ["Aspect"] = "Appearance", ["Fundal"] = "Background",
        ["Culoare text"] = "Text color", ["Mărime font"] = "Font size", ["Grosime font"] = "Font weight",
        ["Direcția conținutului"] = "Content direction", ["Opacitate (0–1)"] = "Opacity (0–1)",
        ["Alege iconiță (.ico)"] = "Choose icon (.ico)", ["Alege iconița ferestrei"] = "Choose window icon",
        ["Construiește o aplicație Windows"] = "Build a Windows application",
        ["Descrie aplicația pe care o vrei. Pentru Hello World nu ai nevoie de cod sau de o cheie AI."] = "Describe the application you want. Hello World needs no code or AI key.",
        ["Creează din descriere"] = "Create from description",
        ["Panourile au revenit la pozițiile inițiale."] = "Panels returned to their default positions.",
        ["Deschide un proiect, apoi alege formularul sau un control."] = "Open a project, then select the form or a control.",
        ["Designerul de formulare este disponibil pentru proiectele C#/WPF."] = "The form designer is available for C#/WPF projects.",
        ["Clic pe o componentă pentru proprietățile ei. Clic pe fundal pentru formular."] = "Click a component for its properties. Click the background for the form.",
        ["Generează în fișierul deschis"] = "Generate in open file",
        ["Descrie în cuvinte ce cod vrei în fișierul deschis. Exemplu: afișează Hello World."] = "Describe the code you want in the open file. Example: display Hello World.",
        ["Cere modificări, apoi revino din Istoric"] = "Ask for changes, then undo from History",
        ["Descrie ce vrei să creezi sau modifici"] = "Describe what to create or change",
        ["🎙 Dictează"] = "🎙 Dictate", ["Trimite către agent"] = "Send to agent",
        ["Repară eroarea de build cu AI"] = "Fix build error with AI",
        ["Minimizează panoul"] = "Minimize panel", ["Maximizează panoul"] = "Maximize panel",
        ["Creează aplicația din descriere"] = "Create application from description",
        ["Ce aplicație vrei să creezi?"] = "What application do you want to create?",
        ["Exemplu: O aplicație Hello World care afișează Salut, lume!"] = "Example: A Hello World application that displays Hello, World!",
        ["Numele aplicației"] = "Application name", ["Folderul în care creezi proiectul"] = "Project folder",
        ["Alege folderul"] = "Choose folder", ["Alege folderul pentru noul proiect"] = "Choose the folder for the new project",
        ["Construiește și pornește aplicația imediat"] = "Build and run the application now",
        ["Creează proiectul"] = "Create project", ["Alege folderul proiectului"] = "Choose the project folder",
        ["Numele aplicației (litere, cifre și _):"] = "Application name (letters, digits and _):",
        ["Limbajul proiectului"] = "Project language", ["Limbaj: C#, C, C++, Pascal sau VB"] = "Language: C#, C, C++, Pascal or VB",
        ["Identitate și conținut"] = "Identity and content", ["Nume în cod"] = "Name in code",
        ["Text / conținut"] = "Text / content", ["Antet"] = "Header", ["Poziție și dimensiuni"] = "Position and size",
        ["Aspect și comportament"] = "Appearance and behavior", ["Tooltip"] = "Tooltip",
        ["Activ"] = "Enabled", ["Vizibilitate"] = "Visibility", ["Margine (stânga,sus,dreapta,jos)"] = "Margin (left,top,right,bottom)",
        ["Aliniere orizontală"] = "Horizontal alignment", ["Aliniere verticală"] = "Vertical alignment",
        ["Alte proprietăți editabile"] = "Other editable properties", ["Buton implicit"] = "Default button",
        ["Buton de anulare"] = "Cancel button", ["Încadrare text"] = "Text wrapping",
        ["Acceptă Enter"] = "Accept Enter", ["Doar citire"] = "Read only", ["Număr maxim de caractere"] = "Maximum characters",
        ["Acceptă Tab"] = "Accept Tab", ["Aliniere text"] = "Text alignment", ["Selectat"] = "Selected",
        ["Permite tastare"] = "Allow typing", ["Mod de selecție"] = "Selection mode",
        ["Poate adăuga rânduri"] = "Can add rows", ["Poate șterge rânduri"] = "Can delete rows",
        ["Coloane automate"] = "Automatic columns", ["Format dată"] = "Date format",
        ["Minim"] = "Minimum", ["Maxim"] = "Maximum", ["Valoare"] = "Value", ["Orientare"] = "Orientation",
        ["Poziția taburilor"] = "Tab position", ["Extins"] = "Expanded", ["Direcție"] = "Direction",
        ["Culoare contur"] = "Border color", ["Grosime contur"] = "Border thickness", ["Rază colțuri"] = "Corner radius",
        ["Lasă gol pentru valoarea implicită, unde este permis."] = "Leave blank for the default value where permitted.",
        ["Umplere"] = "Fill", ["Urmărește la linie"] = "Watch at line",
        ["Urmărește o variabilă fără a opri execuția"] = "Trace a variable without pausing execution",
        ["Codifică automat cererile"] = "Auto-code requests",
        ["Transformă în cod instrucțiunile scrise în metode C# după o pauză de tastare"] = "Convert plain-language lines inside C# methods after you stop typing",
        ["Oprește"] = "Stop", ["Variabile urmărite · valori live"] = "Watches · live values",
        ["Trage pentru a redimensiona formularul"] = "Drag to resize the form"
        , ["Paletă de comenzi"] = "Command palette", ["Formatează documentul"] = "Format document",
        ["Extensii"] = "Extensions", ["Gestionează extensiile..."] = "Manage extensions...",
        ["ASISTENT"] = "ASSISTANT", ["MOD AI"] = "AI MODE", ["Model API"] = "API model",
        ["WINDOWS APPLICATION STUDIO"] = "WINDOWS APPLICATION STUDIO",
        ["Proiecte · Designer · Cod · Construire"] = "Projects · Designer · Code · Build",
        ["Urmărește la linie"] = "Watch at line", ["Buget tokenuri"] = "Token budget",
        ["Buget opțional în tokenuri; nu este soldul de facturare al furnizorului"] = "Optional token budget; this is not the provider's billing balance",
        ["AI: 0 cereri · 0 tokenuri · sold real indisponibil"] = "AI: 0 requests · 0 tokens · real balance unavailable"
        , ["Editor pregătit. Poți crea și compila proiecte fără cheie AI."] = "Editor ready. You can create and build projects without an AI key.",
        ["Mod fără AI: editorul, designerul și compilarea sunt disponibile."] = "No AI mode: editing, designing, and building are available.",
        ["Cheia API salvată nu a putut fi citită."] = "The saved API key could not be read.",
        ["Proiect deschis."] = "Project opened.", ["Salvat."] = "Saved.",
        ["Textul nu a fost găsit."] = "Text not found.", ["Nu am găsit rezultate."] = "No results found.",
        ["Componenta a fost copiată. Folosește Ctrl+V pentru lipire."] = "Component copied. Use Ctrl+V to paste.",
        ["Componenta a fost lipită."] = "Component pasted.",
        ["Componenta a fost ștearsă. O poți recupera din Istoric."] = "Component deleted. You can restore it from History.",
        ["Deschide mai întâi un fișier de cod."] = "Open a code file first.",
        ["Document formatat."] = "Document formatted.",
        ["Versiunea anterioară a fost restaurată."] = "Previous version restored.",
        ["Versiunea selectată a fost restaurată."] = "Selected version restored.",
        ["Commit creat."] = "Commit created.", ["Modificările au fost trimise la GitHub."] = "Changes pushed to GitHub.",
        ["Proiectul a fost actualizat din GitHub."] = "Project updated from GitHub.",
        ["Compilare în curs..."] = "Building...", ["Construiesc aplicația..."] = "Building application...",
        ["Unele teste au eșuat."] = "Some tests failed.", ["Testele au trecut."] = "Tests passed.",
        ["Dictarea s-a oprit."] = "Dictation stopped.", ["Dictarea este activă."] = "Dictation is active.",
        ["Agentul analizează proiectul..."] = "The agent is analyzing the project...",
        ["Modificările au fost aplicate. Poți folosi Revino."] = "Changes applied. You can use Undo.",
        ["Proprietățile au fost aplicate."] = "Properties applied."
    };

    private string T(string romanian) => english && englishLabels.TryGetValue(romanian, out var translated) ? translated : romanian;
    private static readonly Dictionary<string, string> componentHintsRo = new()
    {
        ["Action button"] = "Buton de acțiune", ["Label"] = "Etichetă", ["Text"] = "Text", ["Text input"] = "Introducere text",
        ["Password input"] = "Introducere parolă", ["Rich text"] = "Text formatat", ["Checkbox"] = "Casetă de bifat",
        ["Radio option"] = "Opțiune unică", ["Toggle"] = "Comutator", ["Dropdown"] = "Listă derulantă",
        ["List"] = "Listă", ["Detailed list"] = "Listă detaliată", ["Tree"] = "Arbore",
        ["Data table"] = "Tabel de date", ["Date picker"] = "Selector de dată", ["Calendar"] = "Calendar",
        ["Slider"] = "Glisor", ["Progress"] = "Progres", ["Tabs"] = "Taburi", ["Group"] = "Grup",
        ["Expandable section"] = "Secțiune extensibilă", ["Border"] = "Contur", ["Rectangle"] = "Dreptunghi", ["Ellipse"] = "Elipsă",
        ["Heading"] = "Titlu", ["Input"] = "Câmp de introducere", ["Text area"] = "Zonă de text",
        ["Image"] = "Imagine", ["Select"] = "Selecție", ["Container"] = "Container", ["Link"] = "Legătură",
        ["Table"] = "Tabel", ["View"] = "Vizualizare", ["Pressable"] = "Zonă apăsabilă", ["Switch"] = "Comutator",
        ["Scroll view"] = "Vizualizare derulantă", ["Flat list"] = "Listă performantă"
    };
    private string ComponentHint(string englishHint) => english ? englishHint : componentHintsRo.GetValueOrDefault(englishHint, englishHint);
    private static readonly Dictionary<string, string> componentNamesRo = new()
    {
        ["Button"] = "Buton", ["Label"] = "Etichetă", ["TextBlock"] = "Text", ["TextBox"] = "Casetă text",
        ["PasswordBox"] = "Parolă", ["RichTextBox"] = "Text formatat", ["CheckBox"] = "Bifare",
        ["RadioButton"] = "Opțiune", ["ToggleButton"] = "Comutator", ["ComboBox"] = "Listă derulantă",
        ["ListBox"] = "Listă", ["ListView"] = "Listă detaliată", ["TreeView"] = "Arbore",
        ["DataGrid"] = "Tabel", ["DatePicker"] = "Dată", ["Calendar"] = "Calendar",
        ["Slider"] = "Glisor", ["ProgressBar"] = "Progres", ["TabControl"] = "Taburi",
        ["GroupBox"] = "Grup", ["Expander"] = "Secțiune", ["Border"] = "Contur",
        ["Rectangle"] = "Dreptunghi", ["Ellipse"] = "Elipsă", ["Heading"] = "Titlu", ["Text"] = "Text",
        ["Input"] = "Câmp text", ["TextArea"] = "Zonă de text", ["Image"] = "Imagine", ["Select"] = "Selecție",
        ["Container"] = "Container", ["Link"] = "Legătură", ["List"] = "Listă", ["Table"] = "Tabel",
        ["View"] = "Vizualizare", ["TextInput"] = "Câmp text", ["Pressable"] = "Zonă apăsabilă",
        ["Switch"] = "Comutator", ["ScrollView"] = "Vizualizare derulantă", ["FlatList"] = "Listă performantă"
    };
    private string ComponentName(string type) => english ? type : componentNamesRo.GetValueOrDefault(type, type);

    private void English_Click(object sender, RoutedEventArgs e) { english = true; SaveLanguage(); ApplyLanguage(); }
    private void Romanian_Click(object sender, RoutedEventArgs e) { english = false; SaveLanguage(); ApplyLanguage(); }

    private void SaveLanguage()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LanguagePreference)!);
        File.WriteAllText(LanguagePreference, english ? "en" : "ro");
    }

    private void ApplyLanguage()
    {
        try { english = File.Exists(LanguagePreference) ? File.ReadAllText(LanguagePreference).Trim() != "ro" : english; } catch { }
        LocalizeTree(this);
        BuildMenuItem.Header = projectDir != null && IsWebProject ? (english ? "Build deployable files" : "Generează fișiere de publicare") : T("Generează EXE");
        HybridMenuItem.IsEnabled = projectDir != null && state.Language == "C#";
        foreach (var pane in toolPanes)
        {
            pane.Caption.Text = T(pane.Title).ToUpperInvariant();
            pane.MinimizeButton.ToolTip = T("Minimizează panoul");
            pane.MaximizeButton.ToolTip = T("Maximizează panoul");
        }
        if (projectDir == null) { RenderDesigner(); LocalizeTree(DesignCanvas); }
        RefreshInspector();
        foreach (var entry in Toolbox.Items.OfType<ListBoxItem>())
            if (entry.Tag is string type && componentIcons.TryGetValue(type, out var icon))
            {
                entry.ToolTip = ComponentHint(icon.Hint);
                if (entry.Content is StackPanel row && row.Children.OfType<TextBlock>().LastOrDefault() is { } label)
                    label.Text = ComponentName(type);
            }
        RefreshCreditBalance();
        StatusText.Text = T("Pregătit");
        foreach (var document in openEditors.Values.Where(d => d.HtmlVisual != null)) _ = RefreshHtmlPreviewAsync(document);
    }

    private void LocalizeTree(DependencyObject root)
    {
        var seen = new HashSet<DependencyObject>();
        Visit(root);
        void Visit(DependencyObject element)
        {
            if (!seen.Add(element)) return;
            if (element is TextBlock text) Set(text, text.Text, v => text.Text = v);
            if (element is MenuItem menu) Set(menu, menu.Header as string, v => menu.Header = v);
            else if (element is TabItem tab) Set(tab, tab.Header as string, v => tab.Header = v);
            else if (element is Expander expander) Set(expander, expander.Header as string, v => expander.Header = v);
            else if (element is Button button) Set(button, button.Content as string, v => button.Content = v);
            else if (element is ComboBoxItem combo) Set(combo, combo.Content as string, v => combo.Content = v);
            if (element is FrameworkElement framework && framework.ToolTip is string tip)
                Set((framework, "tooltip"), tip, v => framework.ToolTip = v);
            if (element is ItemsControl items)
                foreach (var item in items.Items.OfType<DependencyObject>()) Visit(item);
            foreach (var child in LogicalTreeHelper.GetChildren(element).OfType<DependencyObject>()) Visit(child);
        }
        void Set(object key, string? current, Action<string> write)
        {
            if (current == null) return;
            if (!originalLabels.TryGetValue(key, out var original)) originalLabels[key] = original = current;
            write(T(original));
        }
    }
}
