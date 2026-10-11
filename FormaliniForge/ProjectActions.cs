using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace FormaliniForge;

public partial class MainWindow
{
    private string? ChooseProjectLanguage()
    {
        var dialog = new Window { Title = english ? "Choose project type" : "Alege tipul proiectului", Owner = this,
            Width = 430, Height = 255, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize, Background = System.Windows.Media.Brushes.White };
        var layout = new System.Windows.Controls.StackPanel { Margin = new Thickness(22) };
        layout.Children.Add(new System.Windows.Controls.TextBlock { Text = english ? "What would you like to build?" : "Ce vrei să construiești?",
            FontSize = 19, FontWeight = FontWeights.SemiBold, Foreground = System.Windows.Media.Brushes.Black, Margin = new Thickness(0, 0, 0, 10) });
        var picker = new System.Windows.Controls.ComboBox { MinHeight = 36 };
        foreach (var language in new[] { "C#", "C", "C++", "Pascal", "VB", "Vue", "React", "React Native" }) picker.Items.Add(language);
        picker.SelectedIndex = 0;
        layout.Children.Add(picker);
        var hint = new System.Windows.Controls.TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = System.Windows.Media.Brushes.DimGray, Margin = new Thickness(0, 9, 0, 18) };
        void Explain() => hint.Text = picker.SelectedItem?.ToString() switch
        {
            "Vue" or "React" => english ? "Website with a visual designer. Build creates files ready for web hosting." : "Site web cu designer vizual. Build creează fișiere pentru găzduire web.",
            "React Native" => english ? "Mobile app with a visual designer. Build packages Expo source for Android/iOS deployment." : "Aplicație mobilă cu designer vizual. Build împachetează sursa Expo pentru Android/iOS.",
            _ => english ? "Windows application. Build creates an executable." : "Aplicație Windows. Build creează un executabil."
        };
        picker.SelectionChanged += (_, _) => Explain(); Explain(); layout.Children.Add(hint);
        var buttons = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        var cancel = new System.Windows.Controls.Button { Content = english ? "Cancel" : "Anulează", Width = 90, Margin = new Thickness(0, 0, 8, 0) };
        cancel.Click += (_, _) => dialog.DialogResult = false;
        var create = new System.Windows.Controls.Button { Content = english ? "Create" : "Creează", Width = 90, IsDefault = true };
        create.Click += (_, _) => dialog.DialogResult = true;
        buttons.Children.Add(cancel); buttons.Children.Add(create); layout.Children.Add(buttons);
        dialog.Content = layout;
        return dialog.ShowDialog() == true ? picker.SelectedItem?.ToString() : null;
    }

    private static IEnumerable<string> EnumerateProjectSourceFiles(string root)
    {
        var ignored = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".forge-history", ".git", "obj", "bin", "publish", "publish-debug", "node_modules", "dist", ".expo", ".forge-npm-cache" };
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var folder = pending.Pop();
            foreach (var file in Directory.EnumerateFiles(folder)) yield return file;
            foreach (var child in Directory.EnumerateDirectories(folder))
                if (!ignored.Contains(Path.GetFileName(child))) pending.Push(child);
        }
    }

    private void NewFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireProject();
            var suggested = state.Language switch
            {
                "C#" => "App/NewFile.cs", "C" => "App/new_file.c", "C++" => "App/new_file.cpp",
                "Pascal" => "App/new_unit.pas", "VB" => "App/NewFile.vb",
                "Vue" => "App/src/NewComponent.vue", "React" => "App/src/NewComponent.jsx",
                "React Native" => "App/components/NewComponent.jsx", _ => "App/new_file.txt"
            };
            var relative = Microsoft.VisualBasic.Interaction.InputBox(
                english ? "New file path inside the project (any extension):" : "Calea fișierului nou în proiect (orice extensie):",
                T("Fișier nou..."), suggested).Trim().Replace('/', Path.DirectorySeparatorChar);
            if (relative.Length == 0) return;
            if (Path.IsPathRooted(relative)) throw new InvalidOperationException(english ? "Use a path inside the project." : "Folosește o cale din interiorul proiectului.");
            var root = Path.GetFullPath(projectDir!);
            var full = Path.GetFullPath(Path.Combine(root, relative));
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                relative.Split(Path.DirectorySeparatorChar).Any(x => x is ".git" or "node_modules" or "obj" or "bin" or "publish" or "publish-debug"))
                throw new InvalidOperationException(english ? "That file path is not allowed." : "Această cale nu este permisă.");
            if (File.Exists(full)) throw new IOException(english ? "The file already exists." : "Fișierul există deja.");
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, "");
            RefreshFiles(); OpenEditor(full);
            Status(english ? "File created." : "Fișier creat.");
        }
        catch (Exception ex) { Error(ex); }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Formalini Forge\nformalini.com\nDeveloper: Marco PASCALINI",
            T("Despre Formalini Forge"), MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
