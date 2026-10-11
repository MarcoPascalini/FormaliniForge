using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TextBox = System.Windows.Controls.TextBox;
using ListBox = System.Windows.Controls.ListBox;
using Orientation = System.Windows.Controls.Orientation;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using MessageBox = System.Windows.MessageBox;

namespace FormaliniForge;

internal sealed class SnippetCompletion : ICompletionData
{
    private readonly ForgeExtensionSnippet snippet;
    public SnippetCompletion(ForgeExtensionSnippet snippet) => this.snippet = snippet;
    public System.Windows.Media.ImageSource? Image => null;
    public string Text => snippet.Trigger;
    public object Content => snippet.Trigger;
    public object Description => snippet.Description;
    public double Priority => 1;
    public void Complete(TextArea area, ISegment segment, EventArgs e) => area.Document.Replace(segment, snippet.Body);
}

public partial class MainWindow
{
    private ExtensionCatalog? extensionCatalog;
    private void LoadExtensions()
    {
        extensionCatalog = new ExtensionCatalog(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FormaliniForge", "Extensions"));
        extensionCatalog.Reload();
    }

    private IEnumerable<ForgeExtensionSnippet> MatchingSnippets(string path, string prefix) =>
        extensionCatalog?.Items.Where(x => x.Enabled).SelectMany(x => x.Manifest.Snippets)
            .Where(x => x.Trigger.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                (x.Extensions.Count == 0 || x.Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)))
        ?? Enumerable.Empty<ForgeExtensionSnippet>();

    private void CommandPalette_Click(object sender, RoutedEventArgs e)
    {
        var entries = new List<(string Name, Action Run)>
        {
            (T("Proiect nou"), () => NewProject_Click(this, new RoutedEventArgs())),
            (T("Deschide proiect"), () => OpenProject_Click(this, new RoutedEventArgs())),
            (T("Salvează"), () => Save_Click(this, new RoutedEventArgs())),
            (T("Caută în proiect"), () => SearchProject_Click(this, new RoutedEventArgs())),
            (T("Formatează documentul"), () => FormatDocument_Click(this, new RoutedEventArgs())),
            (T("Generează EXE"), () => Build_Click(this, new RoutedEventArgs())),
            (T("Rulează"), () => Run_Click(this, new RoutedEventArgs())),
            (T("Gestionează extensiile..."), () => ManageExtensions_Click(this, new RoutedEventArgs()))
        };
        foreach (var installed in extensionCatalog?.Items.Where(x => x.Enabled) ?? Enumerable.Empty<InstalledForgeExtension>())
            foreach (var command in installed.Manifest.Commands)
            {
                var contribution = command;
                entries.Add((installed.Manifest.Name + ": " + command.Title, () => InsertExtensionText(contribution.InsertText)));
            }
        var dialog = new Window { Title = T("Paletă de comenzi"), Owner = this, Width = 620, Height = 430,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = System.Windows.Media.Brushes.White };
        var panel = new DockPanel { Margin = new Thickness(12) };
        var query = new TextBox { FontSize = 17, Margin = new Thickness(0, 0, 0, 8) };
        var list = new ListBox { DisplayMemberPath = "Name" };
        DockPanel.SetDock(query, Dock.Top); panel.Children.Add(query); panel.Children.Add(list); dialog.Content = panel;
        void Refresh()
        {
            list.ItemsSource = entries.Where(x => x.Name.Contains(query.Text, StringComparison.OrdinalIgnoreCase))
                .Select(x => new PaletteItem(x.Name, x.Run)).ToList();
            if (list.Items.Count > 0) list.SelectedIndex = 0;
        }
        void Invoke()
        {
            if (list.SelectedItem is not PaletteItem item) return;
            dialog.Close(); Dispatcher.BeginInvoke(item.Run);
        }
        query.TextChanged += (_, _) => Refresh();
        query.KeyDown += (_, args) => { if (args.Key == Key.Enter) Invoke(); else if (args.Key == Key.Down) list.Focus(); };
        list.MouseDoubleClick += (_, _) => Invoke();
        list.KeyDown += (_, args) => { if (args.Key == Key.Enter) Invoke(); };
        Refresh(); dialog.ShowDialog();
    }

    private sealed record PaletteItem(string Name, Action Run);

    private void InsertExtensionText(string value)
    {
        var editor = CurrentEditor;
        if (editor == null) { Status("Deschide mai întâi un fișier de cod."); return; }
        editor.Document.Replace(editor.SelectionStart, editor.SelectionLength, value);
        editor.CaretOffset = editor.SelectionStart + value.Length;
        editor.Focus();
    }

    private void FormatDocument_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var editor = CurrentEditor ?? throw new InvalidOperationException("Deschide mai întâi un fișier.");
            var path = (EditorTabs.SelectedItem as TabItem)?.Tag as string ?? "";
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var formatted = ext switch
            {
                ".json" => JsonSerializer.Serialize(JsonDocument.Parse(editor.Text).RootElement, new JsonSerializerOptions { WriteIndented = true }),
                ".xml" or ".xaml" or ".csproj" or ".vbproj" => System.Xml.Linq.XDocument.Parse(editor.Text).ToString(),
                ".html" or ".htm" => FormatHtmlSource(editor.Text).TrimEnd(),
                _ => throw new NotSupportedException("Formatarea locală este disponibilă acum pentru HTML, JSON, XML și XAML.")
            };
            editor.Document.Replace(0, editor.Document.TextLength, formatted + Environment.NewLine);
            Status("Document formatat.");
        }
        catch (Exception ex) { Error(ex); }
    }

    private void ManageExtensions_Click(object sender, RoutedEventArgs e)
    {
        if (extensionCatalog == null) LoadExtensions();
        var dialog = new Window { Title = T("Gestionează extensiile..."), Owner = this, Width = 680, Height = 470,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = System.Windows.Media.Brushes.White };
        var grid = new Grid { Margin = new Thickness(14) };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var list = new ListBox(); Grid.SetRow(list, 0); grid.Children.Add(list);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
        Grid.SetRow(buttons, 1); grid.Children.Add(buttons);
        System.Windows.Controls.Button Add(string label, Action action)
        {
            var button = new System.Windows.Controls.Button { Content = label, Margin = new Thickness(0, 0, 8, 0) };
            button.Click += (_, _) => { try { action(); Refresh(); } catch (Exception ex) { MessageBox.Show(ex.Message, dialog.Title); } };
            buttons.Children.Add(button); return button;
        }
        InstalledForgeExtension? Selected() => list.SelectedItem is ExtensionRow row ? row.Extension : null;
        void Refresh() => list.ItemsSource = extensionCatalog!.Items.Select(x => new ExtensionRow(x)).ToList();
        Add(english ? "Install ZIP..." : "Instalează ZIP...", () =>
        {
            var picker = new Microsoft.Win32.OpenFileDialog { Filter = "Forge extension (*.zip)|*.zip" };
            if (picker.ShowDialog(dialog) == true) extensionCatalog!.Install(picker.FileName);
        });
        Add(english ? "Enable / Disable" : "Activează / Dezactivează", () =>
        {
            var item = Selected(); if (item != null) extensionCatalog!.SetEnabled(item, !item.Enabled);
        });
        Add(english ? "Remove" : "Elimină", () =>
        {
            var item = Selected();
            if (item != null && MessageBox.Show((english ? "Remove " : "Elimini ") + item.Manifest.Name + "?", dialog.Title,
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) extensionCatalog!.Remove(item);
        });
        Refresh(); dialog.Content = grid; dialog.ShowDialog();
    }

    private sealed class ExtensionRow
    {
        public InstalledForgeExtension Extension { get; }
        public ExtensionRow(InstalledForgeExtension extension) => Extension = extension;
        public override string ToString() => $"{Extension.Manifest.Name}  {Extension.Manifest.Version}  [{(Extension.Enabled ? "on" : "off")}]  — {Extension.Manifest.Description}";
    }
}
