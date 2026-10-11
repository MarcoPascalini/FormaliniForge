// Copyright (c) 2026 Marco Irinel Pascalini (https://formalini.com)
// SPDX-License-Identifier: MIT
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;

namespace FormaliniForge;

public partial class MainWindow
{
    private string? pendingAiTarget;

    private static bool IsHelloWorldRequest(string request)
        => Regex.IsMatch(request, @"hello\s*[,!]?\s*world|salut\s*[,!]?\s*lume|bun[aă]\s*[,!]?\s*lume", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private async void QuickStart_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Window
            {
                Owner = this, Title = "Creează aplicația din descriere", Width = 610, Height = 420,
                MinWidth = 500, MinHeight = 370, WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new SolidColorBrush(Color.FromRgb(18, 29, 46)), Foreground = Brushes.White,
                ResizeMode = ResizeMode.NoResize
            };
            var layout = new StackPanel { Margin = new Thickness(24) };
            layout.Children.Add(new TextBlock { Text = "Ce aplicație vrei să creezi?", FontSize = 22, FontWeight = FontWeights.SemiBold });
            layout.Children.Add(new TextBlock { Text = "Exemplu: O aplicație Hello World care afișează Salut, lume!", Margin = new Thickness(0, 5, 0, 8), Foreground = Brushes.LightSteelBlue });
            var requestBox = new TextBox { Text = english ? "A Hello World application" : "O aplicație Hello World", Height = 85, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
            layout.Children.Add(requestBox);
            layout.Children.Add(new TextBlock { Text = "Numele aplicației", Margin = new Thickness(0, 12, 0, 4) });
            var nameBox = new TextBox { Text = "HelloWorld" };
            layout.Children.Add(nameBox);
            layout.Children.Add(new TextBlock { Text = "Folderul în care creezi proiectul", Margin = new Thickness(0, 12, 0, 4) });
            var folderRow = new DockPanel();
            var browse = new Button { Content = "Alege folderul", Margin = new Thickness(6, 0, 0, 0) };
            DockPanel.SetDock(browse, Dock.Right);
            folderRow.Children.Add(browse);
            var folderBox = new TextBox { Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) };
            folderRow.Children.Add(folderBox);
            browse.Click += (_, _) =>
            {
                using var picker = new System.Windows.Forms.FolderBrowserDialog { Description = T("Alege folderul pentru noul proiect") };
                if (picker.ShowDialog() == System.Windows.Forms.DialogResult.OK) folderBox.Text = picker.SelectedPath;
            };
            layout.Children.Add(folderRow);
            var autoRun = new System.Windows.Controls.CheckBox { Content = "Construiește și pornește aplicația imediat", IsChecked = true, Margin = new Thickness(0, 10, 0, 0), Foreground = Brushes.White };
            layout.Children.Add(autoRun);
            var create = new Button { Content = "Creează proiectul", Margin = new Thickness(0, 18, 0, 0), Padding = new Thickness(16, 9, 16, 9), HorizontalAlignment = System.Windows.HorizontalAlignment.Left, Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)) };
            create.Click += (_, _) => { dialog.DialogResult = true; dialog.Close(); };
            layout.Children.Add(create);
            dialog.Content = layout;
            dialog.Title = T(dialog.Title);
            LocalizeTree(dialog);
            requestBox.Focus();
            if (dialog.ShowDialog() != true) return;
            CreateProjectFromRequest(folderBox.Text.Trim(), nameBox.Text.Trim(), requestBox.Text.Trim());
            if (IsHelloWorldRequest(requestBox.Text) && autoRun.IsChecked == true && await BuildProject())
            {
                Process.Start(new ProcessStartInfo(lastBuiltExe!) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(lastBuiltExe!)! });
                Status("The Hello World application is open.");
            }
        }
        catch (Exception ex) { Error(ex); }
    }

    private void CreateProjectFromRequest(string root, string name, string request)
    {
        if (!Directory.Exists(root)) throw new InvalidOperationException("Choose an existing folder for the project.");
        if (!Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$")) throw new InvalidOperationException("The application name must contain only letters, digits, and _.");
        if (request.Length == 0) throw new InvalidOperationException("Describe the application before continuing.");
        var hello = IsHelloWorldRequest(request);
        if (!hello && (Provider == "Fără AI" || ApiKeyBox.Password.Trim().Length == 0))
            throw new InvalidOperationException("For requests beyond Hello World, choose OpenAI or Gemini and enter an API key. The Hello World example works without a key.");
        var target = Path.Combine(root, name);
        if (Directory.Exists(target) && Directory.EnumerateFileSystemEntries(target).Any())
            throw new InvalidOperationException("The project folder already exists and is not empty.");
        SaveCode();
        Directory.CreateDirectory(target);
        projectDir = target;
        var greeting = Regex.IsMatch(request, "salut|bună|buna", RegexOptions.IgnoreCase) ? "Salut, lume!" : "Hello, World!";
        state = new ProjectState
        {
            Name = name, Language = "C#",
            Items = hello ? new()
            {
                new DesignItem { Type = "TextBlock", Name = "helloTitle", Text = greeting, X = 70, Y = 95, Width = 650, Height = 74, FontSize = 36, Foreground = "#16315C", Options = new() { ["FontWeight"] = "Bold" } },
                new DesignItem { Type = "TextBlock", Name = "helloHint", Text = "Creat cu Formalini Forge", X = 73, Y = 185, Width = 450, Height = 36, FontSize = 16, Foreground = "#51637E" }
            } : new(),
            Forms = new() { ["MainWindow"] = new FormSettings { Title = hello ? greeting : name, Background = "#F8FAFC" } }
        };
        GenerateApp();
        SaveProject();
        OpenProject(target);
        MainTabs.SelectedIndex = 0;
        if (hello) Status("The Hello World application is ready. Press F5 to build and run it.");
        else
        {
            PromptBox.Text = "Create the application described below. Generate the required interface and code, then briefly explain the result.\n\n" + request;
            SendPrompt_Click(this, new RoutedEventArgs());
        }
    }

    private void GenerateCode_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireProject();
            var request = CodeRequestBox.Text.Trim();
            if (request.Length == 0) throw new InvalidOperationException("Describe what the open file should do.");
            var document = openEditors.Values.FirstOrDefault(x => ReferenceEquals(x.Tab, EditorTabs.SelectedItem));
            if (document == null) throw new InvalidOperationException("Open a file from the project explorer first.");
            if (IsHelloWorldRequest(request))
            {
                var source = HelloWorldSource(document.Path);
                Snapshot();
                document.Editor.Text = source;
                SaveProject();
                Status("Hello World code was generated in " + Path.GetFileName(document.Path) + ".");
                CodeRequestBox.Text = "";
                return;
            }
            if (Provider == "Fără AI" || ApiKeyBox.Password.Trim().Length == 0)
                throw new InvalidOperationException("For general code requests, choose OpenAI or Gemini and enter an API key. Hello World also works without AI.");
            pendingAiTarget = Path.GetRelativePath(projectDir!, document.Path).Replace('\\', '/');
            PromptBox.Text = "Modify only file " + pendingAiTarget + " for this request: " + request + ". Keep the necessary existing code and return the complete file.";
            SendPrompt_Click(sender, e);
            CodeRequestBox.Text = "";
        }
        catch (Exception ex) { pendingAiTarget = null; Error(ex); }
    }

    private string HelloWorldSource(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        var greeting = "Hello, World!";
        if (extension == ".c") return "#include <stdio.h>\nint main(void) { puts(\"" + greeting + "\"); return 0; }\n";
        if (extension == ".cpp") return "#include <iostream>\nint main() { std::cout << \"" + greeting + "\\n\"; return 0; }\n";
        if (extension == ".pas") return "program " + state.Name + ";\nbegin\n  writeln('" + greeting + "');\nend.\n";
        if (extension == ".cs")
        {
            var filename = Path.GetFileName(path);
            if (filename.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase))
            {
                var className = filename[..^8];
                return "using System.Windows;\nnamespace " + state.Name + ";\npublic partial class " + className + " : Window\n{\n    public " + className + "()\n    {\n        InitializeComponent();\n        Loaded += (_, _) => MessageBox.Show(\"" + greeting + "\");\n    }\n}\n";
            }
            var typeName = Path.GetFileNameWithoutExtension(path);
            if (!Regex.IsMatch(typeName, @"^[A-Za-z_][A-Za-z0-9_]*$")) throw new InvalidOperationException("Choose a C# file with a valid name.");
            return "namespace " + state.Name + ";\npublic static class " + typeName + "\n{\n    public static string Message => \"" + greeting + "\";\n}\n";
        }
        throw new InvalidOperationException("For Hello World, open a .cs, .c, .cpp, or .pas file.");
    }
}
