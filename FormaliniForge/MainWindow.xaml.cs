using Microsoft.Win32;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using System.ComponentModel;
using System.Speech.Recognition;
using Microsoft.Data.Sqlite;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using DragEventArgs = System.Windows.DragEventArgs;
using MessageBox = System.Windows.MessageBox;
using Brushes = System.Windows.Media.Brushes;
using TextBox = System.Windows.Controls.TextBox;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using ListBox = System.Windows.Controls.ListBox;
using Button = System.Windows.Controls.Button;
using DragDrop = System.Windows.DragDrop;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using Orientation = System.Windows.Controls.Orientation;
using RichTextBox = System.Windows.Controls.RichTextBox;
using Label = System.Windows.Controls.Label;
using RadioButton = System.Windows.Controls.RadioButton;
using ListView = System.Windows.Controls.ListView;
using TreeView = System.Windows.Controls.TreeView;
using ProgressBar = System.Windows.Controls.ProgressBar;
using TabControl = System.Windows.Controls.TabControl;
using GroupBox = System.Windows.Controls.GroupBox;
using Control = System.Windows.Controls.Control;

namespace FormaliniForge;

public class DesignItem
{
    public string Type { get; set; } = "Button";
    public string Name { get; set; } = "button1";
    public string Text { get; set; } = "Button";
    public double X { get; set; } = 30;
    public double Y { get; set; } = 30;
    public double Width { get; set; } = 130;
    public double Height { get; set; } = 32;
    public string DataTable { get; set; } = "";
    public string Background { get; set; } = "";
    public string Foreground { get; set; } = "";
    public double FontSize { get; set; } = 14;
    public string ToolTip { get; set; } = "";
    public Dictionary<string, string> Options { get; set; } = new();
    public Dictionary<string, string> Events { get; set; } = new();
}

public class ProjectState
{
    public string Name { get; set; } = "NewApp";
    public string Language { get; set; } = "C#";
    public string ConnectionString { get; set; } = "";
    public string DatabaseFile { get; set; } = "";
    public List<DesignItem> Items { get; set; } = new();
    public Dictionary<string, List<DesignItem>> Windows { get; set; } = new();
    public List<ExternalDependency> Dependencies { get; set; } = new();
    public Dictionary<string, FormSettings> Forms { get; set; } = new();
}

public class FormSettings
{
    public string Title { get; set; } = "";
    public double Width { get; set; } = 850;
    public double Height { get; set; } = 650;
    public double MinWidth { get; set; } = 300;
    public double MinHeight { get; set; } = 200;
    public string Background { get; set; } = "White";
    public string Foreground { get; set; } = "Black";
    public string ResizeMode { get; set; } = "CanResize";
    public string SizeToContent { get; set; } = "Manual";
    public string WindowStyle { get; set; } = "SingleBorderWindow";
    public string IconFile { get; set; } = "";
    public string WindowStartupLocation { get; set; } = "CenterScreen";
    public string WindowState { get; set; } = "Normal";
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public bool Topmost { get; set; }
    public bool ShowInTaskbar { get; set; } = true;
    public bool ShowActivated { get; set; } = true;
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 13;
    public string FontWeight { get; set; } = "Normal";
    public string FlowDirection { get; set; } = "LeftToRight";
    public double Opacity { get; set; } = 1;
}

public class ExternalDependency
{
    public string Name { get; set; } = "";
    public string ProbePath { get; set; } = "";
    public string InstallerFile { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string Arguments { get; set; } = "";
}

internal sealed class EditorDocument
{
    public required string Path { get; init; }
    public required TabItem Tab { get; init; }
    public required TextEditor Editor { get; init; }
    public bool Dirty { get; set; }
    public HtmlVisualDocument? HtmlVisual { get; set; }
}

internal sealed class WordCompletion : ICompletionData
{
    public WordCompletion(string text) { Text = text; }
    public System.Windows.Media.ImageSource? Image => null;
    public string Text { get; }
    public object Content => Text;
    public object Description => "Sugestie locală";
    public double Priority => 0;
    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        => textArea.Document.Replace(completionSegment, Text);
}

public partial class MainWindow : Window
{
    private readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private ProjectState state = new();
    private string? projectDir;
    private readonly Dictionary<string, EditorDocument> openEditors = new(StringComparer.OrdinalIgnoreCase);
    private CompletionWindow? completionWindow;
    private SpeechRecognitionEngine? speechEngine;
    private string activeWindow = "MainWindow";
    private List<DesignItem> CurrentItems => activeWindow == "MainWindow" ? state.Items : state.Windows[activeWindow];
    private DesignItem? selected;
    private Point dragStart;
    private bool dragging;
    private bool dragSnapshot;
    private string? lastBuiltExe;
    private static readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(3) };
    private string Provider => ProviderBox.SelectedIndex switch { 1 => "OpenAI", 2 => "Gemini", _ => "Fără AI" };
    private string KeyFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FormaliniForge", Provider.ToLowerInvariant() + ".key");

    public MainWindow()
    {
        InitializeComponent();
        ProviderBox.SelectedIndex = 0;
        Status("Editor pregătit. Poți crea și compila proiecte fără cheie AI.");
        RenderDesigner();
        Loaded += (_, _) => { InitializeToolWindows(); DecorateToolbox(); InitializeInspector(); LoadExtensions(); LoadUsage(); ApplyLanguage(); };
        Closing += MainWindow_Closing;
        Closed += (_, _) => { try { if (webServer is { HasExited: false }) webServer.Kill(entireProcessTree: true); } catch { } };
    }
    private void ProviderBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ApiKeyBox == null || ModelBox == null) return;
        var offline = Provider == "Fără AI";
        ModelBox.IsEnabled = !offline;
        ApiKeyBox.IsEnabled = !offline;
        SaveKeyButton.IsEnabled = !offline;
        ModelBox.Text = offline ? "" : Provider == "OpenAI" ? "gpt-6-luna" : "gemini-2.5-flash";
        ApiKeyBox.Password = "";
        if (!offline && File.Exists(KeyFile))
        {
            try { ApiKeyBox.Password = Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(KeyFile), null, DataProtectionScope.CurrentUser)); }
            catch { Status("Cheia API salvată nu a putut fi citită."); }
        }
        if (offline) Status("Mod fără AI: editorul, designerul și compilarea sunt disponibile.");
        RefreshCreditBalance();
    }

    private void Status(string message) => StatusText.Text = T(message);
    private void RequireProject()
    {
        if (projectDir == null) throw new InvalidOperationException("Creează sau deschide mai întâi un proiect.");
    }
    private void Error(Exception ex) { Status(ex.Message); MessageBox.Show(ex.Message, "Formalini Forge", MessageBoxButton.OK, MessageBoxImage.Warning); }

    private void NewProject_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveCode();
            var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = T("Alege folderul pentru noul proiect"), UseDescriptionForTitle = true };
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            var name = Microsoft.VisualBasic.Interaction.InputBox(T("Numele aplicației (litere, cifre și _):"), T("Proiect nou"), "MyApp").Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$")) throw new InvalidOperationException("Numele trebuie să înceapă cu o literă sau _ și să conțină doar litere, cifre sau _.");
            var language = ChooseProjectLanguage();
            if (language == null) return;
            var target = Path.Combine(dialog.SelectedPath, name);
            if (Directory.Exists(target) && Directory.EnumerateFileSystemEntries(target).Any()) throw new InvalidOperationException("Folderul proiectului există și nu este gol.");
            projectDir = target;
            Directory.CreateDirectory(projectDir);
            state = new ProjectState { Name = name, Language = language, Items = language == "C#" ? new() { new DesignItem { Type = "TextBlock", Name = "title", Text = name, X = 28, Y = 25, Width = 500, Height = 36 } } : new() };
            if (language == "React Native") state.Forms["MainWindow"] = new FormSettings { Title = name, Width = 390, Height = 760, WindowStyle = "None" };
            else if (language is "Vue" or "React") state.Forms["MainWindow"] = new FormSettings { Title = name, Width = 1024, Height = 720, WindowStyle = "None" };
            GenerateApp();
            SaveProject();
            OpenProject(target);
        }
        catch (Exception ex) { Error(ex); }
    }

    private void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveCode();
            var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = T("Alege folderul proiectului") };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) OpenProject(dialog.SelectedPath);
        }
        catch (Exception ex) { Error(ex); }
    }

    private void OpenProject(string path)
    {
        var statePath = Path.Combine(path, "forge.json");
        if (!File.Exists(statePath)) throw new InvalidOperationException("Folderul nu conține forge.json.");
        state = JsonSerializer.Deserialize<ProjectState>(File.ReadAllText(statePath), jsonOptions) ?? throw new InvalidOperationException("Proiect invalid.");
        projectDir = path;
        selected = null;
        activeWindow = "MainWindow";
        CloseAllEditors();
        if (IsWebProject) GenerateWebProject();
        ConnectionBox.Text = state.DatabaseFile;
        ProjectLabel.Text = state.Name + " · " + state.Language;
        lastBuiltExe = null;
        WindowBox.Items.Clear();
        WindowBox.Items.Add("MainWindow");
        foreach (var name in state.Windows.Keys.OrderBy(x => x)) WindowBox.Items.Add(name);
        WindowBox.SelectedIndex = 0;
        ConfigureToolboxForProject();
        RenderDesigner();
        ShowProperties();
        RefreshFiles();
        LoadWatchDefinitions();
        ApplyLanguage();
        Status("Proiect deschis.");
    }

    private void Save_Click(object sender, RoutedEventArgs e) { try { SaveProject(); Status("Salvat."); } catch (Exception ex) { Error(ex); } }
    private void SaveProject()
    {
        RequireProject();
        SaveAllEditors();
        File.WriteAllText(Path.Combine(projectDir!, "forge.json"), JsonSerializer.Serialize(state, jsonOptions));
        RefreshFiles();
    }
    private void SaveCode() => SaveAllEditors();
    private void SaveAllEditors()
    {
        foreach (var document in openEditors.Values.Where(x => x.Dirty))
        {
            File.WriteAllText(document.Path, document.Editor.Text);
            document.Dirty = false;
            SetTabTitle(document);
        }
    }
    private void SetTabTitle(EditorDocument document)
    {
        if (document.Tab.Header is StackPanel panel && panel.Children[0] is TextBlock title)
            title.Text = Path.GetFileName(document.Path) + (document.Dirty ? " ●" : "");
    }
    private void CloseAllEditors()
    {
        completionWindow?.Close();
        foreach (var document in openEditors.Values)
        {
            document.HtmlVisual?.RefreshTimer.Stop();
            document.HtmlVisual?.View.Dispose();
        }
        EditorTabs.Items.Clear();
        openEditors.Clear();
    }

    private void RefreshFiles()
    {
        FileTree.Items.Clear();
        if (projectDir == null) return;
        void AddChildren(DirectoryInfo directory, ItemCollection target)
        {
            foreach (var sub in directory.EnumerateDirectories().Where(d => d.Name is not (".forge-history" or ".git" or "bin" or "obj" or "publish" or "publish-debug" or "node_modules" or "dist" or ".expo" or ".forge-npm-cache")).OrderBy(d => d.Name))
            {
                var folder = new TreeViewItem { Header = "📁 " + sub.Name, Foreground = Brushes.White };
                AddChildren(sub, folder.Items);
                target.Add(folder);
            }
            foreach (var file in directory.EnumerateFiles().OrderBy(f => f.Name))
                target.Add(new TreeViewItem { Header = file.Name, Tag = file.FullName, Foreground = Brushes.White });
        }
        AddChildren(new DirectoryInfo(projectDir), FileTree.Items);
    }
    private void FileTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not TreeViewItem item || item.Tag is not string path) return;
        try { OpenEditor(path); }
        catch (Exception ex) { Error(ex); }
    }
    private void OpenEditor(string path)
    {
        if (openEditors.TryGetValue(path, out var existing))
        {
            EditorTabs.SelectedItem = existing.Tab; MainTabs.SelectedIndex = 1; return;
        }
        var editor = new TextEditor
        {
            Text = ReadFormattedSource(path),
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            FontSize = 14,
            ShowLineNumbers = true,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var syntax = ext switch { ".cs" => "C#", ".c" or ".cpp" or ".h" or ".hpp" => "C++", ".xaml" or ".xml" or ".csproj" or ".vbproj" or ".vue" => "XML", ".html" or ".htm" => "HTML", ".vb" => "VB", ".js" or ".jsx" or ".ts" or ".tsx" or ".json" => "JavaScript", ".java" => "Java", _ => null };
        if (syntax != null) editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(syntax);
        if (ext == ".pas")
        {
            const string pascal = """
<SyntaxDefinition name="Pascal" extensions=".pas" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
 <Color name="Comment" foreground="Green"/>
 <Color name="Keyword" foreground="Blue" fontWeight="bold"/>
 <Color name="String" foreground="Brown"/>
 <RuleSet>
  <Span color="Comment" begin="\{" end="\}"/>
  <Span color="Comment" begin="//" end="$"/>
  <Span color="String" begin="'" end="'"/>
  <Keywords color="Keyword"><Word>program</Word><Word>unit</Word><Word>uses</Word><Word>interface</Word><Word>implementation</Word><Word>begin</Word><Word>end</Word><Word>var</Word><Word>const</Word><Word>type</Word><Word>procedure</Word><Word>function</Word><Word>if</Word><Word>then</Word><Word>else</Word><Word>for</Word><Word>while</Word><Word>do</Word><Word>repeat</Word><Word>until</Word><Word>class</Word><Word>private</Word><Word>public</Word></Keywords>
 </RuleSet>
</SyntaxDefinition>
""";
            using var reader = XmlReader.Create(new StringReader(pascal));
            editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
        var title = new StackPanel { Orientation = Orientation.Horizontal };
        title.Children.Add(new TextBlock { Text = Path.GetFileName(path) });
        var close = new Button { Content = "×", Padding = new Thickness(3, 0, 3, 0), Margin = new Thickness(8, 0, 0, 0), Tag = path };
        close.Click += CloseTab_Click; title.Children.Add(close);
        var tab = new TabItem { Header = title, Content = editor, Tag = path };
        var document = new EditorDocument { Path = path, Tab = tab, Editor = editor };
        openEditors[path] = document;
        editor.TextChanged += (_, _) => { document.Dirty = true; SetTabTitle(document); };
        editor.TextArea.TextEntered += Editor_TextEntered;
        editor.TextArea.TextEntering += Editor_TextEntering;
        AttachWatchHover(editor, path);
        AttachInlineAssistant(document);
        if (ext is ".html" or ".htm") AttachHtmlVisualEditor(document);
        EditorTabs.Items.Add(tab);
        EditorTabs.SelectedItem = tab;
        MainTabs.SelectedIndex = 1;
    }
    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string path || !openEditors.TryGetValue(path, out var doc)) return;
        if (doc.Dirty) { File.WriteAllText(path, doc.Editor.Text); doc.Dirty = false; }
        doc.HtmlVisual?.RefreshTimer.Stop();
        doc.HtmlVisual?.View.Dispose();
        EditorTabs.Items.Remove(doc.Tab); openEditors.Remove(path);
        e.Handled = true;
    }
    private void EditorTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source == EditorTabs && EditorTabs.SelectedItem is TabItem tab && tab.Tag is string path && openEditors.TryGetValue(path, out var document) && document.HtmlVisual == null)
            document.Editor.Focus();
    }
    private TextEditor? CurrentEditor => (EditorTabs.SelectedItem as TabItem)?.Tag is string path && openEditors.TryGetValue(path, out var document) ? document.Editor : null;
    private void Find_Click(object sender, RoutedEventArgs e)
    {
        var editor = CurrentEditor;
        var query = FindBox.Text;
        if (editor == null || query.Length == 0) return;
        var start = editor.SelectionStart + editor.SelectionLength;
        var index = editor.Text.IndexOf(query, Math.Min(start, editor.Text.Length), StringComparison.OrdinalIgnoreCase);
        if (index < 0) index = editor.Text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index < 0) { Status("Textul nu a fost găsit."); return; }
        editor.Select(index, query.Length);
        editor.ScrollToLine(editor.Document.GetLineByOffset(index).LineNumber);
        editor.Focus();
        Status("Găsit la linia " + editor.Document.GetLineByOffset(index).LineNumber);
    }
    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        var editor = CurrentEditor;
        if (editor == null || FindBox.Text.Length == 0) return;
        if (!editor.SelectedText.Equals(FindBox.Text, StringComparison.OrdinalIgnoreCase)) Find_Click(sender, e);
        if (editor.SelectedText.Equals(FindBox.Text, StringComparison.OrdinalIgnoreCase))
            editor.Document.Replace(editor.SelectionStart, editor.SelectionLength, ReplaceBox.Text);
    }
    private void ReplaceAll_Click(object sender, RoutedEventArgs e)
    {
        var editor = CurrentEditor;
        if (editor == null || FindBox.Text.Length == 0) return;
        var count = Regex.Matches(editor.Text, Regex.Escape(FindBox.Text), RegexOptions.IgnoreCase).Count;
        editor.Text = editor.Text.Replace(FindBox.Text, ReplaceBox.Text, StringComparison.OrdinalIgnoreCase);
        Status(count + " apariții înlocuite în fișierul curent.");
    }
    private void SearchProject_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireProject();
            var query = FindBox.Text;
            if (query.Length == 0) return;
            SaveAllEditors();
            var lines = new List<string>();
            foreach (var file in Directory.EnumerateFiles(projectDir!, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(projectDir!, file);
                if (rel.Split(Path.DirectorySeparatorChar).Any(part => part is ".git" or ".forge-history" or "bin" or "obj" or "publish" or "publish-debug")) continue;
                if (new FileInfo(file).Length > 1_000_000) continue;
                try
                {
                    var number = 0;
                    foreach (var line in File.ReadLines(file))
                    {
                        number++;
                        if (line.Contains(query, StringComparison.OrdinalIgnoreCase))
                            lines.Add($"{rel}:{number}: {line.Trim()}");
                        if (lines.Count >= 200) break;
                    }
                }
                catch (IOException) { }
                if (lines.Count >= 200) break;
            }
            BuildOutput.Text = lines.Count == 0 ? "Nu am găsit rezultate." : string.Join(Environment.NewLine, lines);
            MainTabs.SelectedIndex = 2;
            Status(lines.Count + " rezultate găsite.");
        }
        catch (Exception ex) { Error(ex); }
    }
    private void Editor_TextEntering(object? sender, TextCompositionEventArgs e)
    {
        if (completionWindow != null && e.Text.Length > 0 && !char.IsLetterOrDigit(e.Text[0]) && e.Text[0] != '_')
            completionWindow.CompletionList.RequestInsertion(e);
    }
    private void Editor_TextEntered(object? sender, TextCompositionEventArgs e)
    {
        if (sender is not TextArea area || e.Text.Length != 1 || !(char.IsLetterOrDigit(e.Text[0]) || e.Text[0] == '_')) return;
        var editor = openEditors.Values.FirstOrDefault(x => x.Editor.TextArea == area)?.Editor;
        if (editor == null || completionWindow != null) return;
        var offset = editor.CaretOffset;
        var start = offset;
        while (start > 0 && (char.IsLetterOrDigit(editor.Document.GetCharAt(start - 1)) || editor.Document.GetCharAt(start - 1) == '_')) start--;
        var prefix = editor.Document.GetText(start, offset - start);
        if (prefix.Length < 2) return;
        var keywords = new[] { "class", "public", "private", "static", "void", "string", "int", "return", "using", "namespace", "async", "await", "if", "else", "foreach", "while", "new", "var", "try", "catch", "include", "main", "begin", "end", "procedure", "function" };
        var words = Regex.Matches(editor.Text, @"\b[A-Za-z_][A-Za-z0-9_]{2,}\b").Select(x => x.Value).Concat(keywords).Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !x.Equals(prefix, StringComparison.OrdinalIgnoreCase)).Take(20).ToList();
        var currentPath = openEditors.Values.First(x => x.Editor == editor).Path;
        var snippets = MatchingSnippets(currentPath, prefix).ToList();
        if (words.Count == 0 && snippets.Count == 0) return;
        completionWindow = new CompletionWindow(area) { StartOffset = start, EndOffset = offset };
        foreach (var word in words) completionWindow.CompletionList.CompletionData.Add(new WordCompletion(word));
        foreach (var snippet in snippets) completionWindow.CompletionList.CompletionData.Add(new SnippetCompletion(snippet));
        completionWindow.Closed += (_, _) => completionWindow = null;
        completionWindow.Show();
    }

    private void RenderDesigner()
    {
        DesignCanvas.Children.Clear();
        FormResizeHandle.Visibility = projectDir != null ? Visibility.Visible : Visibility.Collapsed;
        if (projectDir == null)
        {
            Toolbox.IsEnabled = false;
            var welcome = new StackPanel { Width = 540 };
            welcome.Children.Add(new TextBlock { Text = "Construiește o aplicație Windows", FontSize = 29, FontWeight = FontWeights.SemiBold, Foreground = Brushes.Black, TextWrapping = TextWrapping.Wrap });
            welcome.Children.Add(new TextBlock { Text = "Descrie aplicația pe care o vrei. Pentru Hello World nu ai nevoie de cod sau de o cheie AI.", FontSize = 16, Foreground = Brushes.DimGray, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 15, 0, 0) });
            var quickStart = new Button { Content = "Creează din descriere", Width = 220, Margin = new Thickness(0, 22, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            quickStart.Click += QuickStart_Click;
            welcome.Children.Add(quickStart);
            Canvas.SetLeft(welcome, 85); Canvas.SetTop(welcome, 135);
            DesignCanvas.Children.Add(welcome);
            return;
        }
        DesignCanvas.IsEnabled = true;
        Toolbox.IsEnabled = true;
        var formSettings = CurrentForm;
        DesignerFrame.Width = formSettings.Width;
        DesignerFrame.Height = Math.Max(200, formSettings.Height);
        DesignCanvas.Width = DesignerFrame.Width - 2;
        var chromeHeight = formSettings.WindowStyle == "None" ? 0 : formSettings.WindowStyle == "ToolWindow" ? 26 : 32;
        PreviewChromeRow.Height = new GridLength(chromeHeight);
        PreviewChrome.Visibility = chromeHeight == 0 ? Visibility.Collapsed : Visibility.Visible;
        PreviewChrome.Background = Brush(formSettings.WindowStyle == "ToolWindow" ? "#D5DCE6" : "#E6EBF2");
        DesignerFrame.BorderThickness = new Thickness(formSettings.WindowStyle == "None" ? 0 : formSettings.WindowStyle == "ThreeDBorderWindow" ? 4 : 1);
        PreviewTitle.Text = formSettings.Title;
        PreviewMinButton.Visibility = formSettings.WindowStyle == "ToolWindow" || formSettings.ResizeMode == "NoResize" ? Visibility.Collapsed : Visibility.Visible;
        PreviewMaxButton.Visibility = formSettings.WindowStyle == "ToolWindow" || formSettings.ResizeMode is "NoResize" or "CanMinimize" ? Visibility.Collapsed : Visibility.Visible;
        PreviewCloseButton.Visibility = Visibility.Visible;
        PreviewIcon.Source = LoadPreviewIcon(formSettings.IconFile);
        DesignCanvas.Height = Math.Max(100, DesignerFrame.Height - chromeHeight - 2);
        DesignCanvas.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(formSettings.Background.Length == 0 ? "White" : formSettings.Background)!;
        DesignerFrame.Opacity = formSettings.Opacity;
        DesignCanvas.FlowDirection = formSettings.FlowDirection == "RightToLeft" ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;
        System.Windows.Documents.TextElement.SetFontFamily(DesignCanvas, new System.Windows.Media.FontFamily(formSettings.FontFamily));
        System.Windows.Documents.TextElement.SetFontSize(DesignCanvas, formSettings.FontSize);
        System.Windows.Documents.TextElement.SetForeground(DesignCanvas, (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(formSettings.Foreground.Length == 0 ? "Black" : formSettings.Foreground)!);
        foreach (var item in CurrentItems)
        {
            FrameworkElement visual = item.Type switch
            {
                "TextBox" => new TextBox { Text = item.Text },
                "PasswordBox" => new PasswordBox(),
                "RichTextBox" => new RichTextBox { IsReadOnly = true },
                "TextBlock" => new TextBlock { Text = item.Text, FontSize = 21, Foreground = Brushes.Black },
                "Label" => new Label { Content = item.Text, Foreground = Brushes.Black },
                "CheckBox" => new CheckBox { Content = item.Text, Foreground = Brushes.Black },
                "RadioButton" => new RadioButton { Content = item.Text, Foreground = Brushes.Black },
                "ToggleButton" => new System.Windows.Controls.Primitives.ToggleButton { Content = item.Text },
                "ComboBox" => new ComboBox { Text = item.Text, IsEditable = true },
                "ListBox" => new ListBox(),
                "ListView" => new ListView(),
                "TreeView" => new TreeView(),
                "DataGrid" => new DataGrid { AutoGenerateColumns = true, IsReadOnly = true },
                "DatePicker" => new DatePicker(),
                "Calendar" => new Calendar(),
                "Slider" => new Slider { Minimum = 0, Maximum = 100, Value = 40 },
                "ProgressBar" => new ProgressBar { Minimum = 0, Maximum = 100, Value = 40 },
                "TabControl" => new TabControl { Items = { new TabItem { Header = "Tab 1" }, new TabItem { Header = "Tab 2" } } },
                "GroupBox" => new GroupBox { Header = item.Text },
                "Expander" => new Expander { Header = item.Text, IsExpanded = true },
                "Border" => new Border { BorderBrush = Brushes.SlateGray, BorderThickness = new Thickness(1) },
                "Rectangle" => new System.Windows.Shapes.Rectangle { Fill = Brushes.LightBlue, Stroke = Brushes.SteelBlue },
                "Ellipse" => new System.Windows.Shapes.Ellipse { Fill = Brushes.LightBlue, Stroke = Brushes.SteelBlue },
                "Heading" => new TextBlock { Text = item.Text, FontSize = 23, FontWeight = FontWeights.Bold, Foreground = Brushes.Black },
                "Text" => new TextBlock { Text = item.Text, Foreground = Brushes.Black },
                "Input" or "TextInput" => new TextBox { Text = item.Text },
                "TextArea" => new TextBox { Text = item.Text, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap },
                "Select" => new ComboBox { Text = item.Text, IsEditable = true },
                "Container" or "View" => new Border { BorderBrush = Brushes.SlateGray, BorderThickness = new Thickness(1), Background = Brushes.WhiteSmoke },
                "Link" => new TextBlock { Text = item.Text, Foreground = Brushes.RoyalBlue, TextDecorations = TextDecorations.Underline },
                "List" or "FlatList" => new ListBox { Items = { item.Text } },
                "Table" => new DataGrid { AutoGenerateColumns = true },
                "Image" => new Border { Background = Brushes.LightGray, Child = new TextBlock { Text = "▧ " + item.Text, Foreground = Brushes.DimGray } },
                "Switch" => new System.Windows.Controls.Primitives.ToggleButton { Content = item.Text },
                "ScrollView" => new ScrollViewer { Content = new TextBlock { Text = item.Text, Foreground = Brushes.Black } },
                "Pressable" => new Button { Content = item.Text },
                _ => new Button { Content = item.Text }
            };
            visual.Width = item.Width; visual.Height = item.Height;
            if (visual is Control control)
            {
                if (item.Background.Length > 0) control.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(item.Background)!;
                control.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(item.Foreground.Length > 0 ? item.Foreground : formSettings.Foreground)!;
                control.FontSize = item.FontSize;
            }
            else if (visual is TextBlock text)
            {
                if (item.Background.Length > 0) text.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(item.Background)!;
                text.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(item.Foreground.Length > 0 ? item.Foreground : formSettings.Foreground)!;
                text.FontSize = item.FontSize;
            }
            else if (visual is Border border)
            {
                if (item.Background.Length > 0) border.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(item.Background)!;
                if (item.Foreground.Length > 0 && border.Child is TextBlock child)
                    child.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(item.Foreground)!;
            }
            else if (visual is System.Windows.Shapes.Shape shape)
            {
                if (item.Background.Length > 0) shape.Fill = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(item.Background)!;
                if (item.Foreground.Length > 0) shape.Stroke = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(item.Foreground)!;
            }
            if (item.ToolTip.Length > 0) visual.ToolTip = item.ToolTip;
            foreach (var option in item.Options ?? new())
            {
                var descriptor = TypeDescriptor.GetProperties(visual)[option.Key];
                if (descriptor != null && descriptor.Converter.CanConvertFrom(typeof(string)))
                    descriptor.SetValue(visual, descriptor.Converter.ConvertFromInvariantString(option.Value));
            }
            visual.Tag = item;
            Canvas.SetLeft(visual, item.X); Canvas.SetTop(visual, item.Y);
            visual.PreviewMouseLeftButtonDown += Component_MouseDown;
            visual.PreviewMouseMove += Component_MouseMove;
            visual.PreviewMouseLeftButtonUp += Component_MouseUp;
            visual.PreviewMouseRightButtonDown += Component_PreviewMouseRightButtonDown;
            DesignCanvas.Children.Add(visual);
        }
        UpdateSelectionAdorner();
    }
    private void Component_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not DesignItem item) return;
        if (e.ClickCount >= 2)
        {
            dragging = false;
            try
            {
                if (state.Language == "C#") OpenComponentEvents(item);
                else if (IsWebProject) OpenEditor(Path.Combine(projectDir!, "App", "src", state.Language == "Vue" ? "ForgeScreen.vue" : "ForgeScreen.jsx"));
                else OpenEditor(NativeUiSource);
            }
            catch (Exception ex) { Error(ex); }
            e.Handled = true;
            return;
        }
        selected = item; ShowProperties();
        UpdateSelectionAdorner();
        dragStart = e.GetPosition(DesignCanvas); dragging = true;
        dragSnapshot = false;
        element.CaptureMouse(); e.Handled = true;
    }
    private void Component_MouseMove(object sender, MouseEventArgs e)
    {
        if (!dragging || sender is not FrameworkElement element || element.Tag is not DesignItem item || e.LeftButton != MouseButtonState.Pressed) return;
        if (!dragSnapshot) { Snapshot(); dragSnapshot = true; }
        var point = e.GetPosition(DesignCanvas);
        item.X = Math.Clamp(item.X + point.X - dragStart.X, 0, DesignCanvas.Width - item.Width);
        item.Y = Math.Clamp(item.Y + point.Y - dragStart.Y, 0, DesignCanvas.Height - item.Height);
        Canvas.SetLeft(element, item.X); Canvas.SetTop(element, item.Y);
        dragStart = point; PositionSelectionAdorner();
    }
    private void Component_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!dragging) return;
        dragging = false;
        if (sender is FrameworkElement element) element.ReleaseMouseCapture();
        try { if (dragSnapshot) { GenerateApp(); SaveProject(); ShowProperties(); } } catch (Exception ex) { Error(ex); }
    }
    private void ShowProperties()
    {
        RefreshInspector();
    }
    private void WindowBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WindowBox.SelectedItem is not string name || projectDir == null) return;
        activeWindow = name;
        selected = null;
        RenderDesigner();
        ShowProperties();
        MainTabs.SelectedIndex = 0;
    }
    private void AddWindow_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireProject();
            if (state.Language != "C#") throw new InvalidOperationException("Designerul cu mai multe ferestre este disponibil acum pentru C#/WPF.");
            var name = Microsoft.VisualBasic.Interaction.InputBox("Numele ferestrei:", "Fereastră nouă", "SecondWindow").Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$") || name == "MainWindow" || state.Windows.ContainsKey(name))
                throw new InvalidOperationException("Alege un nume unic, format din litere, cifre și _.");
            Snapshot();
            state.Windows[name] = new List<DesignItem>();
            WindowBox.Items.Add(name);
            WindowBox.SelectedItem = name;
            GenerateApp(); SaveProject();
        }
        catch (Exception ex) { Error(ex); }
    }
    private void AddComponent(string type, double x = 35, double y = 70)
    {
        RequireProject(); Snapshot();
        var large = type is "DataGrid" or "Calendar" or "TabControl" or "TreeView" or "ListView" or "GroupBox" or "Expander" or "RichTextBox";
        var next = 1;
        while (CurrentItems.Any(x => x.Name.Equals(type.ToLowerInvariant() + next, StringComparison.OrdinalIgnoreCase))) next++;
        var item = new DesignItem { Type = type, Name = type.ToLowerInvariant() + next, Text = type, X = x, Y = y, Width = type == "DataGrid" ? 420 : large ? 240 : 130, Height = type == "DataGrid" ? 220 : large ? 160 : 32 };
        CurrentItems.Add(item); selected = item; RenderDesigner(); ShowProperties(); GenerateApp(); SaveProject(); MainTabs.SelectedIndex = 0; FocusProperties();
    }
    private void Toolbox_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        try { if (Toolbox.SelectedItem is ListBoxItem item) AddComponent((item.Tag as string) ?? item.Content.ToString()!); }
        catch (Exception ex) { Error(ex); }
    }
    private void Toolbox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || Toolbox.SelectedItem is not ListBoxItem item) return;
        toolboxDragging = true;
        item.Background = Brush("#1765A7");
        item.RenderTransform = new TranslateTransform(1, 1);
        try { DragDrop.DoDragDrop(item, (item.Tag as string) ?? item.Content.ToString()!, DragDropEffects.Copy); }
        finally { toolboxDragging = false; item.Background = Brush("#24466D"); item.RenderTransform = Transform.Identity; }
    }
    private void DesignCanvas_Drop(object sender, DragEventArgs e)
    {
        try { if (e.Data.GetData(DataFormats.StringFormat) is string type) { var p = e.GetPosition(DesignCanvas); AddComponent(type, p.X, p.Y); } }
        catch (Exception ex) { Error(ex); }
    }
    private void ApplyProperties_Click(object sender, RoutedEventArgs e)
    {
        ApplyInspector();
    }
    private void DeleteComponent_Click(object sender, RoutedEventArgs e)
    {
        try { DeleteSelectedComponent(); }
        catch (Exception ex) { Error(ex); }
    }
    private void SaveConnection_Click(object sender, RoutedEventArgs e)
    {
        try { RequireProject(); Snapshot(); state.ConnectionString = ConnectionBox.Text; SaveProject(); Status("Conexiune salvată în proiect. Nu include parole aici."); }
        catch (Exception ex) { Error(ex); }
    }
    private void ChooseDatabase_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireProject();
            if (state.Language != "C#") throw new InvalidOperationException("Legarea vizuală SQLite este disponibilă pentru proiectele C#/WPF.");
            var picker = new Microsoft.Win32.OpenFileDialog { Title = "Alege o bază SQLite", Filter = "SQLite|*.db;*.sqlite;*.sqlite3|Toate fișierele|*.*" };
            if (picker.ShowDialog() != true) return;
            Snapshot();
            var directory = Path.Combine(projectDir!, "App", "Data");
            Directory.CreateDirectory(directory);
            var target = Path.Combine(directory, Path.GetFileName(picker.FileName));
            File.Copy(picker.FileName, target, true);
            state.DatabaseFile = Path.Combine("Data", Path.GetFileName(target));
            ConnectionBox.Text = state.DatabaseFile;
            GenerateApp(); SaveProject();
            ExploreDatabase_Click(sender, e);
        }
        catch (Exception ex) { Error(ex); }
    }
    private void ExploreDatabase_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireProject();
            var db = Path.GetFullPath(Path.Combine(projectDir!, "App", state.DatabaseFile));
            if (!db.StartsWith(Path.GetFullPath(Path.Combine(projectDir!, "App")) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(db))
                throw new InvalidOperationException("Alege mai întâi o bază SQLite din proiect.");
            DatabaseTablesBox.Items.Clear();
            using var connection = new SqliteConnection("Data Source=" + db + ";Mode=ReadOnly");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var reader = command.ExecuteReader();
            while (reader.Read()) DatabaseTablesBox.Items.Add(reader.GetString(0));
            if (DatabaseTablesBox.Items.Count > 0) DatabaseTablesBox.SelectedIndex = 0;
            Status(DatabaseTablesBox.Items.Count + " tabele găsite.");
        }
        catch (Exception ex) { Error(ex); }
    }
    private void BindTable_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireProject();
            if (selected?.Type != "DataGrid") throw new InvalidOperationException("Selectează un DataGrid din designer.");
            if (DatabaseTablesBox.SelectedItem is not string table) throw new InvalidOperationException("Alege un tabel SQLite.");
            Snapshot();
            selected.DataTable = table;
            GenerateApp(); SaveProject();
            Status("DataGrid-ul va afișa tabelul " + table + " la pornirea aplicației.");
        }
        catch (Exception ex) { Error(ex); }
    }
    private void SaveKey_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Provider == "Fără AI") return;
            if (ApiKeyBox.Password.Trim().Length == 0) throw new InvalidOperationException("Introdu o cheie API înainte de salvare.");
            Directory.CreateDirectory(Path.GetDirectoryName(KeyFile)!);
            File.WriteAllBytes(KeyFile, ProtectedData.Protect(Encoding.UTF8.GetBytes(ApiKeyBox.Password.Trim()), null, DataProtectionScope.CurrentUser));
            Status("Cheia " + Provider + " a fost salvată în profilul Windows.");
        }
        catch (Exception ex) { Error(ex); }
    }

    private string HistoryDir => Path.Combine(projectDir!, ".forge-history");
    private void Snapshot()
    {
        RequireProject(); SaveProject();
        Directory.CreateDirectory(HistoryDir);
        var snap = Path.Combine(HistoryDir, DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
        Directory.CreateDirectory(snap);
        CopyProject(projectDir!, snap);
    }
    private static void CopyProject(string source, string destination)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, file);
            var first = rel.Split(Path.DirectorySeparatorChar)[0];
            if (first is ".forge-history" or ".git" or "bin" or "obj" or "publish" or "publish-debug") continue;
            if (rel.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj")) continue;
            var target = Path.Combine(destination, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }
    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireProject();
            if (!Directory.Exists(HistoryDir)) throw new InvalidOperationException("Nu există versiuni anterioare.");
            var last = Directory.EnumerateDirectories(HistoryDir).OrderByDescending(x => x).FirstOrDefault();
            if (last == null) throw new InvalidOperationException("Nu există versiuni anterioare.");
            CopyProject(last, projectDir!);
            Directory.Delete(last, true);
            CloseAllEditors();
            OpenProject(projectDir!);
            Status("Versiunea anterioară a fost restaurată.");
        }
        catch (Exception ex) { Error(ex); }
    }
    private void History_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireProject();
            var snapshots = Directory.Exists(HistoryDir) ? Directory.EnumerateDirectories(HistoryDir).OrderByDescending(x => x).Take(20).ToList() : new();
            if (snapshots.Count == 0) throw new InvalidOperationException("Nu există încă versiuni salvate.");
            var list = string.Join("\n", snapshots.Select((p, i) => $"{i + 1}. {Path.GetFileName(p)}"));
            var answer = Microsoft.VisualBasic.Interaction.InputBox("Alege numărul versiunii:\n" + list, "Istoricul proiectului", "1");
            if (!int.TryParse(answer, out var index) || index < 1 || index > snapshots.Count) return;
            Snapshot();
            foreach (var file in Directory.EnumerateFiles(projectDir!, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(projectDir!, file);
                var first = rel.Split(Path.DirectorySeparatorChar)[0];
                if (first is ".forge-history" or "publish" or "publish-debug" or ".git") continue;
                if (rel.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj")) continue;
                File.Delete(file);
            }
            CopyProject(snapshots[index - 1], projectDir!);
            CloseAllEditors();
            OpenProject(projectDir!);
            Status("Versiunea selectată a fost restaurată.");
        }
        catch (Exception ex) { Error(ex); }
    }
    private async void Git_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireProject();
            var git = FindTool("git.exe") ?? throw new InvalidOperationException("Instalează Git pentru a folosi această funcție.");
            if (!Directory.Exists(Path.Combine(projectDir!, ".git")))
            {
                var init = new ProcessStartInfo(git) { WorkingDirectory = projectDir!, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                init.ArgumentList.Add("init");
                using var process = Process.Start(init)!;
                await process.WaitForExitAsync();
                if (process.ExitCode != 0) throw new InvalidOperationException(await process.StandardError.ReadToEndAsync());
                File.WriteAllText(Path.Combine(projectDir!, ".gitignore"), "bin/\nobj/\npublish/\npublish-debug/\n.forge-history/\n");
            }
            var psi = new ProcessStartInfo(git) { WorkingDirectory = projectDir!, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            psi.ArgumentList.Add("status"); psi.ArgumentList.Add("--short");
            using var statusProcess = Process.Start(psi)!;
            var output = await statusProcess.StandardOutput.ReadToEndAsync();
            await statusProcess.WaitForExitAsync();
            MainTabs.SelectedIndex = 2;
            BuildOutput.Text = "Git status:\n" + (output.Length == 0 ? "Nu există modificări." : output);
            Status("Repository Git pregătit. Pentru GitHub, adaugă o adresă remote.");
        }
        catch (Exception ex) { Error(ex); }
    }
    private async void ConnectGitHub_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireProject();
            var git = FindTool("git.exe") ?? throw new InvalidOperationException("Instalează Git.");
            if (!Directory.Exists(Path.Combine(projectDir!, ".git")))
            {
                var init = new ProcessStartInfo(git) { WorkingDirectory = projectDir!, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                init.ArgumentList.Add("init");
                using var initProcess = Process.Start(init)!;
                await initProcess.WaitForExitAsync();
                if (initProcess.ExitCode != 0) throw new InvalidOperationException("Nu am putut inițializa repository-ul Git.");
                File.WriteAllText(Path.Combine(projectDir!, ".gitignore"), "bin/\nobj/\npublish/\npublish-debug/\n.forge-history/\n");
            }
            var url = Microsoft.VisualBasic.Interaction.InputBox("Adresa repository-ului GitHub (HTTPS sau SSH):", "Conectare GitHub", "").Trim();
            if (url.Length == 0) return;
            if (!(url.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase) || url.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Introdu o adresă GitHub HTTPS sau SSH validă.");
            var get = new ProcessStartInfo(git) { WorkingDirectory = projectDir!, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            get.ArgumentList.Add("remote"); get.ArgumentList.Add("get-url"); get.ArgumentList.Add("origin");
            using var getProcess = Process.Start(get)!;
            await getProcess.WaitForExitAsync();
            var exists = getProcess.ExitCode == 0;
            var psi = new ProcessStartInfo(git) { WorkingDirectory = projectDir!, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            psi.ArgumentList.Add("remote");
            psi.ArgumentList.Add(exists ? "set-url" : "add");
            psi.ArgumentList.Add("origin"); psi.ArgumentList.Add(url);
            using var process = Process.Start(psi)!;
            var error = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0) throw new InvalidOperationException(await error);
            Status("GitHub conectat ca origin. Folosește Git pentru status.");
        }
        catch (Exception ex) { Error(ex); }
    }
    private async Task<string> RunGit(params string[] arguments)
    {
        RequireProject();
        var git = FindTool("git.exe") ?? throw new InvalidOperationException("Instalează Git pentru a folosi GitHub.");
        var psi = new ProcessStartInfo(git) { WorkingDirectory = projectDir!, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in arguments) psi.ArgumentList.Add(argument);
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Git nu a pornit.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var text = await output + await error;
        if (process.ExitCode != 0) throw new InvalidOperationException(text);
        return text;
    }
    private async void GitCommit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveProject();
            if (!Directory.Exists(Path.Combine(projectDir!, ".git"))) throw new InvalidOperationException("Inițializează mai întâi Git din meniu.");
            var message = Microsoft.VisualBasic.Interaction.InputBox("Descrie schimbarea:", "Commit", "").Trim();
            if (message.Length == 0) return;
            await RunGit("add", "-A");
            BuildOutput.Text = await RunGit("commit", "-m", message);
            MainTabs.SelectedIndex = 2;
            Status("Commit creat.");
        }
        catch (Exception ex) { Error(ex); }
    }
    private async void GitPush_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            BuildOutput.Text = await RunGit("push", "-u", "origin", "HEAD");
            MainTabs.SelectedIndex = 2;
            Status("Modificările au fost trimise la GitHub.");
        }
        catch (Exception ex) { Error(ex); }
    }
    private async void GitPull_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveProject();
            BuildOutput.Text = await RunGit("pull", "--rebase");
            CloseAllEditors();
            OpenProject(projectDir!);
            MainTabs.SelectedIndex = 2;
            Status("Proiectul a fost actualizat din GitHub.");
        }
        catch (Exception ex) { Error(ex); }
    }
    private void Dependencies_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireProject();
            if (state.Language != "C#") throw new InvalidOperationException("Installerul de dependențe externe este disponibil acum pentru aplicațiile C#/WPF.");
            var list = state.Dependencies.Count == 0 ? "Nu sunt dependențe externe declarate." : string.Join("\n", state.Dependencies.Select(x => "• " + x.Name + " → " + x.ProbePath));
            if (MessageBox.Show(list + "\n\nAdaugi o dependență cu installer local?", "Dependențe", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            var name = Microsoft.VisualBasic.Interaction.InputBox("Numele dependenței:", "Dependență nouă", "").Trim();
            if (name.Length == 0) return;
            var probe = Microsoft.VisualBasic.Interaction.InputBox("Calea fișierului care dovedește că este instalată (poate folosi %ProgramFiles%):", "Verificare instalare", "").Trim();
            if (probe.Length == 0) throw new InvalidOperationException("Este necesară o cale de verificare.");
            var picker = new Microsoft.Win32.OpenFileDialog { Title = "Alege installerul care va fi inclus în aplicație", Filter = "Installere|*.exe;*.msi|Toate fișierele|*.*" };
            if (picker.ShowDialog() != true) return;
            var args = Microsoft.VisualBasic.Interaction.InputBox("Argumentele installerului (opțional):", "Argumente instalare", "").Trim();
            Snapshot();
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(picker.FileName)));
            var filename = hash[..12] + "-" + Path.GetFileName(picker.FileName);
            var targetDir = Path.Combine(projectDir!, "App", "Prerequisites");
            Directory.CreateDirectory(targetDir);
            File.Copy(picker.FileName, Path.Combine(targetDir, filename), true);
            state.Dependencies.Add(new ExternalDependency { Name = name, ProbePath = probe, InstallerFile = filename, Sha256 = hash, Arguments = args });
            GenerateApp(); SaveProject();
            Status("Installerul a fost inclus în pachet; aplicația îl verifică la pornire.");
        }
        catch (Exception ex) { Error(ex); }
    }

    private void GenerateApp()
    {
        RequireProject();
        if (IsWebProject) { GenerateWebProject(); return; }
        var app = Path.Combine(projectDir!, "App");
        Directory.CreateDirectory(app);
        var ns = state.Name;
        if (state.Language != "C#")
        {
            if (state.Language == "VB")
            {
                File.WriteAllText(Path.Combine(app, ns + ".vbproj"), "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <OutputType>WinExe</OutputType>\n    <TargetFramework>net8.0-windows</TargetFramework>\n    <UseWindowsForms>true</UseWindowsForms>\n    <RootNamespace>" + ns + "</RootNamespace>\n  </PropertyGroup>\n</Project>\n");
                GenerateNativeUi();
                return;
            }
            var (filename, source) = state.Language switch
            {
                "C" => ("main.c", "#include <stdio.h>\n\nint main(void)\n{\n    puts(\"Hello from " + ns + "\");\n    return 0;\n}\n"),
                "C++" => ("main.cpp", "#include <iostream>\n\nint main()\n{\n    std::cout << \"Hello from " + ns + "\\n\";\n    return 0;\n}\n"),
                _ => ("main.pas", "program " + ns + ";\nbegin\n  writeln('Hello from " + ns + "');\nend.\n")
            };
            var path = Path.Combine(app, filename);
            if (!File.Exists(path)) File.WriteAllText(path, source);
            GenerateNativeUi();
            return;
        }
        var databaseExtras = state.DatabaseFile.Length == 0 ? "" : """
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.10" />
    <Content Include="Data\*" CopyToPublishDirectory="PreserveNewest" ExcludeFromSingleFile="true" />
""";
        var appIcon = state.Forms?.GetValueOrDefault("MainWindow")?.IconFile;
        var iconProperty = string.IsNullOrWhiteSpace(appIcon) ? "" : $"    <ApplicationIcon>{appIcon}</ApplicationIcon>\n";
        var nativeContent = File.Exists(Path.Combine(app, "NativeModules", "forge_native.c")) ? "    <Content Include=\"NativeModules\\forge_native.dll\" CopyToPublishDirectory=\"PreserveNewest\"><TargetPath>forge_native.dll</TargetPath></Content>\n" : "";
        File.WriteAllText(Path.Combine(app, ns + ".csproj"), $""""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
{iconProperty}  </PropertyGroup>
  <ItemGroup>
    <EmbeddedResource Include="dependencies.json" />
    <Content Include="Prerequisites\*" CopyToPublishDirectory="PreserveNewest" ExcludeFromSingleFile="true" />
    <Resource Include="Assets\*.ico" />
{nativeContent}{databaseExtras}  </ItemGroup>
</Project>
"""");
        File.WriteAllText(Path.Combine(app, "App.xaml"), $""""
<Application x:Class="{ns}.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
  <Application.Resources />
</Application>
"""");
        File.WriteAllText(Path.Combine(app, "App.xaml.cs"), $$"""
using System.Windows;

namespace {{ns}};

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, error) => ForgeWatch.Record("Unhandled UI exception", () => error.Exception.ToString(), "App", 0);
        AppDomain.CurrentDomain.UnhandledException += (_, error) => ForgeWatch.Record("Unhandled exception", () => error.ExceptionObject.ToString() ?? "unknown", "App", 0);
        System.Diagnostics.Trace.Listeners.Add(new ForgeTraceListener());
        if (!DependencyBootstrap.Ensure())
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);
        ForgeWatch.Record("Application", () => "started", "App", 0);
    }
}
""");
        File.WriteAllText(Path.Combine(app, "dependencies.json"), JsonSerializer.Serialize(state.Dependencies, jsonOptions));
        File.WriteAllText(Path.Combine(app, "DependencyBootstrap.cs"), DependencyBootstrapSource.Replace("__NAMESPACE__", ns));
        File.WriteAllText(Path.Combine(app, "ForgeWatch.g.cs"), ForgeWatchSource.Replace("__NAMESPACE__", ns));
        GenerateWindow(app, ns, "MainWindow", state.Items, state.DatabaseFile, state.Forms?.GetValueOrDefault("MainWindow"));
        foreach (var window in state.Windows) GenerateWindow(app, ns, window.Key, window.Value, state.DatabaseFile, state.Forms?.GetValueOrDefault(window.Key));
    }
    private const string DependencyBootstrapSource = """
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
namespace __NAMESPACE__;
internal sealed class DependencyEntry
{
    public string Name { get; set; } = "";
    public string ProbePath { get; set; } = "";
    public string InstallerFile { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string Arguments { get; set; } = "";
}
internal static class DependencyBootstrap
{
    public static bool Ensure()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var manifest = assembly.GetManifestResourceNames().First(x => x.EndsWith(".dependencies.json", StringComparison.OrdinalIgnoreCase));
            using var source = assembly.GetManifestResourceStream(manifest)!;
            var entries = JsonSerializer.Deserialize<List<DependencyEntry>>(source, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            foreach (var entry in entries)
            {
                var probe = Environment.ExpandEnvironmentVariables(entry.ProbePath);
                if (!Path.IsPathRooted(probe)) probe = Path.Combine(AppContext.BaseDirectory, probe);
                if (File.Exists(probe)) continue;
                if (MessageBox.Show(entry.Name + " lipsește. Instalezi acum dependența inclusă în aplicație?", "Dependență necesară", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return false;
                var installer = Path.Combine(AppContext.BaseDirectory, "Prerequisites", entry.InstallerFile);
                if (!File.Exists(installer)) throw new InvalidOperationException("Installerul inclus lipsește: " + entry.InstallerFile);
                var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(installer)));
                if (!actual.Equals(entry.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Verificarea integrității installerului a eșuat.");
                var msi = installer.EndsWith(".msi", StringComparison.OrdinalIgnoreCase);
                var psi = new ProcessStartInfo(msi ? "msiexec.exe" : installer)
                {
                    Arguments = msi ? "/i \"" + installer + "\" " + entry.Arguments : entry.Arguments,
                    UseShellExecute = true
                };
                using var process = Process.Start(psi) ?? throw new InvalidOperationException("Installerul nu a pornit.");
                process.WaitForExit();
                if (process.ExitCode != 0 || !File.Exists(probe)) throw new InvalidOperationException("Instalarea nu a fost confirmată: " + entry.Name);
            }
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Dependențe", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
}
""";
    private static void GenerateWindow(string app, string ns, string windowName, List<DesignItem> items, string databaseFile, FormSettings? settings)
    {
        settings ??= new FormSettings { Title = windowName };
        var root = new XElement(XName.Get("Window", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"),
            new XAttribute(XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml"),
            new XAttribute(XName.Get("Class", "http://schemas.microsoft.com/winfx/2006/xaml"), ns + "." + windowName),
            new XAttribute("Title", settings.Title), new XAttribute("Width", settings.Width), new XAttribute("Height", settings.Height),
            new XAttribute("MinWidth", settings.MinWidth), new XAttribute("MinHeight", settings.MinHeight),
            new XAttribute("Background", settings.Background), new XAttribute("Foreground", settings.Foreground),
            new XAttribute("ResizeMode", settings.ResizeMode), new XAttribute("SizeToContent", settings.SizeToContent),
            new XAttribute("WindowStyle", settings.WindowStyle),
            new XAttribute("WindowStartupLocation", settings.WindowStartupLocation), new XAttribute("WindowState", settings.WindowState),
            new XAttribute("Left", settings.Left), new XAttribute("Top", settings.Top),
            new XAttribute("Topmost", settings.Topmost), new XAttribute("ShowInTaskbar", settings.ShowInTaskbar),
            new XAttribute("ShowActivated", settings.ShowActivated), new XAttribute("Opacity", settings.Opacity),
            new XAttribute("FontFamily", settings.FontFamily), new XAttribute("FontSize", settings.FontSize),
            new XAttribute("FontWeight", settings.FontWeight), new XAttribute("FlowDirection", settings.FlowDirection));
        var bindings = items.Where(x => x.Type == "DataGrid" && x.DataTable.Length > 0).ToList();
        if (!string.IsNullOrWhiteSpace(settings.IconFile)) root.SetAttributeValue("Icon", settings.IconFile.Replace('\\', '/'));
        if (bindings.Count > 0 && databaseFile.Length > 0) root.SetAttributeValue("Loaded", "ForgeLoaded");
        var canvas = new XElement(XName.Get("Canvas", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"));
        foreach (var item in items)
        {
            if (item.Type is not ("Button" or "Label" or "TextBlock" or "TextBox" or "PasswordBox" or "RichTextBox" or "CheckBox" or "RadioButton" or "ToggleButton" or "ComboBox" or "ListBox" or "ListView" or "TreeView" or "DataGrid" or "DatePicker" or "Calendar" or "Slider" or "ProgressBar" or "TabControl" or "GroupBox" or "Expander" or "Border" or "Rectangle" or "Ellipse")) continue;
            var element = new XElement(XName.Get(item.Type, "http://schemas.microsoft.com/winfx/2006/xaml/presentation"),
                new XAttribute(XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml"), item.Name),
                new XAttribute("Width", item.Width), new XAttribute("Height", item.Height),
                new XAttribute("Canvas.Left", item.X), new XAttribute("Canvas.Top", item.Y));
            if (item.Type is "Button" or "Label" or "CheckBox" or "RadioButton" or "ToggleButton") element.SetAttributeValue("Content", item.Text);
            else if (item.Type is "TextBox" or "TextBlock") element.SetAttributeValue("Text", item.Text);
            else if (item.Type is "GroupBox" or "Expander") element.SetAttributeValue("Header", item.Text);
            else if (item.Type is "Slider" or "ProgressBar") { element.SetAttributeValue("Minimum", 0); element.SetAttributeValue("Maximum", 100); element.SetAttributeValue("Value", 40); }
            else if (item.Type is "Rectangle" or "Ellipse") { element.SetAttributeValue("Fill", "LightBlue"); element.SetAttributeValue("Stroke", "SteelBlue"); }
            else if (item.Type == "Border") { element.SetAttributeValue("BorderBrush", "SlateGray"); element.SetAttributeValue("BorderThickness", 1); }
            else if (item.Type == "TabControl")
            {
                element.Add(new XElement(XName.Get("TabItem", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), new XAttribute("Header", "Tab 1")));
                element.Add(new XElement(XName.Get("TabItem", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), new XAttribute("Header", "Tab 2")));
            }
            if (item.Background.Length > 0)
                element.SetAttributeValue(item.Type is "Rectangle" or "Ellipse" ? "Fill" : "Background", item.Background);
            if (item.Foreground.Length > 0 && item.Type is not ("Rectangle" or "Ellipse" or "Border"))
                element.SetAttributeValue("Foreground", item.Foreground);
            if (item.Type is not ("Rectangle" or "Ellipse" or "Border")) element.SetAttributeValue("FontSize", item.FontSize);
            if (item.ToolTip.Length > 0) element.SetAttributeValue("ToolTip", item.ToolTip);
            foreach (var option in item.Options ?? new()) element.SetAttributeValue(option.Key, option.Value);
            foreach (var eventHandler in item.Events ?? new()) element.SetAttributeValue(eventHandler.Key, eventHandler.Value);
            canvas.Add(element);
        }
        root.Add(canvas);
        File.WriteAllText(Path.Combine(app, windowName + ".xaml"), new XDocument(root).ToString());
        var codePath = Path.Combine(app, windowName + ".xaml.cs");
        if (!File.Exists(codePath)) File.WriteAllText(codePath, WindowCodeBehind(ns, windowName));
        var bindingPath = Path.Combine(app, windowName + ".Data.g.cs");
        if (bindings.Count == 0 || databaseFile.Length == 0)
        {
            if (File.Exists(bindingPath)) File.Delete(bindingPath);
            return;
        }
        var bindingCode = new StringBuilder($"using System; using System.Data; using System.IO; using System.Windows; using Microsoft.Data.Sqlite; namespace {ns}; public partial class {windowName} {{ private void ForgeLoaded(object sender, RoutedEventArgs e) {{ ");
        foreach (var binding in bindings)
        {
            var table = binding.DataTable.Replace("\"", "\"\"");
            bindingCode.Append("using (var db = new SqliteConnection(")
                .Append(JsonSerializer.Serialize("Data Source=")).Append(" + Path.Combine(AppContext.BaseDirectory, ")
                .Append(JsonSerializer.Serialize(databaseFile)).Append("))) { db.Open(); using var cmd = db.CreateCommand(); cmd.CommandText = ")
                .Append(JsonSerializer.Serialize("SELECT * FROM \"" + table + "\" LIMIT 500")).Append("; using var reader = cmd.ExecuteReader(); var data = new DataTable(); data.Load(reader); ")
                .Append(binding.Name).Append(".ItemsSource = data.DefaultView; } ");
        }
        bindingCode.Append("} }");
        File.WriteAllText(bindingPath, bindingCode.ToString());
    }

    private static string? FindTool(params string[] names)
    {
        var roots = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Tools"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "tools")),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FormaliniForge", "Tools"),
            @"C:\FPC", @"C:\lazarus", @"C:\msys64", @"C:\ProgramData\chocolatey\bin"
        };
        foreach (var root in roots.Where(Directory.Exists))
            foreach (var name in names)
            {
                try
                {
                    var match = Directory.EnumerateFiles(root, name, SearchOption.AllDirectories).FirstOrDefault();
                    if (match != null) return match;
                }
                catch { }
            }
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            foreach (var name in names)
            {
                try { var full = Path.Combine(dir, name); if (File.Exists(full)) return full; }
                catch { }
            }
        return null;
    }
    private async Task<bool> BuildProject(bool debug = false)
    {
        if (IsWebProject) return await BuildWebProject(debug);
        RequireProject(); SaveProject(); MainTabs.SelectedIndex = 2;
        BuildOutput.Text = T("Construiesc aplicația...") + "\n"; RunStatus.Text = english ? "Building" : "Se compilează"; Status("Compilare în curs...");
        var output = Path.Combine(projectDir!, debug ? "publish-debug" : "publish");
        Directory.CreateDirectory(output);
        if (state.Language == "C#" && !await BuildHybridModule(debug)) return false;
        var psi = new ProcessStartInfo { WorkingDirectory = projectDir!, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        if (state.Language is "C#" or "VB")
        {
            psi.FileName = FindTool("dotnet.exe") ?? throw new InvalidOperationException("Instalează .NET SDK 8 pentru a construi aplicația.");
            psi.ArgumentList.Add("publish"); psi.ArgumentList.Add(Path.Combine(projectDir!, "App", state.Name + (state.Language == "VB" ? ".vbproj" : ".csproj")));
            psi.ArgumentList.Add("-c"); psi.ArgumentList.Add(debug ? "Debug" : "Release"); psi.ArgumentList.Add("-r"); psi.ArgumentList.Add("win-x64");
            psi.ArgumentList.Add("--self-contained"); psi.ArgumentList.Add("true");
            psi.ArgumentList.Add("-p:PublishSingleFile=false");
            psi.ArgumentList.Add("-v"); psi.ArgumentList.Add("normal");
            psi.ArgumentList.Add("-o"); psi.ArgumentList.Add(output);
        }
        else
        {
            if (!File.Exists(NativeUiSource)) GenerateNativeUi();
            var source = NativeUiSource;
            var exe = Path.Combine(output, state.Name + ".exe");
            if (state.Language == "Pascal")
            {
                psi.FileName = FindTool("fpc.exe") ?? throw new InvalidOperationException("Pentru Pascal instalează Free Pascal și adaugă fpc.exe în PATH.");
                psi.ArgumentList.Add(source); psi.ArgumentList.Add("-o" + exe);
            }
            else
            {
                var compiler = FindTool(state.Language == "C++" ? "g++.exe" : "gcc.exe", state.Language == "C++" ? "clang++.exe" : "clang.exe");
                if (compiler == null)
                {
                    if (!await ConfigureMsvc(psi, source, exe, debug))
                        throw new InvalidOperationException("Pentru C/C++ instalează Visual Studio Build Tools, GCC sau Clang.");
                }
                else
                {
                    psi.FileName = compiler;
                    psi.ArgumentList.Add(source); psi.ArgumentList.Add("-o"); psi.ArgumentList.Add(exe);
                    psi.ArgumentList.Add("-mwindows"); psi.ArgumentList.Add("-lcomctl32");
                    if (Path.GetFileName(psi.FileName).StartsWith("g", StringComparison.OrdinalIgnoreCase))
                        psi.ArgumentList.Add("-static");
                    if (debug) psi.ArgumentList.Add("-g");
                }
            }
        }
        AppendBuildLine(psi.FileName + " " + string.Join(" ", psi.ArgumentList));
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Compilatorul nu a pornit.");
        await Task.WhenAll(PumpProcessOutput(process.StandardOutput, ""), PumpProcessOutput(process.StandardError, "ERROR "), process.WaitForExitAsync());
        lastBuiltExe = process.ExitCode == 0 ? Path.Combine(output, state.Name + ".exe") : null;
        RunStatus.Text = lastBuiltExe == null ? (english ? "Build failed" : "Compilare eșuată") : (english ? "Build succeeded" : "Compilare reușită");
        AppendBuildLine("Build exited with code " + process.ExitCode + ".");
        Status(lastBuiltExe == null ? "Compilarea a eșuat. Vezi jurnalul." : "EXE creat: " + lastBuiltExe);
        return lastBuiltExe != null;
    }
    private async void Build_Click(object sender, RoutedEventArgs e)
    {
        try { if (await BuildProject() && IsWebProject) await RunWebProject(); } catch (Exception ex) { Error(ex); }
    }
    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!await BuildProject(debug: true)) return;
            if (IsWebProject) { await RunWebProject(); return; }
            await LaunchAndMonitor(lastBuiltExe!);
        }
        catch (Exception ex) { Error(ex); }
    }
    private async void Debug_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!await BuildProject(debug: true)) return;
            if (IsWebProject) { await RunWebProject(); return; }
            await LaunchAndMonitor(lastBuiltExe!);
        }
        catch (Exception ex) { Error(ex); }
    }
    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireProject();
            if (state.Language != "C#") throw new InvalidOperationException("Pentru acest limbaj, cere agentului să creeze un proiect de teste și configurează rularea lui.");
            var testProjects = Directory.EnumerateFiles(projectDir!, "*Tests.csproj", SearchOption.AllDirectories).Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)).ToList();
            if (testProjects.Count == 0) throw new InvalidOperationException("Nu există încă proiect de teste. Cere agentului: «Creează teste unitare pentru aplicație».");
            SaveProject(); MainTabs.SelectedIndex = 2; BuildOutput.Text = "Rulez testele...\n";
            foreach (var testProject in testProjects)
            {
                var psi = new ProcessStartInfo(FindTool("dotnet.exe") ?? "dotnet") { WorkingDirectory = projectDir!, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                psi.ArgumentList.Add("test"); psi.ArgumentList.Add(testProject); psi.ArgumentList.Add("-c"); psi.ArgumentList.Add("Release");
                using var process = Process.Start(psi)!;
                var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                BuildOutput.Text += await stdout + await stderr;
                if (process.ExitCode != 0) { Status("Unele teste au eșuat."); return; }
            }
            Status("Testele au trecut.");
        }
        catch (Exception ex) { Error(ex); }
    }
    private void RepairBuild_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(BuildOutput.Text))
        {
            Status("Rulează mai întâi Construiește EXE sau Testează pentru a obține un jurnal.");
            return;
        }
        PromptBox.Text = "Analizează următoarea eroare de build sau test, repară fișierele relevante și explică modificarea. Nu modifica fișiere fără legătură.\n\n" + BuildOutput.Text[^Math.Min(BuildOutput.Text.Length, 7000)..];
        SendPrompt_Click(sender, e);
    }
    private void Dictation_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (speechEngine != null)
            {
                speechEngine.RecognizeAsyncCancel();
                speechEngine.Dispose();
                speechEngine = null;
                DictationButton.Content = "🎙 Dictează";
                Status("Dictarea s-a oprit.");
                return;
            }
            var recognizer = SpeechRecognitionEngine.InstalledRecognizers().FirstOrDefault()
                ?? throw new InvalidOperationException("Windows nu are un pachet de recunoaștere vocală instalat.");
            speechEngine = new SpeechRecognitionEngine(recognizer);
            speechEngine.LoadGrammar(new DictationGrammar());
            speechEngine.SetInputToDefaultAudioDevice();
            speechEngine.SpeechRecognized += (_, args) =>
            {
                if (args.Result.Confidence < 0.45) return;
                Dispatcher.BeginInvoke(() => PromptBox.Text += (PromptBox.Text.EndsWith(' ') || PromptBox.Text.Length == 0 ? "" : " ") + args.Result.Text);
            };
            speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            DictationButton.Content = "■ Oprește dictarea";
            Status("Dictarea este activă.");
        }
        catch (Exception ex) { speechEngine?.Dispose(); speechEngine = null; Error(ex); }
    }

    private async void SendPrompt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireProject();
            var prompt = PromptBox.Text.Trim();
            if (prompt.Length == 0) return;
            if (Provider == "Fără AI") throw new InvalidOperationException("Agentul AI este oprit. Alege OpenAI sau Gemini în Setări dacă vrei să îl folosești.");
            var key = ApiKeyBox.Password.Trim();
            if (key.Length == 0) throw new InvalidOperationException("Introdu cheia API pentru " + Provider + " în Setări.");
            var model = ModelBox.Text.Trim();
            if (model.Length == 0) throw new InvalidOperationException("Introdu numele modelului API.");
            PromptBox.IsEnabled = false;
            ChatLog.AppendText("\nTU: " + prompt + "\n");
            Status("Agentul analizează proiectul...");
            SaveProject();
            var files = EnumerateProjectSourceFiles(projectDir!)
                .Where(p => new FileInfo(p).Length < 100_000).Take(30)
                .Select(p => new { path = Path.GetRelativePath(projectDir!, p).Replace('\\', '/'), content = File.ReadAllText(p) }).ToArray();
            var instruction = """
You are an AI agent inside an application builder. Return ONLY JSON with fields message (string), files (array of {path,content}), and items (array of designer components {type,name,text,x,y,width,height}) or null if unchanged. The app source lives under App/. If targetFile is set, change only that file and leave items null. For every supported language (C#, C, C++, Pascal, VB, Vue, React, React Native), change the visual form with items; the host regenerates its UI source. Vue components are Heading, Text, Button, Input, TextArea, Image, CheckBox, Select, Container, Link, List, Table. React Native components are View, Text, TextInput, Pressable, Image, Switch, ScrollView, FlatList. Preserve existing component names and event mappings whenever possible. Use files for nonvisual code, including App/src/ForgeScreen files that are user-editable wrappers. Never include secrets. Keep code buildable. Do not overwrite forge.json, App/App.xaml, App/App.xaml.cs, App/ForgeWatch.g.cs, App/*.csproj, App/*.vbproj, or generated App/src/ForgeLayout files. Existing project context follows.
""";
            var context = JsonSerializer.Serialize(new { request = prompt, targetFile = pendingAiTarget, selectedWindow = activeWindow, language = state.Language, state, files });
            string resultText;
            if (Provider == "OpenAI")
            {
                var request = new { model, instructions = instruction, input = context, store = false };
                using var msg = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
                msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                msg.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                using var response = await http.SendAsync(msg);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) throw new InvalidOperationException("OpenAI: " + body);
                using var outer = JsonDocument.Parse(body);
                RecordUsage(outer.RootElement, true);
                var parts = outer.RootElement.GetProperty("output").EnumerateArray()
                    .Where(x => x.TryGetProperty("type", out var type) && type.GetString() == "message")
                    .SelectMany(x => x.GetProperty("content").EnumerateArray())
                    .Where(x => x.TryGetProperty("type", out var type) && type.GetString() == "output_text")
                    .Select(x => x.GetProperty("text").GetString());
                resultText = string.Concat(parts);
            }
            else
            {
                var request = new
                {
                    system_instruction = new { parts = new[] { new { text = instruction } } },
                    contents = new[] { new { role = "user", parts = new[] { new { text = context } } } },
                    generationConfig = new { responseMimeType = "application/json" }
                };
                using var msg = new HttpRequestMessage(HttpMethod.Post, "https://generativelanguage.googleapis.com/v1beta/models/" + Uri.EscapeDataString(model) + ":generateContent");
                msg.Headers.Add("x-goog-api-key", key);
                msg.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                using var response = await http.SendAsync(msg);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Gemini: " + body);
                using var outer = JsonDocument.Parse(body);
                if (outer.RootElement.TryGetProperty("usageMetadata", out var usageMetadata))
                {
                    using var wrapper = JsonDocument.Parse("{\"usage\":" + usageMetadata.GetRawText() + "}");
                    RecordUsage(wrapper.RootElement, false);
                }
                else RecordUsage(outer.RootElement, false);
                resultText = outer.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "{}";
            }
            using var result = JsonDocument.Parse(resultText);
            var root = result.RootElement;
            Snapshot();
            if (root.TryGetProperty("files", out var changes) && changes.ValueKind == JsonValueKind.Array)
                foreach (var file in changes.EnumerateArray())
                {
                    var rel = file.GetProperty("path").GetString() ?? "";
                    if (pendingAiTarget != null && !rel.Replace('\\', '/').Equals(pendingAiTarget, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Agentul a propus un fișier diferit de cel selectat: " + rel);
                    var full = Path.GetFullPath(Path.Combine(projectDir!, rel));
                    if (!full.StartsWith(Path.GetFullPath(projectDir!) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                        rel.Replace('\\', '/').Split('/').Any(part => part is ".forge-history" or ".git" or "bin" or "obj" or "publish" or "publish-debug" or "node_modules" or "dist" or ".expo" or ".forge-npm-cache") ||
                        rel.Equals("forge.json", StringComparison.OrdinalIgnoreCase) || rel.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || rel.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase) || rel.EndsWith("App.xaml", StringComparison.OrdinalIgnoreCase) || rel.EndsWith("App.xaml.cs", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Agentul a propus o cale de fișier nepermisă: " + rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                    File.WriteAllText(full, file.GetProperty("content").GetString() ?? "");
                }
            if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                if (pendingAiTarget != null) throw new InvalidOperationException("Agentul a propus schimbări de formular în locul fișierului selectat.");
                var updated = JsonSerializer.Deserialize<List<DesignItem>>(items.GetRawText(), jsonOptions) ?? new();
                foreach (var item in updated)
                    if ((item.Events == null || item.Events.Count == 0) && CurrentItems.FirstOrDefault(old => old.Name == item.Name) is { } oldItem)
                        item.Events = new Dictionary<string, string>(oldItem.Events ?? new());
                if (activeWindow == "MainWindow") state.Items = updated; else state.Windows[activeWindow] = updated;
                GenerateApp();
            }
            var reopenTarget = pendingAiTarget == null ? null : Path.Combine(projectDir!, pendingAiTarget.Replace('/', Path.DirectorySeparatorChar));
            selected = null;
            CloseAllEditors();
            SaveProject(); RenderDesigner(); RefreshFiles();
            if (reopenTarget != null && File.Exists(reopenTarget)) OpenEditor(reopenTarget);
            ChatLog.AppendText("\nAGENT: " + (root.TryGetProperty("message", out var m) ? m.GetString() : "Modificări aplicate.") + "\n");
            PromptBox.Text = ""; Status("Modificările au fost aplicate. Poți folosi Revino.");
        }
        catch (Exception ex) { Error(ex); ChatLog.AppendText("\nEROARE: " + ex.Message + "\n"); }
        finally { PromptBox.IsEnabled = true; pendingAiTarget = null; }
    }
}
