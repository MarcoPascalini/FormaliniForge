using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;

namespace FormaliniForge;

public partial class MainWindow
{
    private Process? activeRun;
    private DispatcherTimer? watchTimer;
    private string? watchLogPath;
    private long watchReadOffset;
    private string watchRemainder = "";
    private readonly Dictionary<string, string> lastWatchValues = new();

    private const string ForgeWatchSource = """
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace __NAMESPACE__;

internal static class ForgeWatch
{
    private static readonly object Gate = new();

    public static void Record<T>(string name, Func<T> evaluate, string file, int line)
    {
        var path = Environment.GetEnvironmentVariable("FORMALINI_FORGE_WATCH");
        if (string.IsNullOrWhiteSpace(path)) return;
        string value;
        try
        {
            var result = evaluate();
            value = result is null ? "<null>" : result.ToString() ?? "<null>";
        }
        catch (Exception ex) { value = "<error: " + ex.Message + ">"; }
        try
        {
            var entry = JsonSerializer.Serialize(new { time = DateTimeOffset.Now.ToString("HH:mm:ss.fff"), file, line, name, value });
            lock (Gate) File.AppendAllText(path, entry + Environment.NewLine);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

internal sealed class ForgeTraceListener : TraceListener
{
    public override void Write(string? message) => ForgeWatch.Record("Trace", () => message ?? "", "Trace", 0);
    public override void WriteLine(string? message) => ForgeWatch.Record("Trace", () => message ?? "", "Trace", 0);
}
""";

    private void AppendBuildLine(string line)
    {
        if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(() => AppendBuildLine(line)); return; }
        BuildOutput.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + line + Environment.NewLine);
        BuildOutput.ScrollToEnd();
    }

    private async Task PumpProcessOutput(StreamReader reader, string prefix)
    {
        string? line;
        while ((line = await reader.ReadLineAsync()) != null) AppendBuildLine(prefix + line);
    }

    private void AddWatch_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireProject();
            if (state.Language != "C#") throw new InvalidOperationException("Line watches currently support C#/WPF projects.");
            var editor = CurrentEditor ?? throw new InvalidOperationException("Open a C# source file first.");
            var path = CurrentEditorPath() ?? throw new InvalidOperationException("Open a C# source file first.");
            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Select a C# source file.");
            var expression = Microsoft.VisualBasic.Interaction.InputBox(
                english ? "Which variable or property should be watched at this line?" : "Ce variabilă sau proprietate urmărești la această linie?",
                english ? "Watch value" : "Urmărește valoarea", editor.SelectedText.Trim()).Trim();
            if (expression.Length == 0) return;
            if (!Regex.IsMatch(expression, @"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$"))
                throw new InvalidOperationException("Use a variable or property name, for example count or customer.Name.");
            var line = editor.Document.GetLineByOffset(editor.CaretOffset);
            var sourceLine = editor.Document.GetText(line);
            if (!sourceLine.TrimEnd().EndsWith(';')) throw new InvalidOperationException("Place the caret on a completed C# statement (a line ending in ;).");
            var indentation = Regex.Match(sourceLine, @"^\s*").Value;
            var relative = Path.GetRelativePath(Path.Combine(projectDir!, "App"), path).Replace('\\', '/');
            var watch = indentation + "ForgeWatch.Record(" + JsonSerializer.Serialize(expression) + ", () => " + expression + ", " + JsonSerializer.Serialize(relative) + ", " + line.LineNumber + "); // forge-watch" + Environment.NewLine;
            Snapshot();
            var insertAt = line.Offset + line.TotalLength;
            editor.Document.Insert(insertAt, (line.DelimiterLength == 0 ? Environment.NewLine : "") + watch);
            SaveAllEditors();
            lastWatchValues[relative + ":" + line.LineNumber + ":" + expression] = "waiting for run";
            RefreshWatchList();
            Status(english ? "Watch added. Run with F5 to see live values." : "Urmărirea a fost adăugată. Rulează cu F5 pentru valori live.");
        }
        catch (Exception ex) { Error(ex); }
    }

    private void RefreshWatchList()
    {
        WatchList.Items.Clear();
        foreach (var (key, value) in lastWatchValues) WatchList.Items.Add(key + " = " + value);
    }

    private void LoadWatchDefinitions()
    {
        lastWatchValues.Clear();
        if (projectDir == null || state.Language != "C#") { RefreshWatchList(); return; }
        var appDir = Path.Combine(projectDir, "App");
        if (!Directory.Exists(appDir)) return;
        foreach (var path in Directory.EnumerateFiles(appDir, "*.cs", SearchOption.TopDirectoryOnly))
            foreach (var line in File.ReadLines(path).Where(x => x.Contains("// forge-watch", StringComparison.Ordinal)))
            {
                var match = Regex.Match(line, "ForgeWatch\\.Record\\(\\\"(?<name>[^\\\"]+)\\\",.*?\\\"(?<file>[^\\\"]+)\\\",\\s*(?<line>\\d+)\\)");
                if (match.Success) lastWatchValues[match.Groups["file"].Value + ":" + match.Groups["line"].Value + ":" + match.Groups["name"].Value] = "waiting for run";
            }
        RefreshWatchList();
    }

    private async Task LaunchAndMonitor(string exe)
    {
        if (activeRun is { HasExited: false }) throw new InvalidOperationException("The application is already running. Stop it before starting another instance.");
        watchLogPath = Path.Combine(Path.GetTempPath(), "FormaliniForge", "watch-" + Guid.NewGuid().ToString("N") + ".jsonl");
        Directory.CreateDirectory(Path.GetDirectoryName(watchLogPath)!);
        watchReadOffset = 0;
        watchRemainder = "";
        var psi = new ProcessStartInfo(exe)
        {
            WorkingDirectory = Path.GetDirectoryName(exe)!, UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        psi.Environment["FORMALINI_FORGE_WATCH"] = watchLogPath;
        var process = Process.Start(psi) ?? throw new InvalidOperationException("The application did not start.");
        activeRun = process;
        MainTabs.SelectedIndex = 2;
        RunStatus.Text = (english ? "Running · PID " : "Rulează · PID ") + process.Id;
        AppendBuildLine("Started " + exe + " (PID " + process.Id + ").");
        watchTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) => ReadWatchUpdates(), Dispatcher);
        watchTimer.Start();
        try
        {
            await Task.WhenAll(PumpProcessOutput(process.StandardOutput, "APP "), PumpProcessOutput(process.StandardError, "APP ERROR "), process.WaitForExitAsync());
            ReadWatchUpdates();
            RunStatus.Text = (english ? "Exited · code " : "Încheiat · cod ") + process.ExitCode;
            AppendBuildLine("Application exited with code " + process.ExitCode + ".");
        }
        finally
        {
            watchTimer.Stop();
            watchTimer = null;
            activeRun = null;
            process.Dispose();
        }
    }

    private void StopRun_Click(object sender, RoutedEventArgs e)
    {
        if (activeRun is not { HasExited: false } running) return;
        try { running.Kill(entireProcessTree: true); AppendBuildLine("Stopped by user."); }
        catch (Exception ex) { Error(ex); }
    }

    private void ReadWatchUpdates()
    {
        if (watchLogPath == null || !File.Exists(watchLogPath)) return;
        try
        {
            using var stream = new FileStream(watchLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (stream.Length < watchReadOffset) watchReadOffset = 0;
            stream.Seek(watchReadOffset, SeekOrigin.Begin);
            using var reader = new StreamReader(stream, Encoding.UTF8, false, leaveOpen: true);
            var chunk = reader.ReadToEnd();
            watchReadOffset = stream.Position;
            watchRemainder += chunk;
            var lines = watchRemainder.Split('\n');
            watchRemainder = lines[^1];
            foreach (var raw in lines[..^1])
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                using var value = JsonDocument.Parse(raw.Trim());
                var root = value.RootElement;
                var file = root.GetProperty("file").GetString() ?? "";
                var line = root.GetProperty("line").GetInt32();
                var name = root.GetProperty("name").GetString() ?? "";
                var current = root.GetProperty("value").GetString() ?? "";
                lastWatchValues[file + ":" + line + ":" + name] = current;
                AppendBuildLine("WATCH " + file + ":" + line + " " + name + " = " + current);
            }
            if (lines.Length > 1) RefreshWatchList();
        }
        catch (IOException) { }
        catch (JsonException) { }
    }

    private void AttachWatchHover(TextEditor editor, string path)
    {
        editor.TextArea.TextView.MouseMove += (_, args) =>
        {
            if (lastWatchValues.Count == 0) return;
            var position = editor.TextArea.TextView.GetPosition(args.GetPosition(editor.TextArea.TextView));
            if (position == null) return;
            var line = editor.Document.GetLineByNumber(position.Value.Line);
            var text = editor.Document.GetText(line);
            var relative = projectDir == null ? "" : Path.GetRelativePath(Path.Combine(projectDir, "App"), path).Replace('\\', '/');
            var match = lastWatchValues.FirstOrDefault(x => x.Key.StartsWith(relative + ":", StringComparison.OrdinalIgnoreCase) && text.Contains(x.Key[(x.Key.LastIndexOf(':') + 1)..], StringComparison.Ordinal));
            editor.ToolTip = match.Key == null ? null : match.Key[(match.Key.LastIndexOf(':') + 1)..] + " = " + match.Value;
        };
    }
}
