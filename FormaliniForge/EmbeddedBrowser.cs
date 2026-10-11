using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace FormaliniForge;

public partial class MainWindow
{
    private sealed class BrowserPage
    {
        public required TabItem Tab { get; init; }
        public required TextBlock Title { get; init; }
        public WebView2? View { get; set; }
        public string Address { get; set; } = "about:blank";
        public bool IsSource { get; init; }
        public StringBuilder ConsoleLog { get; } = new();
        public StringBuilder NetworkLog { get; } = new();
    }

    private readonly List<BrowserPage> browserPages = new();
    private CoreWebView2Environment? browserEnvironment;
    private string? browserPreviewAddress;
    private string? browserPreviewProject;
    private BrowserPage? CurrentBrowserPage => browserPages.FirstOrDefault(p => ReferenceEquals(p.Tab, BrowserTabs.SelectedItem));
    private WebView2? CurrentBrowserView => CurrentBrowserPage?.View;

    private async Task<CoreWebView2Environment> GetBrowserEnvironmentAsync()
    {
        if (browserEnvironment != null) return browserEnvironment;
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FormaliniForge", "BrowserProfile");
        Directory.CreateDirectory(folder);
        browserEnvironment = await CoreWebView2Environment.CreateAsync(userDataFolder: folder);
        return browserEnvironment;
    }

    private static string NormalizeBrowserAddress(string value)
    {
        value = value.Trim();
        if (value.Length == 0) return "about:blank";
        if (value.StartsWith("view-source:", StringComparison.OrdinalIgnoreCase)) return value;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeFile || uri.Scheme == "about")) return uri.AbsoluteUri;
        if (value.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) || value.StartsWith("127.0.0.1")) return "http://" + value;
        return "https://" + value;
    }

    private BrowserPage CreateBrowserPage(string title, string address, bool source = false)
    {
        var label = new TextBlock { Text = title, MaxWidth = 190, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        var tab = new TabItem();
        var close = new System.Windows.Controls.Button { Content = "×", Width = 23, Height = 22, Padding = new Thickness(0), Margin = new Thickness(7, 0, 0, 0), ToolTip = T("Închide tabul") };
        var header = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        header.Children.Add(label);
        header.Children.Add(close);
        tab.Header = header;
        var page = new BrowserPage { Tab = tab, Title = label, Address = address, IsSource = source };
        close.Click += (_, _) => CloseBrowserPage(page);
        browserPages.Add(page);
        BrowserTabs.Items.Add(tab);
        BrowserTabs.SelectedItem = tab;
        MainTabs.SelectedItem = BrowserWorkspaceTab;
        return page;
    }

    private async Task<BrowserPage> OpenBrowserTabAsync(string address, bool preview = false)
    {
        address = NormalizeBrowserAddress(address);
        if (preview && browserPages.FirstOrDefault(p => !p.IsSource && p.Address == address) is { } existing)
        {
            BrowserTabs.SelectedItem = existing.Tab;
            MainTabs.SelectedItem = BrowserWorkspaceTab;
            existing.View?.Reload();
            RefreshBrowserControls();
            return existing;
        }
        var page = CreateBrowserPage(preview ? T("Previzualizare") : T("Tab nou"), address);
        var view = new WebView2 { DefaultBackgroundColor = System.Drawing.Color.White };
        page.View = view;
        page.Tab.Content = view;
        try
        {
            await Dispatcher.Yield(DispatcherPriority.Loaded);
            await view.EnsureCoreWebView2Async(await GetBrowserEnvironmentAsync());
            view.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            view.CoreWebView2.Settings.AreDevToolsEnabled = true;
            view.CoreWebView2.Settings.IsStatusBarEnabled = true;
            try { await AttachDockedDevToolsAsync(page); }
            catch (Exception ex) { AddBrowserConsoleLine(page, "[DevTools] " + ex.Message); }
            view.CoreWebView2.NewWindowRequested += (_, args) =>
            {
                args.Handled = true;
                _ = OpenBrowserTabSafelyAsync(args.Uri);
            };
            view.CoreWebView2.DocumentTitleChanged += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(view.CoreWebView2.DocumentTitle)) return;
                page.Title.Text = view.CoreWebView2.DocumentTitle;
            };
            view.SourceChanged += (_, _) =>
            {
                page.Address = view.Source?.AbsoluteUri ?? page.Address;
                RefreshBrowserControls();
            };
            view.CoreWebView2.HistoryChanged += (_, _) => RefreshBrowserControls();
            view.NavigationStarting += (_, _) => { if (ReferenceEquals(CurrentBrowserPage, page)) BrowserStatusText.Text = T("Se încarcă..."); };
            view.NavigationCompleted += (_, args) =>
            {
                if (ReferenceEquals(CurrentBrowserPage, page))
                    BrowserStatusText.Text = args.IsSuccess ? T("Pregătit") : $"{T("Eroare de navigare")}: {args.WebErrorStatus}";
                RefreshBrowserControls();
                if (BrowserDevPanel.Visibility == Visibility.Visible) _ = RefreshBrowserElementsAsync();
            };
            view.Source = new Uri(address);
            RefreshBrowserControls();
            return page;
        }
        catch
        {
            CloseBrowserPage(page);
            throw;
        }
    }

    private async Task OpenBrowserTabSafelyAsync(string address)
    {
        try { await OpenBrowserTabAsync(address); }
        catch (Exception ex) { Error(ex); }
    }

    private async Task OpenPreviewInBrowserAsync(string address)
    {
        browserPreviewAddress = address;
        browserPreviewProject = projectDir;
        await OpenBrowserTabAsync(address, preview: true);
        BrowserStatusText.Text = T("Previzualizare pregătită");
    }

    private void CloseBrowserPage(BrowserPage page)
    {
        browserPages.Remove(page);
        BrowserTabs.Items.Remove(page.Tab);
        page.View?.Dispose();
        RefreshBrowserControls();
    }

    private void RefreshBrowserControls()
    {
        if (BrowserAddressBox == null) return;
        var page = CurrentBrowserPage;
        BrowserAddressBox.Text = page?.Address ?? "";
        BrowserBackButton.IsEnabled = page?.View?.CanGoBack == true;
        BrowserForwardButton.IsEnabled = page?.View?.CanGoForward == true;
    }

    private void BrowserTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReferenceEquals(e.Source, BrowserTabs))
        {
            RefreshBrowserControls();
            RefreshDockedDevTools();
        }
    }
    private void BrowserBack_Click(object sender, RoutedEventArgs e) { if (CurrentBrowserView?.CanGoBack == true) CurrentBrowserView.GoBack(); }
    private void BrowserForward_Click(object sender, RoutedEventArgs e) { if (CurrentBrowserView?.CanGoForward == true) CurrentBrowserView.GoForward(); }
    private void BrowserReload_Click(object sender, RoutedEventArgs e) => CurrentBrowserView?.Reload();
    private void BrowserStop_Click(object sender, RoutedEventArgs e) => CurrentBrowserView?.CoreWebView2?.Stop();
    private async void BrowserHome_Click(object sender, RoutedEventArgs e) => await NavigateBrowserSafelyAsync(browserPreviewAddress ?? "about:blank");
    private async void BrowserNewTab_Click(object sender, RoutedEventArgs e) => await OpenBrowserTabSafelyAsync("about:blank");
    private async void BrowserAddress_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        await NavigateBrowserSafelyAsync(BrowserAddressBox.Text);
    }
    private async Task NavigateBrowserSafelyAsync(string address)
    {
        try
        {
            address = NormalizeBrowserAddress(address);
            if (address.StartsWith("view-source:", StringComparison.OrdinalIgnoreCase)) { await OpenSourceTabAsync(address[12..]); return; }
            if (CurrentBrowserView is { } view) view.Source = new Uri(address);
            else await OpenBrowserTabAsync(address);
        }
        catch (Exception ex) { Error(ex); }
    }

    private async void BrowserViewSource_Click(object sender, RoutedEventArgs e)
    {
        try { if (CurrentBrowserPage is { IsSource: false } page) await OpenSourceTabAsync(page.Address); }
        catch (Exception ex) { Error(ex); }
    }
    private async Task OpenSourceTabAsync(string address)
    {
        string html;
        if (Uri.TryCreate(address, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            html = await client.GetStringAsync(uri);
        }
        else if (CurrentBrowserView?.CoreWebView2 != null)
        {
            var result = await CurrentBrowserView.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
            html = JsonSerializer.Deserialize<string>(result) ?? "";
        }
        else html = "";
        var page = CreateBrowserPage(T("Sursă") + ": " + address, address, source: true);
        var editor = new TextEditor { Text = html, IsReadOnly = true, ShowLineNumbers = true, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("HTML");
        page.Tab.Content = editor;
        BrowserStatusText.Text = T("Sursa paginii");
    }
    private void BrowserDevTools_Click(object sender, RoutedEventArgs e)
    {
        var show = BrowserDevPanel.Visibility != Visibility.Visible;
        BrowserDevPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        BrowserDevSplitter.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        BrowserDevRow.Height = show ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        BrowserDevSplitterRow.Height = show ? new GridLength(5) : new GridLength(0);
        if (show) RefreshDockedDevTools();
    }
    private void BrowserFullDevTools_Click(object sender, RoutedEventArgs e) => CurrentBrowserView?.CoreWebView2?.OpenDevToolsWindow();
    private void BrowserExternal_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentBrowserPage is { Address: var address } && address != "about:blank")
            Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
    }
    private void BrowserFind_Click(object sender, RoutedEventArgs e)
    {
        var query = Microsoft.VisualBasic.Interaction.InputBox(T("Text de căutat în pagină"), T("Găsește în pagină"));
        if (!string.IsNullOrWhiteSpace(query) && CurrentBrowserView?.CoreWebView2 is { } core)
            _ = core.ExecuteScriptAsync("window.find(" + JsonSerializer.Serialize(query) + ")");
    }
    private void BrowserZoomIn_Click(object sender, RoutedEventArgs e) { if (CurrentBrowserView is { } view) view.ZoomFactor = Math.Min(3, view.ZoomFactor + 0.1); }
    private void BrowserZoomOut_Click(object sender, RoutedEventArgs e) { if (CurrentBrowserView is { } view) view.ZoomFactor = Math.Max(0.25, view.ZoomFactor - 0.1); }
    private void BrowserZoomReset_Click(object sender, RoutedEventArgs e) { if (CurrentBrowserView is { } view) view.ZoomFactor = 1; }
    private void BrowserPrint_Click(object sender, RoutedEventArgs e) => CurrentBrowserView?.CoreWebView2?.ShowPrintUI();
    private void BrowserCopyAddress_Click(object sender, RoutedEventArgs e) { if (CurrentBrowserPage is { } page) System.Windows.Clipboard.SetText(page.Address); }
}
