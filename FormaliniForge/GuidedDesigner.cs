// Copyright (c) 2026 Marco Irinel Pascalini (https://formalini.com)
// SPDX-License-Identifier: MIT
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;
using Orientation = System.Windows.Controls.Orientation;
using FontFamily = System.Windows.Media.FontFamily;

namespace FormaliniForge;

public partial class MainWindow
{
    private bool toolboxDragging;
    private static readonly Dictionary<string, (string Glyph, string Color, string Hint)> componentIcons = new()
    {
        ["Button"] = ("▣", "#60A5FA", "Action button"), ["Label"] = ("A", "#C4B5FD", "Label"),
        ["TextBlock"] = ("T", "#93C5FD", "Text"), ["TextBox"] = ("▤", "#34D399", "Text input"),
        ["PasswordBox"] = ("●", "#FBBF24", "Password input"), ["RichTextBox"] = ("≡", "#A78BFA", "Rich text"),
        ["CheckBox"] = ("☑", "#34D399", "Checkbox"), ["RadioButton"] = ("◉", "#34D399", "Radio option"),
        ["ToggleButton"] = ("◐", "#34D399", "Toggle"), ["ComboBox"] = ("▾", "#38BDF8", "Dropdown"),
        ["ListBox"] = ("☷", "#38BDF8", "List"), ["ListView"] = ("☰", "#38BDF8", "Detailed list"),
        ["TreeView"] = ("⚑", "#38BDF8", "Tree"), ["DataGrid"] = ("▦", "#FBBF24", "Data table"),
        ["DatePicker"] = ("▤", "#F472B6", "Date picker"), ["Calendar"] = ("▦", "#F472B6", "Calendar"),
        ["Slider"] = ("━", "#FB923C", "Slider"), ["ProgressBar"] = ("▰", "#FB923C", "Progress"),
        ["TabControl"] = ("▣", "#A78BFA", "Tabs"), ["GroupBox"] = ("▢", "#A78BFA", "Group"),
        ["Expander"] = ("⌄", "#A78BFA", "Expandable section"), ["Border"] = ("▯", "#94A3B8", "Border"),
        ["Rectangle"] = ("■", "#94A3B8", "Rectangle"), ["Ellipse"] = ("●", "#94A3B8", "Ellipse"),
        ["Heading"] = ("H", "#93C5FD", "Heading"), ["Text"] = ("T", "#93C5FD", "Text"),
        ["Input"] = ("▤", "#34D399", "Input"), ["TextArea"] = ("≡", "#A78BFA", "Text area"),
        ["Image"] = ("▧", "#FBBF24", "Image"), ["Select"] = ("▾", "#38BDF8", "Select"),
        ["Container"] = ("▢", "#A78BFA", "Container"), ["Link"] = ("↗", "#60A5FA", "Link"),
        ["List"] = ("☷", "#38BDF8", "List"), ["Table"] = ("▦", "#FBBF24", "Table"),
        ["View"] = ("▢", "#A78BFA", "View"), ["TextInput"] = ("▤", "#34D399", "Text input"),
        ["Pressable"] = ("▣", "#60A5FA", "Pressable"), ["Switch"] = ("◐", "#34D399", "Switch"),
        ["ScrollView"] = ("↕", "#38BDF8", "Scroll view"), ["FlatList"] = ("☷", "#38BDF8", "Flat list")
    };

    private void DecorateToolbox()
    {
        Toolbox.Background = Brush("#172D49");
        Toolbox.BorderBrush = Brush("#345579");
        Toolbox.BorderThickness = new Thickness(1);
        foreach (var entry in Toolbox.Items.OfType<ListBoxItem>())
        {
            if (entry.Tag is string) continue;
            var type = entry.Content?.ToString() ?? "";
            if (!componentIcons.TryGetValue(type, out var icon)) continue;
            entry.Tag = type;
            entry.ToolTip = ComponentHint(icon.Hint);
            entry.Background = Brush("#24466D");
            entry.Foreground = Brush("#FFFFFF");
            entry.BorderBrush = Brush("#426A97");
            entry.BorderThickness = new Thickness(1);
            entry.Padding = new Thickness(6, 4, 6, 4);
            entry.Margin = new Thickness(5, 3, 7, 3);
            entry.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 7, ShadowDepth = 2, Opacity = 0.35 };
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(3, 2, 0, 2) };
            row.Children.Add(new Border
            {
                Width = 23, Height = 23, CornerRadius = new CornerRadius(4),
                Background = Brush("#26364D"), Margin = new Thickness(0, 0, 8, 0),
                Child = new TextBlock { Text = icon.Glyph, FontFamily = new FontFamily("Segoe UI Symbol"),
                    FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = Brush(icon.Color),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center, VerticalAlignment = System.Windows.VerticalAlignment.Center }
            });
            row.Children.Add(new TextBlock { Text = ComponentName(type), Foreground = Brush("#FFFFFF"), VerticalAlignment = VerticalAlignment.Center });
            entry.Content = row;
            entry.PreviewMouseLeftButtonDown += (_, _) => { entry.Background = Brush("#1765A7"); entry.RenderTransform = new TranslateTransform(1, 1); };
            entry.PreviewMouseLeftButtonUp += (_, _) => { entry.Background = Brush("#24466D"); entry.RenderTransform = Transform.Identity; };
            entry.MouseLeave += (_, _) => { if (!toolboxDragging) { entry.Background = Brush("#24466D"); entry.RenderTransform = Transform.Identity; } };
        }
    }

    private void AddBehaviorCoach(DesignItem item)
    {
        var box = new Border { Background = Brush("#203755"), BorderBrush = Brush("#3B82F6"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5), Padding = new Thickness(9), Margin = new Thickness(0, 4, 0, 11) };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = english ? $"What should {item.Name} do?" : $"Ce ar trebui să facă {item.Name}?",
            FontWeight = FontWeights.SemiBold, Foreground = Brush("#E8F1FF"), TextWrapping = TextWrapping.Wrap });
        stack.Children.Add(new TextBlock { Text = english ? "Double click the control to edit its events." : "Dublu clic pe control deschide evenimentele lui.",
            Foreground = Brush("#A9C6EA"), FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 5) });
        var choices = new ComboBox { Margin = new Thickness(0, 3, 0, 3) };
        choices.Items.Add(english ? "Show a message" : "Arată un mesaj");
        choices.Items.Add(english ? "Close this window" : "Închide fereastra");
        choices.Items.Add(english ? "Open another window" : "Deschide altă fereastră");
        choices.Items.Add(english ? "Describe with AI" : "Descrie cu AI");
        choices.SelectedIndex = 0;
        stack.Children.Add(choices);
        var request = new TextBox { Text = english ? "Hello!" : "Salut!", Margin = new Thickness(0, 2, 0, 4), ToolTip = english ? "Message text or describe a custom action" : "Textul mesajului sau descrierea unei acțiuni" };
        stack.Children.Add(request);
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        var create = new Button { Content = english ? "Create action" : "Creează acțiunea", Padding = new Thickness(7, 4, 7, 4) };
        create.Click += (_, _) => { try { ApplyGuidedAction(item, choices.SelectedIndex, request.Text); } catch (Exception ex) { Error(ex); } };
        var edit = new Button { Content = english ? "Edit code" : "Editează codul", Padding = new Thickness(7, 4, 7, 4) };
        edit.Click += (_, _) => { try { OpenComponentEvents(item); } catch (Exception ex) { Error(ex); } };
        actions.Children.Add(create); actions.Children.Add(edit);
        stack.Children.Add(actions);
        box.Child = stack;
        InspectorHost.Children.Add(box);
    }

    private static string PrimaryEvent(string type) => type switch
    {
        "Button" or "ToggleButton" => "Click",
        "CheckBox" or "RadioButton" => "Checked",
        "TextBox" or "RichTextBox" => "TextChanged",
        "ComboBox" or "ListBox" or "ListView" or "DataGrid" or "TabControl" => "SelectionChanged",
        "TreeView" => "SelectedItemChanged",
        "Calendar" => "SelectedDatesChanged",
        "DatePicker" => "SelectedDateChanged",
        "Slider" => "ValueChanged",
        _ => "MouseLeftButtonUp"
    };

    private static string EventArgsType(string eventName) => eventName switch
    {
        "MouseEnter" or "MouseLeave" => "System.Windows.Input.MouseEventArgs",
        "MouseLeftButtonUp" => "System.Windows.Input.MouseButtonEventArgs",
        "TextChanged" => "System.Windows.Controls.TextChangedEventArgs",
        "SelectionChanged" or "SelectedDatesChanged" or "SelectedDateChanged" => "System.Windows.Controls.SelectionChangedEventArgs",
        "SelectedItemChanged" => "System.Windows.RoutedPropertyChangedEventArgs<object>",
        "ValueChanged" => "System.Windows.RoutedPropertyChangedEventArgs<double>",
        _ => "System.Windows.RoutedEventArgs"
    };

    private string EnsureComponentEvents(DesignItem item)
    {
        RequireProject();
        if (state.Language != "C#") throw new InvalidOperationException("Component events are available in the C#/WPF designer.");
        SaveAllEditors();
        var primary = PrimaryEvent(item.Type);
        item.Events ??= new();
        if (item.Events.Count == 0)
        {
            Snapshot();
            item.Events[primary] = item.Name + "_" + primary;
            if (item.Type is "CheckBox" or "RadioButton") item.Events["Unchecked"] = item.Name + "_Unchecked";
            item.Events["MouseEnter"] = item.Name + "_MouseEnter";
            item.Events["MouseLeave"] = item.Name + "_MouseLeave";
        }
        var path = Path.Combine(projectDir!, "App", activeWindow + "." + item.Name + ".Events.cs");
        if (!File.Exists(path))
        {
            var source = new StringBuilder();
            source.AppendLine("// Event handlers for " + item.Name + ". You can edit this file directly.");
            source.AppendLine("namespace " + state.Name + ";").AppendLine();
            source.AppendLine("public partial class " + activeWindow).AppendLine("{");
            foreach (var (eventName, method) in item.Events)
            {
                source.AppendLine($"    private void {method}(object sender, {EventArgsType(eventName)} e)");
                source.AppendLine("    {");
                source.AppendLine("        ForgeWatch.Record(" + JsonSerializer.Serialize(item.Name + "." + eventName) + ", () => \"triggered\", " + JsonSerializer.Serialize(activeWindow + "." + item.Name + ".Events.cs") + ", 0);");
                source.AppendLine("        // Describe this action here, or write C# code.");
                source.AppendLine("    }").AppendLine();
            }
            source.AppendLine("}");
            File.WriteAllText(path, source.ToString());
        }
        GenerateApp(); SaveProject();
        return path;
    }

    private void OpenComponentEvents(DesignItem item)
    {
        var path = EnsureComponentEvents(item);
        OpenEditor(path);
        var handler = item.Events[PrimaryEvent(item.Type)];
        var editor = CurrentEditor!;
        var location = editor.Text.IndexOf("private void " + handler + "(", StringComparison.Ordinal);
        if (location >= 0)
        {
            var body = editor.Text.IndexOf('{', location);
            editor.CaretOffset = Math.Min(editor.Text.Length, body + 2);
            editor.ScrollToLine(editor.Document.GetLineByOffset(body).LineNumber);
            editor.Focus();
        }
    }

    private void ApplyGuidedAction(DesignItem item, int choice, string input)
    {
        if (choice == 3)
        {
            if (Provider == "Fără AI" || string.IsNullOrWhiteSpace(ApiKeyBox.Password))
                throw new InvalidOperationException(english ? "Choose OpenAI or Gemini and add an API key for custom AI actions." : "Alege OpenAI sau Gemini și adaugă o cheie API pentru acțiuni AI personalizate.");
            OpenComponentEvents(item);
            pendingAiTarget = Path.GetRelativePath(projectDir!, CurrentEditorPath()!).Replace('\\', '/');
            PromptBox.Text = $"Modify only handler {item.Events[PrimaryEvent(item.Type)]} in {pendingAiTarget}. Implement this behavior: {input}. Preserve all other handlers and existing code. Return the complete file.";
            SendPrompt_Click(this, new RoutedEventArgs());
            return;
        }
        var path = EnsureComponentEvents(item);
        var primary = item.Events[PrimaryEvent(item.Type)];
        var statement = choice switch
        {
            0 => "System.Windows.MessageBox.Show(" + JsonSerializer.Serialize(string.IsNullOrWhiteSpace(input) ? "Hello!" : input) + ");",
            1 => "Close();",
            2 when state.Windows.Keys.FirstOrDefault(x => x != activeWindow) is string target => "new " + target + "().Show();",
            2 => throw new InvalidOperationException(english ? "Create another window first." : "Creează mai întâi o altă fereastră."),
            _ => throw new InvalidOperationException("Choose an action.")
        };
        SaveAllEditors();
        var source = File.ReadAllText(path);
        var start = source.IndexOf("private void " + primary + "(", StringComparison.Ordinal);
        var marker = "        // Describe this action here, or write C# code.";
        var markerAt = start < 0 ? -1 : source.IndexOf(marker, start, StringComparison.Ordinal);
        if (markerAt < 0) throw new InvalidOperationException(english ? "This handler already contains custom code. Open it to edit safely." : "Acest handler conține deja cod. Deschide-l pentru a-l edita în siguranță.");
        Snapshot();
        source = source.Remove(markerAt, marker.Length).Insert(markerAt, "        " + statement);
        File.WriteAllText(path, source);
        if (openEditors.TryGetValue(path, out var open)) open.Editor.Text = source;
        SaveProject();
        OpenEditor(path);
        Status(english ? "Action created. Run the app with F5." : "Acțiunea a fost creată. Rulează aplicația cu F5.");
    }

    private string? CurrentEditorPath() => openEditors.Values.FirstOrDefault(x => ReferenceEquals(x.Tab, EditorTabs.SelectedItem))?.Path;
}
