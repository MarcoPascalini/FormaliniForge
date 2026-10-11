using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Wpf;

namespace FormaliniForge;

internal sealed class HtmlVisualDocument
{
    public required WebView2 View { get; init; }
    public required RowDefinition CodeRow { get; init; }
    public required RowDefinition SplitterRow { get; init; }
    public required RowDefinition VisualRow { get; init; }
    public required GridSplitter Splitter { get; init; }
    public required DispatcherTimer RefreshTimer { get; init; }
    public bool SyncingFromVisual { get; set; }
    public bool FrameworkShell { get; set; }
    public bool Ready { get; set; }
    public string LastRenderedSource { get; set; } = "";
    public List<string> Scripts { get; } = new();
}

public partial class MainWindow
{
    private void AttachHtmlVisualEditor(EditorDocument document)
    {
        var grid = new Grid { Background = System.Windows.Media.Brushes.White };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var codeRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 100 };
        var splitterRow = new RowDefinition { Height = new GridLength(5) };
        var visualRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 100 };
        grid.RowDefinitions.Add(codeRow); grid.RowDefinitions.Add(splitterRow); grid.RowDefinitions.Add(visualRow);
        var toolbar = new WrapPanel { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(23, 32, 51)), Margin = new Thickness(0, 0, 0, 2) };
        Grid.SetRow(toolbar, 0); grid.Children.Add(toolbar);
        var view = new WebView2 { DefaultBackgroundColor = System.Drawing.Color.White };
        document.Tab.Content = null;
        Grid.SetRow(document.Editor, 1); grid.Children.Add(document.Editor);
        var splitter = new GridSplitter { Height = 5, HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, Background = System.Windows.Media.Brushes.SlateGray };
        Grid.SetRow(splitter, 2); grid.Children.Add(splitter);
        Grid.SetRow(view, 3); grid.Children.Add(view);
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(850) };
        var visual = new HtmlVisualDocument { View = view, CodeRow = codeRow, SplitterRow = splitterRow, VisualRow = visualRow, Splitter = splitter, RefreshTimer = timer };
        document.HtmlVisual = visual;
        document.Tab.Content = grid;
        System.Windows.Controls.Button Add(string label, Action action)
        {
            var button = new System.Windows.Controls.Button { Content = label, Margin = new Thickness(3, 3, 0, 3), Padding = new Thickness(8, 3, 8, 3), MinWidth = 44 };
            button.Click += (_, _) => action();
            toolbar.Children.Add(button);
            return button;
        }
        Add("Cod", () => SetHtmlEditorMode(visual, "code"));
        Add("Vizual", () => SetHtmlEditorMode(visual, "visual"));
        Add("Împărțit", () => SetHtmlEditorMode(visual, "split"));
        Add("Reîncarcă previzualizarea", () => _ = RefreshHtmlPreviewAsync(document));
        Add("Titlu H1", () => _ = InsertHtmlAsync(document, "<h1>Titlu</h1>"));
        Add("Paragraf", () => _ = InsertHtmlAsync(document, "<p>Text nou</p>"));
        Add("Buton", () => _ = InsertHtmlAsync(document, "<button type=\"button\">Buton</button>"));
        Add("Tabel", () => _ = InsertHtmlAsync(document, "<table border=\"1\"><tbody><tr><th>Coloană</th></tr><tr><td>Valoare</td></tr></tbody></table>"));
        Add("Aldin", () => _ = HtmlCommandAsync(document, "bold"));
        Add("Cursiv", () => _ = HtmlCommandAsync(document, "italic"));
        Add("Link", () =>
        {
            var url = Microsoft.VisualBasic.Interaction.InputBox(T("Adresa linkului"), T("Link"));
            if (url.Length > 0) _ = HtmlCommandAsync(document, "createLink", url);
        });
        Add("Imagine", () =>
        {
            var url = Microsoft.VisualBasic.Interaction.InputBox(T("Adresa imaginii"), T("Imagine"));
            if (url.Length > 0) _ = InsertHtmlAsync(document, "<img src=\"" + System.Net.WebUtility.HtmlEncode(url) + "\" alt=\"Imagine\" />");
        });
        LocalizeTree(toolbar);
        timer.Tick += async (_, _) => { timer.Stop(); await RefreshHtmlPreviewAsync(document); };
        document.Editor.TextChanged += (_, _) =>
        {
            if (visual.SyncingFromVisual) return;
            timer.Stop(); timer.Start();
        };
        _ = InitializeHtmlVisualAsync(document);
    }

    private static void SetHtmlEditorMode(HtmlVisualDocument visual, string mode)
    {
        visual.CodeRow.MinHeight = mode == "visual" ? 0 : 100;
        visual.VisualRow.MinHeight = mode == "code" ? 0 : 100;
        visual.CodeRow.Height = mode == "visual" ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        visual.VisualRow.Height = mode == "code" ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        visual.SplitterRow.Height = mode == "split" ? new GridLength(5) : new GridLength(0);
        visual.Splitter.Visibility = mode == "split" ? Visibility.Visible : Visibility.Collapsed;
        visual.View.Visibility = mode == "code" ? Visibility.Collapsed : Visibility.Visible;
    }

    private async Task InitializeHtmlVisualAsync(EditorDocument document)
    {
        var visual = document.HtmlVisual!;
        try
        {
            await Dispatcher.Yield(DispatcherPriority.Loaded);
            await visual.View.EnsureCoreWebView2Async(await GetBrowserEnvironmentAsync());
            visual.View.CoreWebView2.WebMessageReceived += (_, e) =>
            {
                try
                {
                    using var json = JsonDocument.Parse(e.WebMessageAsJson);
                    if (json.RootElement.GetProperty("kind").GetString() != "forge-html-edit" || visual.FrameworkShell) return;
                    ApplyHtmlVisualChange(document, json.RootElement.GetProperty("body").GetString() ?? "");
                }
                catch { }
            };
            visual.View.NavigationCompleted += async (_, e) =>
            {
                if (!e.IsSuccess || visual.FrameworkShell) return;
                await visual.View.CoreWebView2.ExecuteScriptAsync("document.designMode='on';window.__forgeDirty=false;document.addEventListener('input',()=>{window.__forgeDirty=true;chrome.webview.postMessage({kind:'forge-html-edit',body:document.body.innerHTML})});");
                visual.Ready = true;
            };
            await RefreshHtmlPreviewAsync(document);
        }
        catch (Exception ex) { Status((english ? "HTML visual editor: " : "Editor vizual HTML: ") + ex.Message); }
    }

    private async Task RefreshHtmlPreviewAsync(EditorDocument document)
    {
        var visual = document.HtmlVisual;
        if (visual?.View.CoreWebView2 == null) return;
        var source = document.Editor.Text;
        visual.Ready = false;
        visual.LastRenderedSource = source;
        visual.FrameworkShell = source.Contains("id=\"root\"", StringComparison.OrdinalIgnoreCase) &&
            (source.Contains("/src/main.jsx", StringComparison.OrdinalIgnoreCase) || source.Contains("/src/main.js", StringComparison.OrdinalIgnoreCase));
        if (visual.FrameworkShell)
        {
            visual.View.NavigateToString("<!doctype html><html><body style='font:16px system-ui;padding:24px;color:#24344a'><h2>" +
                T("Pagina este construită din componente React/Vue") + "</h2><p>" +
                T("Editează interfața în Designer; aici poți modifica structura HTML în modul Cod.") + "</p></body></html>");
            return;
        }
        visual.Scripts.Clear();
        var preview = Regex.Replace(source, @"<script\b[^>]*>[\s\S]*?</script\s*>", match =>
        {
            var index = visual.Scripts.Count;
            visual.Scripts.Add(match.Value);
            return "<!--FORGE_SCRIPT_" + index + "-->";
        }, RegexOptions.IgnoreCase);
        if (!Regex.IsMatch(preview, @"<html\b", RegexOptions.IgnoreCase)) preview = "<!doctype html><html><head></head><body>" + preview + "</body></html>";
        var baseUri = new Uri(Path.GetDirectoryName(document.Path)! + Path.DirectorySeparatorChar).AbsoluteUri;
        preview = Regex.Replace(preview, @"<head\b[^>]*>", match => match.Value + "<base href=\"" + System.Net.WebUtility.HtmlEncode(baseUri) + "\"/>", RegexOptions.IgnoreCase);
        visual.View.NavigateToString(preview);
        await Task.CompletedTask;
    }

    private void ApplyHtmlVisualChange(EditorDocument document, string body)
    {
        var visual = document.HtmlVisual!;
        for (var i = 0; i < visual.Scripts.Count; i++) body = body.Replace("<!--FORGE_SCRIPT_" + i + "-->", visual.Scripts[i], StringComparison.Ordinal);
        var source = document.Editor.Text;
        var match = Regex.Match(source, @"(?is)<body\b[^>]*>(?<body>.*?)</body\s*>");
        var updated = match.Success
            ? source[..match.Groups["body"].Index] + body + source[(match.Groups["body"].Index + match.Groups["body"].Length)..]
            : body;
        if (source == updated) return;
        visual.SyncingFromVisual = true;
        try { document.Editor.Text = FormatHtmlSource(updated); visual.LastRenderedSource = document.Editor.Text; }
        finally { visual.SyncingFromVisual = false; }
    }

    private async Task SynchronizeHtmlVisualBeforeCloseAsync(EditorDocument document)
    {
        var visual = document.HtmlVisual;
        if (visual?.Ready != true || visual.FrameworkShell || visual.View.CoreWebView2 == null || visual.LastRenderedSource != document.Editor.Text) return;
        var result = await visual.View.CoreWebView2.ExecuteScriptAsync("window.__forgeDirty ? document.body.innerHTML : null");
        var body = JsonSerializer.Deserialize<string?>(result);
        if (body != null) ApplyHtmlVisualChange(document, body);
    }

    private async Task HtmlCommandAsync(EditorDocument document, string command, string? value = null)
    {
        if (document.HtmlVisual?.FrameworkShell == true || document.HtmlVisual?.View.CoreWebView2 == null) return;
        var script = "document.execCommand(" + JsonSerializer.Serialize(command) + ",false," + JsonSerializer.Serialize(value) + ");chrome.webview.postMessage({kind:'forge-html-edit',body:document.body.innerHTML});";
        await document.HtmlVisual.View.CoreWebView2.ExecuteScriptAsync(script);
    }
    private Task InsertHtmlAsync(EditorDocument document, string html) => HtmlCommandAsync(document, "insertHTML", html);
}
