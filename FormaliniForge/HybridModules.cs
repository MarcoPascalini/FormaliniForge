using System.Diagnostics;
using System.IO;
using System.Windows;

namespace FormaliniForge;

public partial class MainWindow
{
    private void AddHybridModule_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireProject();
            if (state.Language != "C#") throw new InvalidOperationException(english ? "The native module host currently supports C#/WPF projects." : "Modulul nativ este disponibil momentan pentru proiecte C#/WPF.");
            var folder = Path.Combine(projectDir!, "App", "NativeModules");
            Directory.CreateDirectory(folder);
            var source = Path.Combine(folder, "forge_native.c");
            if (!File.Exists(source)) File.WriteAllText(source, "/* Export functions with a stable C ABI for the C# host. */\n__declspec(dllexport) int __cdecl ForgeAdd(int left, int right) { return left + right; }\n");
            var wrapper = Path.Combine(projectDir!, "App", "ForgeNative.cs");
            if (!File.Exists(wrapper)) File.WriteAllText(wrapper, "using System.Runtime.InteropServices;\nnamespace " + state.Name + ";\npublic static class ForgeNative\n{\n    [DllImport(\"forge_native.dll\", CallingConvention = CallingConvention.Cdecl)]\n    public static extern int ForgeAdd(int left, int right);\n}\n");
            GenerateApp(); SaveProject(); RefreshFiles(); OpenEditor(source);
            Status(english ? "C module and C# bridge added. Call ForgeNative.ForgeAdd(a, b)." : "Modul C și legătură C# adăugate. Apelează ForgeNative.ForgeAdd(a, b).");
        }
        catch (Exception ex) { Error(ex); }
    }

    private async Task<bool> BuildHybridModule(bool debug)
    {
        var source = Path.Combine(projectDir!, "App", "NativeModules", "forge_native.c");
        if (!File.Exists(source)) return true;
        var output = Path.Combine(Path.GetDirectoryName(source)!, "forge_native.dll");
        var psi = new ProcessStartInfo { WorkingDirectory = Path.GetDirectoryName(source)!, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        var compiler = FindTool("gcc.exe", "clang.exe");
        if (compiler != null)
        {
            psi.FileName = compiler;
            psi.ArgumentList.Add("-shared"); psi.ArgumentList.Add("-o"); psi.ArgumentList.Add(output); psi.ArgumentList.Add(source);
            if (debug) psi.ArgumentList.Add("-g");
        }
        else
        {
            if (!await ConfigureMsvc(psi, source, output, debug)) throw new InvalidOperationException(english ? "Install C/C++ Build Tools, GCC or Clang for the native module." : "Instalează Build Tools C/C++, GCC sau Clang pentru modulul nativ.");
            psi.ArgumentList.Clear();
            psi.ArgumentList.Add("/nologo"); psi.ArgumentList.Add("/LD"); psi.ArgumentList.Add("/utf-8");
            psi.ArgumentList.Add(source); psi.ArgumentList.Add("/Fe:" + output);
            psi.ArgumentList.Add("/Fo:" + Path.Combine(Path.GetDirectoryName(source)!, "forge_native.obj"));
        }
        AppendBuildLine(english ? "Compiling native C module..." : "Compilez modulul nativ C...");
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("C compiler did not start.");
        await Task.WhenAll(PumpProcessOutput(process.StandardOutput, ""), PumpProcessOutput(process.StandardError, "ERROR "), process.WaitForExitAsync());
        if (process.ExitCode == 0 && File.Exists(output)) return true;
        RunStatus.Text = english ? "Native module build failed" : "Compilarea modulului nativ a eșuat";
        return false;
    }
}
