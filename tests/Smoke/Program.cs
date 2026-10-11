using FormaliniForge;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.IO;
using System.IO.Compression;

internal static class Program
{
 [STAThread]
 private static void Main()
 {
  var tempRoot = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "tests", "Smoke", "out"));
  var temp = Path.Combine(tempRoot, "FormaliniForgeSmoke-" + Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(temp);
  try
  {
    var catalogRoot = Path.Combine(temp, "extensions");
    var catalog = new ExtensionCatalog(catalogRoot);
    var package = Path.Combine(temp, "hello.zip");
    using (var zip = ZipFile.Open(package, ZipArchiveMode.Create))
    {
        using var writer = new StreamWriter(zip.CreateEntry("manifest.json").Open());
        writer.Write("{\"apiVersion\":1,\"id\":\"example.hello\",\"name\":\"Hello\",\"commands\":[{\"id\":\"hello\",\"title\":\"Insert greeting\",\"insertText\":\"Hello\"}],\"snippets\":[{\"trigger\":\"greet\",\"body\":\"Hello, world!\",\"extensions\":[\".cs\"]}]}");
    }
    var installed = catalog.Install(package);
    if (installed.Enabled || installed.Manifest.Commands.Count != 1 || installed.Manifest.Snippets.Count != 1) throw new Exception("Extension install failed.");
    catalog.SetEnabled(installed, true); catalog.Reload();
    if (!catalog.Items.Single().Enabled) throw new Exception("Extension enablement was not persisted.");
    var badPackage = Path.Combine(temp, "bad.zip");
    using (var zip = ZipFile.Open(badPackage, ZipArchiveMode.Create))
    {
        using (var writer = new StreamWriter(zip.CreateEntry("manifest.json").Open())) writer.Write("{\"apiVersion\":1,\"id\":\"example.bad\",\"name\":\"Bad\"}");
        using (var writer = new StreamWriter(zip.CreateEntry("../escape.txt").Open())) writer.Write("bad");
    }
    try { catalog.Install(badPackage); throw new Exception("Unsafe extension archive was accepted."); }
    catch (InvalidDataException) { }
    _ = new Application();
    var window = new MainWindow();
    var flags = BindingFlags.Instance | BindingFlags.NonPublic;
    var formatHtml = typeof(MainWindow).GetMethod("FormatHtmlSource", BindingFlags.Static | BindingFlags.NonPublic)!;
    var readableHtml = (string)formatHtml.Invoke(null, new object[] { "<!doctype html><html><head><title>Test</title></head><body><h1>Hello</h1><p>World</p></body></html>" })!;
    if (!readableHtml.Contains("\n") || !readableHtml.Contains("<title>Test</title>") || !readableHtml.Contains("<p>") || !readableHtml.Contains("World")) throw new Exception("HTML formatter did not produce readable source: " + readableHtml);
    var certificateCheck = typeof(MainWindow).GetMethod("IsNpmCertificateError", BindingFlags.Static | BindingFlags.NonPublic)!;
    foreach (var code in new[] { "UNABLE_TO_GET_ISSUER_CERT_LOCALLY", "UNABLE_TO_VERIFY_LEAF_SIGNATURE", "SELF_SIGNED_CERT_IN_CHAIN" })
        if (!(bool)certificateCheck.Invoke(null, new object[] { "npm ERR! code " + code })!) throw new Exception("npm certificate retry does not recognize " + code);
    typeof(MainWindow).GetField("projectDir", flags)!.SetValue(window, temp);
    var state = new ProjectState
    {
        Name = "SmokeApp",
        Items = new()
        {
            new DesignItem { Name = "hello", Type = "TextBlock", Text = "Hello", Options = new() { ["TextWrapping"] = "Wrap", ["FontWeight"] = "Bold" } },
            new DesignItem { Name = "date", Type = "DatePicker", X = 20, Y = 70 },
            new DesignItem { Name = "tabs", Type = "TabControl", X = 20, Y = 120, Width = 250, Height = 160 },
            new DesignItem { Name = "shape", Type = "Rectangle", X = 290, Y = 120, Background = "#60A5FA" },
            new DesignItem { Name = "group", Type = "GroupBox", Text = "Details", X = 20, Y = 300, Width = 250, Height = 120 }
        },
        Windows = new() { ["SecondWindow"] = new() { new DesignItem { Name = "closeButton", Type = "Button", Text = "Close" } } },
        Forms = new() { ["MainWindow"] = new FormSettings { Title = "Smoke design", Width = 900, Height = 680, Background = "#F8FAFC", IconFile = "Assets/test.ico" } }
    };
    typeof(MainWindow).GetField("state", flags)!.SetValue(window, state);
    typeof(MainWindow).GetMethod("RenderDesigner", flags)!.Invoke(window, null);
    var colorSample = new DesignItem { Name = "colorSample", Type = "Image", Text = "Image", Background = "#123456", Foreground = "#FEDCBA", X = 600, Y = 40 };
    state.Items.Add(colorSample);
    typeof(MainWindow).GetMethod("RenderDesigner", flags)!.Invoke(window, null);
    var canvas = (System.Windows.Controls.Canvas)window.FindName("DesignCanvas")!;
    var colorVisual = canvas.Children.OfType<System.Windows.Controls.Border>().First(x => ReferenceEquals(x.Tag, colorSample));
    if (((System.Windows.Media.SolidColorBrush)colorVisual.Background).Color.ToString() != "#FF123456" ||
        ((System.Windows.Media.SolidColorBrush)((System.Windows.Controls.TextBlock)colorVisual.Child).Foreground).Color.ToString() != "#FFFEDCBA")
        throw new Exception("Designer does not display component colors.");
    state.Items.Remove(colorSample);
    typeof(MainWindow).GetMethod("RenderDesigner", flags)!.Invoke(window, null);
    typeof(MainWindow).GetMethod("DecorateToolbox", flags)!.Invoke(window, null);
    var toolbox = (System.Windows.Controls.ListBox)window.FindName("Toolbox")!;
    if (((System.Windows.Controls.ListBoxItem)toolbox.Items[0]).Tag as string != "Button") throw new Exception("Toolbox icons were not attached.");
    var firstTool = (System.Windows.Controls.ListBoxItem)toolbox.Items[0];
    var toolBackground = (System.Windows.Media.SolidColorBrush)firstTool.Background;
    if (toolBackground.Color.R > 100 || firstTool.Effect == null) throw new Exception("Toolbox cards lost their dark background or shadow.");
    typeof(MainWindow).GetField("english", flags)!.SetValue(window, false);
    typeof(MainWindow).GetMethod("RefreshCreditBalance", flags)!.Invoke(window, null);
    if (!((System.Windows.Controls.TextBlock)window.FindName("CreditBalanceText")!).Text.Contains("cereri")) throw new Exception("Romanian AI credit label is missing.");
    typeof(MainWindow).GetField("english", flags)!.SetValue(window, true);
    typeof(MainWindow).GetMethod("RefreshCreditBalance", flags)!.Invoke(window, null);
    if (!((System.Windows.Controls.TextBlock)window.FindName("CreditBalanceText")!).Text.Contains("requests")) throw new Exception("English AI credit label is missing.");
    typeof(MainWindow).GetMethod("FormResize_DragDelta", flags)!.Invoke(window, new object[] { window, new System.Windows.Controls.Primitives.DragDeltaEventArgs(50, 30) });
    if (state.Forms["MainWindow"].Width != 950 || state.Forms["MainWindow"].Height != 710) throw new Exception("Form resize did not update dimensions.");
    if (!(bool)typeof(MainWindow).GetMethod("IsNaturalInstruction", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { "afișează un mesaj de salut" })!) throw new Exception("Natural-language instruction was not detected.");
    var offlineInstruction = (string?)typeof(MainWindow).GetMethod("TryOfflineInstruction", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { "afișează \"Salut\"" });
    if (offlineInstruction != "System.Windows.MessageBox.Show(\"Salut\");") throw new Exception("Offline instruction conversion failed.");
    var refreshInspector = typeof(MainWindow).GetMethod("RefreshInspector", flags)!;
    refreshInspector.Invoke(window, null);
    var inspectorFields = (System.Collections.IDictionary)typeof(MainWindow).GetField("inspectorFields", flags)!.GetValue(window)!;
    if (!inspectorFields.Contains("Title") || !inspectorFields.Contains("Width")) throw new Exception("Form properties are missing.");
    var colorField = (System.Windows.Controls.TextBox)inspectorFields["Background"]!;
    if (colorField.Parent is not System.Windows.Controls.DockPanel colorRow || !colorRow.Children.OfType<System.Windows.Controls.Button>().Any())
        throw new Exception("Color picker is missing from form properties.");
    typeof(MainWindow).GetField("selected", flags)!.SetValue(window, state.Items[0]);
    refreshInspector.Invoke(window, null);
    if (!inspectorFields.Contains("Name") || !inspectorFields.Contains("Option.TextWrapping")) throw new Exception("Control properties are missing.");
    typeof(MainWindow).GetMethod("UpdateSelectionAdorner", flags)!.Invoke(window, null);
    var handles = (System.Collections.IDictionary)typeof(MainWindow).GetField("resizeHandles", flags)!.GetValue(window)!;
    if (handles.Count != 8) throw new Exception("Resize handles are missing.");
    typeof(MainWindow).GetMethod("ResizeSelected", flags)!.Invoke(window, new object[] { "SE", 20d, 10d });
    if (state.Items[0].Width != 150 || state.Items[0].Height != 42) throw new Exception("Resize did not update component dimensions.");
    typeof(MainWindow).GetField("selected", flags)!.SetValue(window, null);
    var actionButton = new DesignItem { Name = "actionButton", Type = "Button", Text = "Act", X = 400, Y = 45 };
    state.Items.Add(actionButton);
    Directory.CreateDirectory(Path.Combine(temp, "App", "Assets"));
    File.Copy(Path.GetFullPath(Path.Combine("FormaliniForge", "Assets", "FormaliniForge.ico")), Path.Combine(temp, "App", "Assets", "test.ico"));
    typeof(MainWindow).GetMethod("GenerateApp", flags)!.Invoke(window, null);
    var eventPath = (string)typeof(MainWindow).GetMethod("EnsureComponentEvents", flags)!.Invoke(window, new object[] { actionButton })!;
    typeof(MainWindow).GetMethod("ApplyGuidedAction", flags)!.Invoke(window, new object[] { actionButton, 0, "Hello from action" });
    if (!File.ReadAllText(Path.Combine(temp, "App", "MainWindow.xaml")).Contains("Click=\"actionButton_Click\"") ||
        !File.ReadAllText(eventPath).Contains("MessageBox.Show(\"Hello from action\")"))
        throw new Exception("Guided button action was not generated.");
    File.WriteAllText(eventPath, File.ReadAllText(eventPath).Replace("System.Windows.MessageBox.Show(\"Hello from action\");",
        "System.Windows.MessageBox.Show(\"Hello from action\");\n        ForgeWatch.Record(\"answer\", () => 42, \"MainWindow.actionButton.Events.cs\", 1); // forge-watch"));
    typeof(MainWindow).GetMethod("LoadWatchDefinitions", flags)!.Invoke(window, null);
    if (((System.Windows.Controls.ListBox)window.FindName("WatchList")!).Items.Count == 0) throw new Exception("Saved watches were not restored.");
    if (!File.ReadAllText(Path.Combine(temp, "App", "MainWindow.xaml.cs")).Contains("\n    public MainWindow()") ||
        !File.ReadAllText(Path.Combine(temp, "App", "MainWindow.xaml")).Contains("Icon=\"Assets/test.ico\""))
        throw new Exception("Formatted code or window icon was not generated.");
    if (Environment.GetEnvironmentVariable("SMOKE_STUB_BOOTSTRAP") == "1")
        File.WriteAllText(Path.Combine(temp, "App", "DependencyBootstrap.cs"), "namespace SmokeApp; internal static class DependencyBootstrap { public static bool Ensure() => true; }");
    var pascal = Path.Combine(temp, "App", "example.pas");
    File.WriteAllText(pascal, "program Example; begin writeln('ok'); end.");
    typeof(MainWindow).GetMethod("OpenEditor", flags)!.Invoke(window, new object[] { Path.Combine(temp, "App", "MainWindow.xaml.cs") });
    typeof(MainWindow).GetMethod("OpenEditor", flags)!.Invoke(window, new object[] { pascal });
    var tabs = (System.Windows.Controls.TabControl)window.FindName("EditorTabs")!;
    if (tabs.Items.Count != 3) throw new Exception("Tabbed editor did not open three files.");
    var htmlPage = Path.Combine(temp, "App", "page.html");
    File.WriteAllText(htmlPage, "<!doctype html><html><head><title>Visual</title></head><body><h1>Edit me</h1></body></html>");
    typeof(MainWindow).GetMethod("OpenEditor", flags)!.Invoke(window, new object[] { htmlPage });
    if (tabs.Items.Count != 4 || ((System.Windows.Controls.TabItem)tabs.SelectedItem).Content is not System.Windows.Controls.Grid)
        throw new Exception("HTML visual editor did not open in a tab.");
    var documents = (System.Collections.IDictionary)typeof(MainWindow).GetField("openEditors", flags)!.GetValue(window)!;
    var htmlDocument = documents[htmlPage]!;
    var htmlEditor = (ICSharpCode.AvalonEdit.TextEditor)htmlDocument.GetType().GetProperty("Editor")!.GetValue(htmlDocument)!;
    if (!htmlEditor.Text.Contains('\n') || htmlDocument.GetType().GetProperty("HtmlVisual")!.GetValue(htmlDocument) == null)
        throw new Exception("Existing one-line HTML was not displayed as readable code with visual mode.");
    htmlEditor.Text += "\n<!-- unsaved test -->";
    var unsavedNames = typeof(MainWindow).GetMethod("UnsavedFileNames", flags)!;
    if (!((string[])unsavedNames.Invoke(window, null)!).Any(name => name.EndsWith("page.html", StringComparison.OrdinalIgnoreCase))) throw new Exception("Unsaved HTML is not detected when closing.");
    typeof(MainWindow).GetMethod("SaveAllEditors", flags)!.Invoke(window, null);
    if (((string[])unsavedNames.Invoke(window, null)!).Any(name => name.EndsWith("page.html", StringComparison.OrdinalIgnoreCase))) throw new Exception("Saved HTML still appears unsaved.");
    var project = Path.Combine(temp, "App", "SmokeApp.csproj");
    var psi = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = temp,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };
    psi.ArgumentList.Add("publish");
    psi.ArgumentList.Add(project);
    psi.ArgumentList.Add("-r");
    psi.ArgumentList.Add("win-x64");
    psi.ArgumentList.Add("--self-contained");
    psi.ArgumentList.Add("true");
    var single = Environment.GetEnvironmentVariable("SMOKE_SINGLE") != "0";
    psi.ArgumentList.Add("-p:PublishSingleFile=" + single.ToString().ToLowerInvariant());
    if (single && Environment.GetEnvironmentVariable("SMOKE_NATIVE") != "0")
        psi.ArgumentList.Add("-p:IncludeNativeLibrariesForSelfExtract=true");
    psi.ArgumentList.Add("-o");
    psi.ArgumentList.Add(Path.Combine(temp, "publish"));
    using var process = Process.Start(psi)!;
    var output = process.StandardOutput.ReadToEndAsync();
    var error = process.StandardError.ReadToEndAsync();
    process.WaitForExit();
    if (process.ExitCode != 0) throw new Exception(output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
    if (!File.Exists(Path.Combine(temp, "publish", "SmokeApp.exe"))) throw new Exception("Published EXE was not created.");
    if (single && Environment.GetEnvironmentVariable("SMOKE_NATIVE") != "0")
    {
        var extraDlls = Directory.EnumerateFiles(Path.Combine(temp, "publish"), "*.dll").ToList();
        if (extraDlls.Count > 0) throw new Exception("Publish still requires separate DLLs: " + string.Join(", ", extraDlls.Select(Path.GetFileName)));
    }
    var hybridFolder = Path.Combine(temp, "App", "NativeModules");
    Directory.CreateDirectory(hybridFolder);
    File.WriteAllText(Path.Combine(hybridFolder, "forge_native.c"), "__declspec(dllexport) int __cdecl ForgeAdd(int a, int b) { return a+b; }\n");
    typeof(MainWindow).GetMethod("GenerateApp", flags)!.Invoke(window, null);
    if (!File.ReadAllText(project).Contains("<TargetPath>forge_native.dll</TargetPath>")) throw new Exception("Hybrid DLL is not published beside the EXE.");
    var hybridBuild = (Task<bool>)typeof(MainWindow).GetMethod("BuildHybridModule", flags)!.Invoke(window, new object[] { false })!;
    if (!hybridBuild.GetAwaiter().GetResult() || !File.Exists(Path.Combine(hybridFolder, "forge_native.dll"))) throw new Exception("Hybrid C module did not compile.");
    var hybridOutput = Path.Combine(temp, "hybrid-publish");
    var hybridPsi = new ProcessStartInfo("dotnet") { WorkingDirectory = temp, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
    foreach (var arg in new[] { "publish", project, "-c", "Release", "-r", "win-x64", "--self-contained", "true", "-p:PublishSingleFile=false", "-o", hybridOutput }) hybridPsi.ArgumentList.Add(arg);
    using (var hybridPublish = Process.Start(hybridPsi)!)
    {
        var standard = hybridPublish.StandardOutput.ReadToEndAsync();
        var errors = hybridPublish.StandardError.ReadToEndAsync();
        hybridPublish.WaitForExit();
        if (hybridPublish.ExitCode != 0) throw new Exception("Hybrid publish failed: " + standard.GetAwaiter().GetResult() + errors.GetAwaiter().GetResult());
    }
    if (!File.Exists(Path.Combine(hybridOutput, "forge_native.dll"))) throw new Exception("Hybrid DLL missing beside EXE.");
    typeof(MainWindow).GetMethod("CreateProjectFromRequest", flags)!.Invoke(window, new object[] { temp, "HelloNoCode", "O aplicație Hello World" });
    var quickProject = Path.Combine(temp, "HelloNoCode", "App");
    if (!File.Exists(Path.Combine(quickProject, "HelloNoCode.csproj")) ||
        !File.ReadAllText(Path.Combine(quickProject, "MainWindow.xaml")).Contains("Hello, World!"))
        throw new Exception("No-code Hello World project was not generated.");
    var helloSource = (string)typeof(MainWindow).GetMethod("HelloWorldSource", flags)!.Invoke(window, new object[] { Path.Combine(quickProject, "MainWindow.xaml.cs") })!;
    if (!helloSource.Contains("MessageBox.Show")) throw new Exception("Hello World code generation failed.");
    foreach (var language in new[] { "C", "C++", "Pascal", "VB" })
    {
        var native = Path.Combine(temp, "Native" + language.Replace("+", "p"));
        Directory.CreateDirectory(native);
        typeof(MainWindow).GetField("projectDir", flags)!.SetValue(window, native);
        typeof(MainWindow).GetField("state", flags)!.SetValue(window, new ProjectState
        {
            Name = "NativeSmoke",
            Language = language,
            Items = new() { new DesignItem { Type = "Button", Name = "hello", Text = "Hello", X = 30, Y = 40 } },
            Forms = new() { ["MainWindow"] = new FormSettings { Title = "Native test" } }
        });
        typeof(MainWindow).GetMethod("GenerateApp", flags)!.Invoke(window, null);
        var generated = Path.Combine(native, "App", language switch { "C" => "forge_ui.c", "C++" => "forge_ui.cpp", "VB" => "forge_ui.vb", _ => "forge_ui.pas" });
        if (!File.Exists(generated) || !File.ReadAllText(generated).Contains("Hello"))
            throw new Exception("Native visual source was not generated for " + language);
        var nativeExe = Path.Combine(native, "App", "NativeSmoke.exe");
        if (language == "VB")
        {
            var vbProject = Path.Combine(native, "App", "NativeSmoke.vbproj");
            using var compile = Process.Start(new ProcessStartInfo("dotnet") { ArgumentList = { "publish", vbProject, "-c", "Release", "-r", "win-x64", "--self-contained", "true", "-o", Path.Combine(native, "publish") },
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true })!;
            var outputText = compile.StandardOutput.ReadToEnd(); var errorText = compile.StandardError.ReadToEnd(); compile.WaitForExit();
            if (compile.ExitCode != 0 || !File.Exists(Path.Combine(native, "publish", "NativeSmoke.exe")))
                throw new Exception("VB compile failed: " + outputText + errorText);
        }
        else if (language == "Pascal")
        {
            var fpc = Path.GetFullPath(Path.Combine("tools", "fpc", "bin", "i386-win32", "fpc.exe"));
            if (File.Exists(fpc))
            {
                using var compile = Process.Start(new ProcessStartInfo(fpc) { ArgumentList = { generated, "-o" + nativeExe },
                    RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true })!;
                var outputText = compile.StandardOutput.ReadToEnd(); compile.WaitForExit();
                if (compile.ExitCode != 0 || !File.Exists(nativeExe)) throw new Exception("Pascal compile failed: " + outputText);
            }
        }
        else
        {
            var buildInfo = new ProcessStartInfo { RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.Combine(native, "App") };
            var configure = (Task<bool>)typeof(MainWindow).GetMethod("ConfigureMsvc", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, new object[] { buildInfo, generated, nativeExe, false })!;
            var configured = configure.GetAwaiter().GetResult();
            if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio")) && !configured)
                throw new Exception("MSVC is installed but Formalini Forge did not detect it.");
            if (configured)
            {
                using var compile = Process.Start(buildInfo)!;
                var outputText = compile.StandardOutput.ReadToEnd(); var errorText = compile.StandardError.ReadToEnd(); compile.WaitForExit();
                if (compile.ExitCode != 0 || !File.Exists(nativeExe)) throw new Exception(language + " compile failed: " + outputText + errorText);
            }
        }
    }
    foreach (var language in new[] { "Vue", "React", "React Native" })
    {
        var webRoot = Path.Combine(temp, language.Replace(' ', '_'));
        Directory.CreateDirectory(webRoot);
        typeof(MainWindow).GetField("projectDir", flags)!.SetValue(window, webRoot);
        typeof(MainWindow).GetField("state", flags)!.SetValue(window, new ProjectState
        {
            Name = "WebSmoke", Language = language,
            Items = language == "React" ? new()
            {
                new DesignItem { Name = "greeting", Type = "Heading", Text = "Hello web", X = 12, Y = 22 },
                new DesignItem { Name = "panel", Type = "Container", Text = "Panel", X = 20, Y = 70 },
                new DesignItem { Name = "button", Type = "Button", Text = "Click", X = 20, Y = 120 }
            } : new() { new DesignItem { Name = "greeting", Type = language == "React Native" ? "Text" : "Heading", Text = "Hello web", X = 12, Y = 22 } }
        });
        typeof(MainWindow).GetMethod("ConfigureToolboxForProject", flags)!.Invoke(window, null);
        if (((System.Windows.Controls.ListBoxItem)toolbox.Items[0]).Tag is not string) throw new Exception(language + " toolbox is missing controls.");
        typeof(MainWindow).GetMethod("GenerateApp", flags)!.Invoke(window, null);
        var app = Path.Combine(webRoot, "App");
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(app, "package.json")));
        if (document.RootElement.GetProperty("name").GetString() != "websmoke") throw new Exception(language + " package invalid.");
        if (language != "React Native" && File.ReadAllLines(Path.Combine(app, "index.html")).Length < 8) throw new Exception(language + " HTML entry point is not readable.");
        var generated = Path.Combine(app, "src", language == "Vue" ? "ForgeLayout.vue" : "ForgeLayout.jsx");
        var screen = File.ReadAllText(generated);
        if (!screen.Contains("Hello web") || (language != "Vue" && !screen.Contains("style={{"))) throw new Exception(language + " designer did not generate a valid screen.");
        if (language == "React" && screen.Contains("style=\"", StringComparison.Ordinal)) throw new Exception("React designer emitted an HTML string style prop.");
        var wrapper = Path.Combine(app, "src", language == "Vue" ? "ForgeScreen.vue" : "ForgeScreen.jsx");
        File.AppendAllText(wrapper, "// user code remains\n");
        typeof(MainWindow).GetMethod("GenerateApp", flags)!.Invoke(window, null);
        if (!File.ReadAllText(wrapper).Contains("user code remains")) throw new Exception(language + " regeneration overwrote custom code.");
    }
    Console.WriteLine("Generated Windows and web/mobile source projects.");
}
  finally
  {
      var resolved = Path.GetFullPath(temp);
      if (Environment.GetEnvironmentVariable("SMOKE_KEEP") == "1") Console.WriteLine("Kept smoke files: " + resolved);
      else if (resolved.StartsWith(tempRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
          Path.GetFileName(resolved).StartsWith("FormaliniForgeSmoke-", StringComparison.Ordinal))
          Directory.Delete(resolved, true);
  }
 }
}
