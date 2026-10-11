using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FormaliniForge;

public partial class MainWindow
{
    private void ChooseFormIcon_Click(object sender, RoutedEventArgs e)
    {
        if (projectDir == null) return;
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Windows icon (*.ico)|*.ico", Title = T("Alege iconița ferestrei") };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var assetDir = Path.Combine(projectDir, "App", "Assets");
            Directory.CreateDirectory(assetDir);
            var iconName = "icon-" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dialog.FileName)))[..12].ToLowerInvariant() + ".ico";
            File.Copy(dialog.FileName, Path.Combine(assetDir, iconName), true);
            Snapshot();
            CurrentForm.IconFile = "Assets/" + iconName;
            GenerateApp();
            SaveProject();
            RenderDesigner();
            RefreshInspector();
        }
        catch (Exception ex) { Error(ex); }
    }

    private BitmapImage? LoadPreviewIcon(string iconFile)
    {
        try
        {
            var path = Path.IsPathRooted(iconFile) ? iconFile : Path.Combine(projectDir ?? AppContext.BaseDirectory, "App", iconFile);
            if (File.Exists(path)) return new BitmapImage(new Uri(path, UriKind.Absolute));
        }
        catch { }
        return new BitmapImage(new Uri("pack://application:,,,/FormaliniForge;component/Assets/FormaliniForge.ico"));
    }

    private static string ReadFormattedSource(string path)
    {
        var source = File.ReadAllText(path);
        if (!path.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase))
            return source.Contains('\n') ? source : FormatReadableSource(path, source);
        var fileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path));
        var nameSpace = Path.GetFileNameWithoutExtension(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.csproj").FirstOrDefault() ?? "Project.csproj");
        var legacy = $"using System.Windows; namespace {nameSpace}; public partial class {fileName} : Window {{ public {fileName}() {{ InitializeComponent(); }} }}";
        if (source.Trim() != legacy) return source;
        source = WindowCodeBehind(nameSpace, fileName);
        File.WriteAllText(path, source);
        return source;
    }

    private static string WindowCodeBehind(string nameSpace, string windowName) => $$"""
using System.Windows;

namespace {{nameSpace}};

public partial class {{windowName}} : Window
{
    public {{windowName}}()
    {
        InitializeComponent();
    }
}
""";
}
