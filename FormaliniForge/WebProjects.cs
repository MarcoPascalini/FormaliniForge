using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows.Controls;

namespace FormaliniForge;

public partial class MainWindow
{
    private Process? webServer;
    private bool IsWebProject => state.Language is "Vue" or "React" or "React Native";
    private static bool IsNpmCertificateError(string error) =>
        error.Contains("UNABLE_TO_GET_ISSUER_CERT_LOCALLY", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("UNABLE_TO_VERIFY_LEAF_SIGNATURE", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("SELF_SIGNED_CERT_IN_CHAIN", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("DEPTH_ZERO_SELF_SIGNED_CERT", StringComparison.OrdinalIgnoreCase);
    private static readonly string[] webControls = ["Heading", "Text", "Button", "Input", "TextArea", "Image", "CheckBox", "Select", "Container", "Link", "List", "Table"];
    private static readonly string[] mobileControls = ["View", "Text", "TextInput", "Pressable", "Image", "Switch", "ScrollView", "FlatList"];

    private void ConfigureToolboxForProject()
    {
        Toolbox.Items.Clear();
        foreach (var type in state.Language == "React Native" ? mobileControls : IsWebProject ? webControls : componentIcons.Keys.Take(24).ToArray())
            Toolbox.Items.Add(new ListBoxItem { Content = type });
        DecorateToolbox();
    }

    private static string Html(string value) => System.Net.WebUtility.HtmlEncode(value);
    private static string Js(string value) => JsonSerializer.Serialize(value);
    private static string Css(DesignItem item) => $"position:absolute;left:{item.X:0}px;top:{item.Y:0}px;width:{item.Width:0}px;height:{item.Height:0}px;box-sizing:border-box;font-size:{item.FontSize:0}px;" +
        (item.Background.Length > 0 ? "background-color:" + item.Background + ";" : "") +
        (item.Foreground.Length > 0 ? "color:" + item.Foreground + ";" : "");
    private static string HtmlControl(DesignItem item)
    {
        var style = Html(Css(item));
        var value = Html(item.Text);
        return item.Type switch
        {
            "Heading" => $"<h2 style=\"{style}\">{value}</h2>",
            "Text" => $"<p style=\"{style}\">{value}</p>",
            "Input" => $"<input style=\"{style}\" placeholder=\"{value}\" />",
            "TextArea" => $"<textarea style=\"{style}\" placeholder=\"{value}\"></textarea>",
            "Image" => $"<img style=\"{style}\" src=\"{value}\" alt=\"{Html(item.Name)}\" />",
            "CheckBox" => $"<label style=\"{style}\"><input type=\"checkbox\" /> {value}</label>",
            "Select" => $"<select style=\"{style}\"><option>{value}</option></select>",
            "Container" => $"<div style=\"{style}border:1px solid #bbc9dd\">{value}</div>",
            "Link" => $"<a style=\"{style}\" href=\"#\">{value}</a>",
            "List" => $"<ul style=\"{style}\"><li>{value}</li></ul>",
            "Table" => $"<table style=\"{style}\"><tbody><tr><td>{value}</td></tr></tbody></table>",
            _ => $"<button style=\"{style}\">{value}</button>"
        };
    }
    private static string ReactControl(DesignItem item)
    {
        var jsxStyle = $"style={{{{position:'absolute',left:{item.X:0},top:{item.Y:0},width:{item.Width:0},height:{item.Height:0},boxSizing:'border-box',fontSize:{item.FontSize:0}" +
            (item.Background.Length > 0 ? ",backgroundColor:" + Js(item.Background) : "") +
            (item.Foreground.Length > 0 ? ",color:" + Js(item.Foreground) : "") +
            (item.Type == "Container" ? ",border:'1px solid #bbc9dd'" : "") + "}}";
        var value = Js(item.Text);
        var name = Js(item.Name);
        return item.Type switch
        {
            "Heading" => $"<h2 {jsxStyle}>{{{value}}}</h2>",
            "Text" => $"<p {jsxStyle}>{{{value}}}</p>",
            "Input" => $"<input {jsxStyle} placeholder={{{value}}} />",
            "TextArea" => $"<textarea {jsxStyle} placeholder={{{value}}} />",
            "Image" => $"<img {jsxStyle} src={{{value}}} alt={{{name}}} />",
            "CheckBox" => $"<label {jsxStyle}><input type=\"checkbox\" /> {{{value}}}</label>",
            "Select" => $"<select {jsxStyle}><option>{{{value}}}</option></select>",
            "Container" => $"<div {jsxStyle}>{{{value}}}</div>",
            "Link" => $"<a {jsxStyle} href=\"#\">{{{value}}}</a>",
            "List" => $"<ul {jsxStyle}><li>{{{value}}}</li></ul>",
            "Table" => $"<table {jsxStyle}><tbody><tr><td>{{{value}}}</td></tr></tbody></table>",
            _ => $"<button {jsxStyle}>{{{value}}}</button>"
        };
    }
    private void GenerateWebProject()
    {
        var app = Path.Combine(projectDir!, "App");
        var src = Path.Combine(app, "src");
        Directory.CreateDirectory(src);
        var formBackground = CurrentForm.Background.Length == 0 ? "White" : CurrentForm.Background;
        var package = Path.Combine(app, "package.json");
        if (!File.Exists(package))
        {
            var name = state.Name.ToLowerInvariant();
            var scripts = state.Language == "React Native" ? new Dictionary<string, string> { ["start"] = "expo start", ["web"] = "expo start --web", ["export"] = "expo export --platform web" } : new Dictionary<string, string> { ["dev"] = "vite", ["build"] = "vite build" };
            var dependencies = state.Language switch
            {
                "Vue" => new Dictionary<string, string> { ["vue"] = "^3.5.0" },
                "React" => new Dictionary<string, string> { ["react"] = "^19.0.0", ["react-dom"] = "^19.0.0" },
                _ => new Dictionary<string, string> { ["expo"] = "~53.0.0", ["react"] = "19.0.0", ["react-native"] = "0.79.6", ["react-dom"] = "19.0.0", ["react-native-web"] = "^0.20.0", ["@expo/metro-runtime"] = "~5.0.0" }
            };
            var devDependencies = state.Language == "Vue" ? new Dictionary<string, string> { ["vite"] = "^6.0.0", ["@vitejs/plugin-vue"] = "^5.0.0" } : state.Language == "React" ? new Dictionary<string, string> { ["vite"] = "^6.0.0" } : new Dictionary<string, string>();
            File.WriteAllText(package, JsonSerializer.Serialize(new { name, version = "1.0.0", privatePackage = true, type = "module", main = "node_modules/expo/AppEntry.js", scripts, dependencies, devDependencies }, new JsonSerializerOptions { WriteIndented = true }).Replace("\"privatePackage\"", "\"private\""));
        }
        if (state.Language == "React Native")
        {
            var appJs = Path.Combine(app, "App.jsx");
            if (!File.Exists(appJs)) File.WriteAllText(appJs, "export { default } from './src/ForgeScreen.jsx';\n");
            var screen = Path.Combine(src, "ForgeScreen.jsx");
            if (!File.Exists(screen)) File.WriteAllText(screen, "import React from 'react';\nimport ForgeLayout from './ForgeLayout.jsx';\n\nexport default function ForgeScreen() {\n  return <ForgeLayout />;\n}\n");
            var appJson = Path.Combine(app, "app.json");
            if (!File.Exists(appJson)) File.WriteAllText(appJson, JsonSerializer.Serialize(new { expo = new { name = state.Name, slug = state.Name.ToLowerInvariant(), version = "1.0.0", platforms = new[] { "android", "ios", "web" } } }, new JsonSerializerOptions { WriteIndented = true }));
            var lines = CurrentItems.Select(MobileControl);
            File.WriteAllText(Path.Combine(src, "ForgeLayout.jsx"), "// Generated by the visual designer. Put custom logic in ForgeScreen.jsx.\nimport React from 'react';\nimport { View, Text, TextInput, Pressable, Image, Switch, ScrollView, FlatList } from 'react-native';\n\nexport default function ForgeLayout() {\n  return (\n    <View style={{ flex: 1, backgroundColor: " + Js(formBackground) + " }}>\n" + string.Join("\n", lines.Select(line => "      " + line)) + "\n    </View>\n  );\n}\n");
            return;
        }
        var index = Path.Combine(app, "index.html");
        if (!File.Exists(index)) File.WriteAllText(index, "<!doctype html>\n<html lang=\"en\">\n  <head>\n    <meta charset=\"UTF-8\" />\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />\n    <link rel=\"icon\" href=\"data:,\" />\n    <title>" + Html(state.Name) + "</title>\n  </head>\n  <body>\n    <div id=\"root\"></div>\n    <script type=\"module\" src=\"/src/main.jsx\"></script>\n  </body>\n</html>\n");
        var indexHtml = File.ReadAllText(index);
        if (!indexHtml.Contains("rel=\"icon\"", StringComparison.OrdinalIgnoreCase) && !indexHtml.Contains("rel='icon'", StringComparison.OrdinalIgnoreCase))
            File.WriteAllText(index, indexHtml.Replace("</head>", "<link rel=\"icon\" href=\"data:,\"/></head>", StringComparison.OrdinalIgnoreCase));
        indexHtml = File.ReadAllText(index);
        if (!indexHtml.Contains('\n') && indexHtml.Contains("/src/main.", StringComparison.OrdinalIgnoreCase))
            File.WriteAllText(index, FormatHtmlSource(indexHtml));
        var markup = string.Join("\n", CurrentItems.Select(item => state.Language == "Vue" ? HtmlControl(item) : ReactControl(item)));
        if (state.Language == "Vue")
        {
            File.WriteAllText(index, File.ReadAllText(index).Replace("/src/main.jsx", "/src/main.js"));
            var main = Path.Combine(src, "main.js");
            if (!File.Exists(main)) File.WriteAllText(main, "import { createApp } from 'vue';\nimport ForgeScreen from './ForgeScreen.vue';\ncreateApp(ForgeScreen).mount('#root');\n");
            var screen = Path.Combine(src, "ForgeScreen.vue");
            if (!File.Exists(screen)) File.WriteAllText(screen, "<script setup>\nimport ForgeLayout from './ForgeLayout.vue';\n</script>\n\n<template>\n  <ForgeLayout />\n</template>\n");
            File.WriteAllText(Path.Combine(src, "ForgeLayout.vue"), "<!-- Generated by the visual designer. Put custom logic in ForgeScreen.vue. -->\n<template>\n  <main class=\"forge-screen\">\n" + string.Join("\n", markup.Split('\n').Select(line => "    " + line)) + "\n  </main>\n</template>\n\n<style>\n.forge-screen {\n  position: relative;\n  min-height: 100vh;\n  font-family: system-ui, sans-serif;\n  background: " + Html(formBackground) + ";\n}\n</style>\n");
            var config = Path.Combine(app, "vite.config.js");
            if (!File.Exists(config)) File.WriteAllText(config, "import { defineConfig } from 'vite';\nimport vue from '@vitejs/plugin-vue';\n\nexport default defineConfig({\n  plugins: [vue()],\n  base: './',\n});\n");
        }
        else
        {
            var main = Path.Combine(src, "main.jsx");
            if (!File.Exists(main)) File.WriteAllText(main, "import React from 'react';\nimport { createRoot } from 'react-dom/client';\nimport ForgeScreen from './ForgeScreen.jsx';\n\ncreateRoot(document.getElementById('root')).render(<ForgeScreen />);\n");
            var screen = Path.Combine(src, "ForgeScreen.jsx");
            if (!File.Exists(screen)) File.WriteAllText(screen, "import React from 'react';\nimport ForgeLayout from './ForgeLayout.jsx';\n\nexport default function ForgeScreen() {\n  return <ForgeLayout />;\n}\n");
            File.WriteAllText(Path.Combine(src, "ForgeLayout.jsx"), "// Generated by the visual designer. Put custom logic in ForgeScreen.jsx.\nimport React from 'react';\n\nexport default function ForgeLayout() {\n  return (\n    <main style={{ position: 'relative', minHeight: '100vh', fontFamily: 'system-ui, sans-serif', backgroundColor: " + Js(formBackground) + " }}>\n" + string.Join("\n", markup.Split('\n').Select(line => "      " + line)) + "\n    </main>\n  );\n}\n");
            var config = Path.Combine(app, "vite.config.js");
            if (!File.Exists(config)) File.WriteAllText(config, "import { defineConfig } from 'vite';\n\nexport default defineConfig({\n  base: './',\n});\n");
        }
    }
    private static string MobileControl(DesignItem item)
    {
        var style = $"{{{{position:'absolute',left:{item.X:0},top:{item.Y:0},width:{item.Width:0},height:{item.Height:0},fontSize:{item.FontSize:0}" +
            (item.Background.Length > 0 ? ",backgroundColor:" + Js(item.Background) : "") +
            (item.Foreground.Length > 0 ? ",color:" + Js(item.Foreground) : "") + "}}";
        var text = Js(item.Text);
        return item.Type switch
        {
            "Text" => $"<Text style={style}>{{{text}}}</Text>",
            "TextInput" => $"<TextInput style={style} placeholder={{{text}}} />",
            "Pressable" => $"<Pressable style={style}><Text>{{{text}}}</Text></Pressable>",
            "Image" => "<Image style=" + style + " source={{uri:" + text + "}} />",
            "Switch" => $"<Switch style={style} />",
            "ScrollView" => $"<ScrollView style={style}><Text>{{{text}}}</Text></ScrollView>",
            "FlatList" => "<FlatList style=" + style + " data={[" + text + "]} renderItem={({item})=><Text>{item}</Text>} />",
            _ => $"<View style={style}><Text>{{{text}}}</Text></View>"
        };
    }
    private async Task<(int ExitCode, string Error)> RunNpmCommand(string app, string command, bool systemCaProfile = false)
    {
        var npm = FindTool("npm.cmd") ?? throw new InvalidOperationException(english ? "Install Node.js and npm to build web projects." : "Instalează Node.js și npm pentru a construi proiecte web.");
        var psi = new ProcessStartInfo(npm) { WorkingDirectory = app, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        psi.Environment["npm_config_cache"] = Path.Combine(projectDir!, ".forge-npm-cache");
        psi.Environment["npm_config_audit"] = "false";
        psi.Environment["npm_config_fund"] = "false";
        psi.Environment["npm_config_fetch_retries"] = "1";
        psi.Environment["npm_config_fetch_timeout"] = "15000";
        if (systemCaProfile)
        {
            var config = Path.Combine(projectDir!, ".forge-npmrc");
            if (!File.Exists(config)) File.WriteAllText(config, "");
            psi.Environment["npm_config_userconfig"] = config;
            psi.Environment.TryGetValue("NODE_OPTIONS", out var nodeOptions);
            psi.Environment["NODE_OPTIONS"] = (nodeOptions + " --use-system-ca").Trim();
        }
        foreach (var argument in command.Split(' ')) psi.ArgumentList.Add(argument);
        AppendBuildLine("npm " + command + (systemCaProfile ? " (Windows CA)" : ""));
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("npm failed to start.");
        var errorTask = CaptureNpmError(process.StandardError);
        await Task.WhenAll(PumpProcessOutput(process.StandardOutput, ""), errorTask, process.WaitForExitAsync());
        return (process.ExitCode, await errorTask);
    }

    private async Task<string> CaptureNpmError(StreamReader reader)
    {
        var error = new StringBuilder();
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            error.AppendLine(line);
            AppendBuildLine("ERROR " + line);
        }
        return error.ToString();
    }

    private async Task<bool> BuildWebProject(bool debug)
    {
        RequireProject(); SaveProject(); GenerateWebProject(); MainTabs.SelectedIndex = 2;
        var app = Path.Combine(projectDir!, "App");
        var output = Path.Combine(projectDir!, "publish");
        Directory.CreateDirectory(output);
        BuildOutput.Text = (english ? "Building project..." : "Construiesc proiectul...") + "\n";
        if (state.Language == "React Native")
        {
            var zip = Path.Combine(output, state.Name + "-react-native-source.zip");
            if (File.Exists(zip)) File.Delete(zip);
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
                foreach (var file in Directory.EnumerateFiles(app, "*", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(app, file);
                    if (relative.Split(Path.DirectorySeparatorChar).Any(part => part is "node_modules" or ".expo" or "dist" or ".forge-npm-cache")) continue;
                    archive.CreateEntryFromFile(file, relative.Replace(Path.DirectorySeparatorChar, '/'), CompressionLevel.Optimal);
                }
            AppendBuildLine((english ? "Deployable Expo source: " : "Cod sursă Expo pentru distribuire: ") + zip);
            RunStatus.Text = english ? "Source package ready" : "Pachet sursă pregătit";
            Status(zip);
            return true;
        }
        if (!File.Exists(Path.Combine(app, "node_modules", ".bin", "vite.cmd")))
        {
            var installation = await RunNpmCommand(app, "install");
            if (installation.ExitCode != 0 && IsNpmCertificateError(installation.Error))
            {
                AppendBuildLine(english ? "Retrying with the Windows trusted certificate store." : "Reîncerc folosind certificatele de încredere Windows.");
                installation = await RunNpmCommand(app, "install", systemCaProfile: true);
            }
            if (installation.ExitCode != 0) { RunStatus.Text = english ? "Dependency install failed" : "Instalarea dependențelor a eșuat"; return false; }
        }
        if ((await RunNpmCommand(app, "run build")).ExitCode != 0) { RunStatus.Text = english ? "Build failed" : "Compilare eșuată"; return false; }
        var dist = Path.Combine(app, "dist");
        var target = Path.Combine(output, "web");
        if (Directory.Exists(target)) Directory.Delete(target, true);
        Directory.Move(dist, target);
        AppendBuildLine((english ? "Deployable web files: " : "Fișiere web pentru publicare: ") + target);
        RunStatus.Text = english ? "Build succeeded" : "Compilare reușită";
        Status(target);
        return true;
    }
    private async Task RunWebProject()
    {
        var app = Path.Combine(projectDir!, "App");
        var npm = FindTool("npm.cmd") ?? throw new InvalidOperationException("Node.js/npm missing.");
        if (activeRun is { HasExited: false })
        {
            if (browserPreviewAddress != null && browserPreviewProject == projectDir)
            {
                await OpenPreviewInBrowserAsync(browserPreviewAddress);
                return;
            }
            throw new InvalidOperationException(english ? "Stop the current run before starting another." : "Oprește rularea curentă înainte de a porni alta.");
        }
        if (state.Language == "React Native" && !File.Exists(Path.Combine(app, "node_modules", ".bin", "expo.cmd")))
        {
            var installation = await RunNpmCommand(app, "install");
            if (installation.ExitCode != 0 && IsNpmCertificateError(installation.Error))
            {
                AppendBuildLine(english ? "Retrying with the Windows trusted certificate store." : "Reîncerc folosind certificatele de încredere Windows.");
                installation = await RunNpmCommand(app, "install", systemCaProfile: true);
            }
            if (installation.ExitCode != 0) throw new InvalidOperationException("npm install failed. See Build output.");
        }
        var port = FindFreeLocalPort();
        var address = "http://127.0.0.1:" + port + "/";
        var psi = new ProcessStartInfo(npm) { WorkingDirectory = app, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        psi.Environment["npm_config_cache"] = Path.Combine(projectDir!, ".forge-npm-cache");
        var trustedConfig = Path.Combine(projectDir!, ".forge-npmrc");
        if (File.Exists(trustedConfig))
        {
            psi.Environment["npm_config_userconfig"] = trustedConfig;
            psi.Environment.TryGetValue("NODE_OPTIONS", out var nodeOptions);
            psi.Environment["NODE_OPTIONS"] = (nodeOptions + " --use-system-ca").Trim();
        }
        psi.ArgumentList.Add("run"); psi.ArgumentList.Add(state.Language == "React Native" ? "web" : "dev");
        psi.ArgumentList.Add("--");
        if (state.Language != "React Native") { psi.ArgumentList.Add("--host"); psi.ArgumentList.Add("127.0.0.1"); }
        psi.ArgumentList.Add("--port"); psi.ArgumentList.Add(port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (state.Language != "React Native") psi.ArgumentList.Add("--strictPort");
        var server = Process.Start(psi) ?? throw new InvalidOperationException("Development server failed to start.");
        activeRun = server;
        webServer = server;
        var stdout = PumpProcessOutput(server.StandardOutput, "");
        var stderr = PumpProcessOutput(server.StandardError, "ERROR ");
        _ = MonitorWebServer(server, stdout, stderr);
        AppendBuildLine((english ? "Starting local preview: " : "Pornesc previzualizarea locală: ") + address);
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (DateTime.UtcNow < deadline)
        {
            if (server.HasExited) throw new InvalidOperationException(english ? "Preview server exited. See Build output." : "Serverul de previzualizare s-a oprit. Vezi jurnalul Build.");
            try
            {
                using var response = await client.GetAsync(address);
                if (response.IsSuccessStatusCode)
                {
                    RunStatus.Text = (english ? "Running · " : "Rulează · ") + address;
                    AppendBuildLine((english ? "Preview ready: " : "Previzualizare pregătită: ") + address);
                    await OpenPreviewInBrowserAsync(address);
                    return;
                }
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }
            await Task.Delay(500);
        }
        throw new InvalidOperationException(english ? "Preview server did not become ready within 90 seconds. See Build output." : "Serverul de previzualizare nu a pornit în 90 de secunde. Vezi jurnalul Build.");
    }

    private async Task MonitorWebServer(Process server, Task stdout, Task stderr)
    {
        try
        {
            await Task.WhenAll(stdout, stderr, server.WaitForExitAsync());
            RunStatus.Text = (english ? "Preview stopped · code " : "Previzualizare oprită · cod ") + server.ExitCode;
            AppendBuildLine("Preview server exited with code " + server.ExitCode + ".");
        }
        catch (Exception ex) { AppendBuildLine("Preview server: " + ex.Message); }
        finally
        {
            if (ReferenceEquals(activeRun, server)) activeRun = null;
            if (ReferenceEquals(webServer, server)) webServer = null;
            if (ReferenceEquals(webServer, null) && browserPreviewProject == projectDir) browserPreviewAddress = null;
        }
    }

    private static int FindFreeLocalPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try { return ((IPEndPoint)listener.LocalEndpoint).Port; }
        finally { listener.Stop(); }
    }
}
