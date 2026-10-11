using System.Diagnostics;
using System.IO;

namespace FormaliniForge;

public partial class MainWindow
{
    private static async Task<bool> ConfigureMsvc(ProcessStartInfo target, string source, string exe, bool debug)
    {
        var visualStudio = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio");
        if (!Directory.Exists(visualStudio)) return false;
        string? compiler;
        try
        {
            compiler = Directory.EnumerateFiles(visualStudio, "cl.exe", SearchOption.AllDirectories)
                .Where(x => x.Contains("Hostx64" + Path.DirectorySeparatorChar + "x64", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        }
        catch { return false; }
        if (compiler == null) return false;
        var marker = compiler.IndexOf(Path.DirectorySeparatorChar + "VC" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        if (marker < 0) return false;
        var setup = Path.Combine(compiler[..marker], "Common7", "Tools", "VsDevCmd.bat");
        if (!File.Exists(setup)) return false;
        var envPsi = new ProcessStartInfo("cmd.exe")
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
        };
        envPsi.Arguments = "/d /c call \"" + setup + "\" -no_logo -arch=x64 >nul && set";
        using var envProcess = Process.Start(envPsi);
        if (envProcess == null) return false;
        var contents = await envProcess.StandardOutput.ReadToEndAsync();
        await envProcess.WaitForExitAsync();
        if (envProcess.ExitCode != 0)
            throw new InvalidOperationException("MSVC environment setup failed: " + await envProcess.StandardError.ReadToEndAsync());
        foreach (var line in contents.Split('\n'))
        {
            var index = line.IndexOf('=');
            if (index > 0) target.Environment[line[..index]] = line[(index + 1)..].TrimEnd('\r');
        }
        target.FileName = compiler;
        target.ArgumentList.Add("/nologo"); target.ArgumentList.Add("/utf-8");
        if (debug) target.ArgumentList.Add("/Zi");
        target.ArgumentList.Add(source);
        target.ArgumentList.Add("/Fe:" + exe);
        target.ArgumentList.Add("/Fo:" + Path.Combine(Path.GetDirectoryName(exe)!, "forge_ui.obj"));
        target.ArgumentList.Add("/link"); target.ArgumentList.Add("/SUBSYSTEM:WINDOWS");
        target.ArgumentList.Add("user32.lib"); target.ArgumentList.Add("comctl32.lib");
        return true;
    }
}
