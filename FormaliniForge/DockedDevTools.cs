// Copyright (c) 2026 Marco Irinel Pascalini (https://formalini.com)
// SPDX-License-Identifier: MIT
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace FormaliniForge;

public partial class MainWindow
{
    private async Task AttachDockedDevToolsAsync(BrowserPage page)
    {
        var core = page.View?.CoreWebView2;
        if (core == null) return;
        core.GetDevToolsProtocolEventReceiver("Runtime.consoleAPICalled").DevToolsProtocolEventReceived += (_, e) =>
        {
            try
            {
                using var json = JsonDocument.Parse(e.ParameterObjectAsJson);
                var root = json.RootElement;
                var level = root.TryGetProperty("type", out var type) ? type.GetString() : "log";
                var values = root.GetProperty("args").EnumerateArray().Select(arg =>
                    arg.TryGetProperty("value", out var value) ? value.ToString() :
                    arg.TryGetProperty("description", out var description) ? description.GetString() : arg.GetRawText());
                AddBrowserConsoleLine(page, $"[{level}] {string.Join(" ", values)}");
            }
            catch { }
        };
        core.GetDevToolsProtocolEventReceiver("Runtime.exceptionThrown").DevToolsProtocolEventReceived += (_, e) =>
        {
            try
            {
                using var json = JsonDocument.Parse(e.ParameterObjectAsJson);
                var details = json.RootElement.GetProperty("exceptionDetails");
                var message = details.TryGetProperty("text", out var text) ? text.GetString() : "JavaScript error";
                if (details.TryGetProperty("exception", out var exception) && exception.TryGetProperty("description", out var description))
                    message = description.GetString();
                AddBrowserConsoleLine(page, "[error] " + message);
            }
            catch { }
        };
        core.GetDevToolsProtocolEventReceiver("Network.responseReceived").DevToolsProtocolEventReceived += (_, e) =>
        {
            try
            {
                using var json = JsonDocument.Parse(e.ParameterObjectAsJson);
                var response = json.RootElement.GetProperty("response");
                AddBrowserNetworkLine(page, $"{response.GetProperty("status")} {response.GetProperty("url").GetString()}");
            }
            catch { }
        };
        core.GetDevToolsProtocolEventReceiver("Network.loadingFailed").DevToolsProtocolEventReceived += (_, e) =>
        {
            try
            {
                using var json = JsonDocument.Parse(e.ParameterObjectAsJson);
                AddBrowserNetworkLine(page, "FAILED " + json.RootElement.GetProperty("errorText").GetString());
            }
            catch { }
        };
        await core.CallDevToolsProtocolMethodAsync("Runtime.enable", "{}");
        await core.CallDevToolsProtocolMethodAsync("Network.enable", "{}");
    }

    private void AddBrowserConsoleLine(BrowserPage page, string line)
    {
        Dispatcher.BeginInvoke(() =>
        {
            page.ConsoleLog.AppendLine(line);
            if (page.ConsoleLog.Length > 100_000) page.ConsoleLog.Remove(0, page.ConsoleLog.Length - 80_000);
            if (ReferenceEquals(CurrentBrowserPage, page))
            {
                BrowserConsoleText.Text = page.ConsoleLog.ToString();
                BrowserConsoleText.ScrollToEnd();
            }
        });
    }
    private void AddBrowserNetworkLine(BrowserPage page, string line)
    {
        Dispatcher.BeginInvoke(() =>
        {
            page.NetworkLog.AppendLine(line);
            if (page.NetworkLog.Length > 100_000) page.NetworkLog.Remove(0, page.NetworkLog.Length - 80_000);
            if (ReferenceEquals(CurrentBrowserPage, page))
            {
                BrowserNetworkText.Text = page.NetworkLog.ToString();
                BrowserNetworkText.ScrollToEnd();
            }
        });
    }
    private void RefreshDockedDevTools()
    {
        if (BrowserConsoleText == null) return;
        BrowserConsoleText.Text = CurrentBrowserPage?.ConsoleLog.ToString() ?? "";
        BrowserNetworkText.Text = CurrentBrowserPage?.NetworkLog.ToString() ?? "";
        _ = RefreshBrowserElementsAsync();
    }
    private async Task RefreshBrowserElementsAsync()
    {
        if (BrowserDevPanel == null || BrowserDevTabs == null || BrowserDevPanel.Visibility != Visibility.Visible || BrowserDevTabs.SelectedIndex != 1 || CurrentBrowserView?.CoreWebView2 == null) return;
        var selected = CurrentBrowserPage;
        try
        {
            var result = await CurrentBrowserView.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
            if (ReferenceEquals(CurrentBrowserPage, selected)) BrowserElementsText.Text = JsonSerializer.Deserialize<string>(result) ?? "";
        }
        catch (Exception ex) { BrowserElementsText.Text = ex.Message; }
    }
    private void BrowserDevTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReferenceEquals(e.Source, BrowserDevTabs)) _ = RefreshBrowserElementsAsync();
    }
    private void BrowserConsoleInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        _ = ExecuteBrowserConsoleAsync();
    }
    private void BrowserConsoleExecute_Click(object sender, RoutedEventArgs e) => _ = ExecuteBrowserConsoleAsync();
    private async Task ExecuteBrowserConsoleAsync()
    {
        var page = CurrentBrowserPage;
        var command = BrowserConsoleInput.Text.Trim();
        if (page?.View?.CoreWebView2 == null || command.Length == 0) return;
        BrowserConsoleInput.Clear();
        AddBrowserConsoleLine(page, "> " + command);
        try
        {
            var result = await page.View.CoreWebView2.ExecuteScriptAsync(command);
            AddBrowserConsoleLine(page, result);
        }
        catch (Exception ex) { AddBrowserConsoleLine(page, "[error] " + ex.Message); }
    }
}
