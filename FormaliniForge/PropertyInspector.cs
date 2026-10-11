// Copyright (c) 2026 Marco Irinel Pascalini (https://formalini.com)
// SPDX-License-Identifier: MIT
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;

namespace FormaliniForge;

public partial class MainWindow
{
    private readonly Dictionary<string, FrameworkElement> inspectorFields = new();

    private FormSettings CurrentForm
    {
        get
        {
            state.Forms ??= new();
            if (!state.Forms.TryGetValue(activeWindow, out var settings))
                state.Forms[activeWindow] = settings = new FormSettings { Title = activeWindow };
            return settings;
        }
    }

    private void InitializeInspector() => RefreshInspector();

    private void DesignCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource != DesignCanvas) return;
        selected = null;
        UpdateSelectionAdorner();
        RefreshInspector();
    }

    private void RefreshInspector()
    {
        if (InspectorHost == null) return;
        inspectorFields.Clear();
        InspectorHost.Children.Clear();
        if (projectDir == null)
        {
            AddInspectorHint("Deschide un proiect, apoi alege formularul sau un control.");
            LocalizeTree(InspectorHost);
            return;
        }
        if (selected == null) ShowFormInspector();
        else ShowControlInspector(selected);
        var apply = new Button { Content = "Aplică proprietățile", Margin = new Thickness(0, 12, 0, 4), Padding = new Thickness(10, 8, 10, 8) };
        apply.Click += ApplyProperties_Click;
        InspectorHost.Children.Add(apply);
        if (selected != null)
        {
            var delete = new Button { Content = "Șterge componenta", Margin = new Thickness(0, 0, 0, 8) };
            delete.Click += DeleteComponent_Click;
            InspectorHost.Children.Add(delete);
        }
        LocalizeTree(InspectorHost);
    }

    private void ShowFormInspector()
    {
        var form = CurrentForm;
        AddInspectorHeading("FEREASTRĂ · " + activeWindow);
        AddInspectorHint("Clic pe o componentă pentru proprietățile ei. Clic pe fundal pentru formular.");
        AddInspectorSection("Identitate și poziționare");
        AddInspectorField("Title", "Titlu", form.Title);
        AddInspectorHint("Icon: " + (string.IsNullOrWhiteSpace(form.IconFile) ? (english ? "default" : "implicită") : form.IconFile));
        var chooseIcon = new Button { Content = T("Alege iconiță (.ico)"), Margin = new Thickness(0, 2, 0, 6) };
        chooseIcon.Click += ChooseFormIcon_Click;
        InspectorHost.Children.Add(chooseIcon);
        AddInspectorField("Width", "Lățime", form.Width.ToString(CultureInfo.CurrentCulture));
        AddInspectorField("Height", "Înălțime", form.Height.ToString(CultureInfo.CurrentCulture));
        AddInspectorField("MinWidth", "Lățime minimă", form.MinWidth.ToString(CultureInfo.CurrentCulture));
        AddInspectorField("MinHeight", "Înălțime minimă", form.MinHeight.ToString(CultureInfo.CurrentCulture));
        AddInspectorField("Left", "Poziție X", form.Left.ToString(CultureInfo.CurrentCulture));
        AddInspectorField("Top", "Poziție Y", form.Top.ToString(CultureInfo.CurrentCulture));
        AddInspectorChoice("WindowStartupLocation", "Poziție la pornire", form.WindowStartupLocation, "Manual", "CenterScreen", "CenterOwner");
        AddInspectorChoice("WindowState", "Stare inițială", form.WindowState, "Normal", "Maximized", "Minimized");
        AddInspectorChoice("ResizeMode", "Redimensionare", form.ResizeMode, "CanResize", "CanResizeWithGrip", "CanMinimize", "NoResize");
        AddInspectorChoice("SizeToContent", "Dimensiune după conținut", form.SizeToContent, "Manual", "Width", "Height", "WidthAndHeight");
        AddInspectorChoice("WindowStyle", "Stilul ferestrei", form.WindowStyle, "SingleBorderWindow", "ThreeDBorderWindow", "ToolWindow", "None");
        AddInspectorChoice("Topmost", "Mereu deasupra", form.Topmost.ToString(), "False", "True");
        AddInspectorChoice("ShowInTaskbar", "În bara de activități", form.ShowInTaskbar.ToString(), "True", "False");
        AddInspectorChoice("ShowActivated", "Activează la pornire", form.ShowActivated.ToString(), "True", "False");
        AddInspectorSection("Aspect");
        AddInspectorField("Background", "Fundal", form.Background);
        AddInspectorField("Foreground", "Culoare text", form.Foreground);
        AddInspectorField("FontFamily", "Font", form.FontFamily);
        AddInspectorField("FontSize", "Mărime font", form.FontSize.ToString(CultureInfo.CurrentCulture));
        AddInspectorChoice("FontWeight", "Grosime font", form.FontWeight, "Normal", "SemiBold", "Bold", "Light");
        AddInspectorChoice("FlowDirection", "Direcția conținutului", form.FlowDirection, "LeftToRight", "RightToLeft");
        AddInspectorField("Opacity", "Opacitate (0–1)", form.Opacity.ToString(CultureInfo.CurrentCulture));
    }

    private void ShowControlInspector(DesignItem item)
    {
        AddInspectorHeading(item.Type.ToUpperInvariant() + " · " + item.Name);
        if (state.Language == "C#") AddBehaviorCoach(item);
        else AddInspectorHint(IsWebProject
            ? (english ? "The visual layout updates automatically. Double click to edit the custom screen code." : "Aspectul vizual se actualizează automat. Dublu clic deschide codul personalizat al ecranului.")
            : (english ? "This native Windows control is generated from the form. Double click it to open the generated source."
                : "Controlul Windows nativ este generat din formular. Dublu clic deschide codul generat."));
        AddInspectorSection("Identitate și conținut");
        AddInspectorField("Name", "Nume în cod", item.Name);
        if (item.Type is "Button" or "Label" or "TextBlock" or "TextBox" or "CheckBox" or "RadioButton" or "ToggleButton" or "GroupBox" or "Expander" or "Heading" or "Text" or "Input" or "TextArea" or "Image" or "Select" or "Container" or "Link" or "List" or "Table" or "View" or "TextInput" or "Pressable" or "Switch" or "ScrollView" or "FlatList")
            AddInspectorField("Text", IsWebProject && item.Type == "Image" ? (english ? "Image URL" : "Adresa imaginii") : item.Type is "GroupBox" or "Expander" ? "Antet" : "Text / conținut", item.Text);
        AddInspectorSection("Poziție și dimensiuni");
        AddInspectorField("X", "X", item.X.ToString(CultureInfo.CurrentCulture));
        AddInspectorField("Y", "Y", item.Y.ToString(CultureInfo.CurrentCulture));
        AddInspectorField("Width", "Lățime", item.Width.ToString(CultureInfo.CurrentCulture));
        AddInspectorField("Height", "Înălțime", item.Height.ToString(CultureInfo.CurrentCulture));
        AddInspectorSection("Aspect și comportament");
        AddInspectorField("Background", item.Type is "Rectangle" or "Ellipse" ? "Umplere" : "Fundal", item.Background);
        if (item.Type is not ("Border" or "Rectangle" or "Ellipse"))
        {
            AddInspectorField("Foreground", "Culoare text", item.Foreground);
            AddInspectorField("FontSize", "Mărime font", item.FontSize.ToString(CultureInfo.CurrentCulture));
        }
        AddInspectorField("ToolTip", "Tooltip", item.ToolTip);
        AddOption(item, "Opacity", "Opacitate (0–1)");
        AddOption(item, "IsEnabled", "Activ", "True", "False");
        AddOption(item, "Visibility", "Vizibilitate", "Visible", "Hidden", "Collapsed");
        AddOption(item, "Margin", "Margine (stânga,sus,dreapta,jos)");
        AddOption(item, "HorizontalAlignment", "Aliniere orizontală", "Left", "Center", "Right", "Stretch");
        AddOption(item, "VerticalAlignment", "Aliniere verticală", "Top", "Center", "Bottom", "Stretch");
        if (item.Type is not ("Border" or "Rectangle" or "Ellipse"))
        {
            AddOption(item, "FontFamily", "Font");
            AddOption(item, "FontWeight", "Grosime font", "Normal", "SemiBold", "Bold", "Light");
        }
        if (item.Type is not ("TextBlock" or "Border" or "Rectangle" or "Ellipse"))
            AddOption(item, "Padding", "Spațiere interioară");
        AddInspectorSection("Specific · " + item.Type);
        foreach (var (name, label, choices) in SpecificOptions(item.Type)) AddOption(item, name, label, choices);
        if (item.Type == "DataGrid") AddInspectorHint("Legarea la SQLite se face în panoul Date și setări.");
        var visual = DesignCanvas.Children.OfType<FrameworkElement>().FirstOrDefault(c => ReferenceEquals(c.Tag, item));
        if (visual != null)
        {
            var advanced = TypeDescriptor.GetProperties(visual).Cast<PropertyDescriptor>()
                .Where(p => p.IsBrowsable && !p.IsReadOnly && IsSimpleProperty(p.PropertyType) && p.Converter.CanConvertFrom(typeof(string)))
                .Where(p => p.Name is not ("Name" or "Text" or "Content" or "Header" or "Width" or "Height" or "Background" or "Foreground" or "FontSize" or "ToolTip"))
                .Where(p => !inspectorFields.ContainsKey("Option." + p.Name))
                .OrderBy(p => p.Name).ToList();
            if (advanced.Count > 0) AddInspectorSection("Alte proprietăți editabile");
            foreach (var property in advanced) AddOption(item, property.Name, property.Name);
        }
    }

    private static bool IsSimpleProperty(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(string) || type == typeof(bool) || type == typeof(int) || type == typeof(double) ||
               type == typeof(char) || type == typeof(Thickness) || type == typeof(CornerRadius) ||
               type == typeof(Brush) || type == typeof(System.Windows.Media.Color) || type == typeof(FontFamily) || type.IsEnum;
    }

    private static IEnumerable<(string name, string label, string[] choices)> SpecificOptions(string type)
    {
        static string[] Choices(params string[] values) => values;
        return type switch
        {
            "Button" => new[] { ("IsDefault", "Buton implicit", Choices("False", "True")), ("IsCancel", "Buton de anulare", Choices("False", "True")) },
            "TextBox" => new[] { ("TextWrapping", "Încadrare text", Choices("NoWrap", "Wrap", "WrapWithOverflow")), ("AcceptsReturn", "Acceptă Enter", Choices("False", "True")), ("IsReadOnly", "Doar citire", Choices("False", "True")), ("MaxLength", "Număr maxim de caractere", Choices()) },
            "PasswordBox" => new[] { ("MaxLength", "Număr maxim de caractere", Choices()) },
            "RichTextBox" => new[] { ("IsReadOnly", "Doar citire", Choices("False", "True")), ("AcceptsTab", "Acceptă Tab", Choices("False", "True")) },
            "TextBlock" => new[] { ("TextWrapping", "Încadrare text", Choices("NoWrap", "Wrap", "WrapWithOverflow")), ("TextAlignment", "Aliniere text", Choices("Left", "Center", "Right", "Justify")) },
            "CheckBox" or "RadioButton" or "ToggleButton" => new[] { ("IsChecked", "Selectat", Choices("False", "True")) },
            "ComboBox" => new[] { ("IsEditable", "Permite tastare", Choices("False", "True")), ("IsReadOnly", "Doar citire", Choices("False", "True")) },
            "ListBox" or "ListView" => new[] { ("SelectionMode", "Mod de selecție", Choices("Single", "Multiple", "Extended")) },
            "DataGrid" => new[] { ("IsReadOnly", "Doar citire", Choices("True", "False")), ("CanUserAddRows", "Poate adăuga rânduri", Choices("False", "True")), ("CanUserDeleteRows", "Poate șterge rânduri", Choices("False", "True")), ("AutoGenerateColumns", "Coloane automate", Choices("True", "False")) },
            "DatePicker" => new[] { ("SelectedDateFormat", "Format dată", Choices("Short", "Long")) },
            "Calendar" => new[] { ("SelectionMode", "Mod de selecție", Choices("SingleDate", "SingleRange", "MultipleRange", "None")) },
            "Slider" or "ProgressBar" => new[] { ("Minimum", "Minim", Choices()), ("Maximum", "Maxim", Choices()), ("Value", "Valoare", Choices()), ("Orientation", "Orientare", Choices("Horizontal", "Vertical")) },
            "TabControl" => new[] { ("TabStripPlacement", "Poziția taburilor", Choices("Top", "Bottom", "Left", "Right")) },
            "Expander" => new[] { ("IsExpanded", "Extins", Choices("True", "False")), ("ExpandDirection", "Direcție", Choices("Down", "Up", "Left", "Right")) },
            "Border" => new[] { ("BorderBrush", "Culoare contur", Choices()), ("BorderThickness", "Grosime contur", Choices()), ("CornerRadius", "Rază colțuri", Choices()) },
            "Rectangle" or "Ellipse" => new[] { ("Stroke", "Culoare contur", Choices()), ("StrokeThickness", "Grosime contur", Choices()) },
            _ => Array.Empty<(string, string, string[])>()
        };
    }

    private void AddOption(DesignItem item, string name, string label, params string[] choices)
    {
        var current = item.Options?.GetValueOrDefault(name) ?? "";
        if (choices.Length == 0) AddInspectorField("Option." + name, label, current);
        else AddInspectorChoice("Option." + name, label, current, choices);
    }

    private void AddInspectorHeading(string title) => InspectorHost.Children.Add(new TextBlock { Text = english ? title.Replace("FEREASTRĂ ·", "WINDOW ·") : title, Foreground = Brushes.White, FontSize = 13, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 2, 0, 4) });
    private void AddInspectorHint(string hint) => InspectorHost.Children.Add(new TextBlock { Text = T(hint), Foreground = BrushFrom("#93A9C6"), TextWrapping = TextWrapping.Wrap, FontSize = 11, Margin = new Thickness(0, 0, 0, 8) });
    private void AddInspectorSection(string title) => InspectorHost.Children.Add(new TextBlock { Text = T(title).ToUpperInvariant(), Foreground = BrushFrom("#79ACF4"), FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 11, 0, 3) });
    private static Brush BrushFrom(string value) => (Brush)new BrushConverter().ConvertFromString(value)!;

    private void AddInspectorField(string key, string label, string value)
    {
        InspectorHost.Children.Add(new TextBlock { Text = T(label), Foreground = BrushFrom("#B8C8DC"), Margin = new Thickness(0, 3, 0, 0) });
        var box = new TextBox { Text = value, Margin = new Thickness(0, 2, 0, 4), ToolTip = "Lasă gol pentru valoarea implicită, unde este permis." };
        if (IsColorInspectorKey(key))
        {
            var row = new DockPanel();
            var swatch = new Button { Width = 34, Height = 27, Margin = new Thickness(5, 2, 0, 4), ToolTip = T("Alege culoarea") };
            DockPanel.SetDock(swatch, Dock.Right);
            row.Children.Add(swatch);
            row.Children.Add(box);
            void RefreshSwatch()
            {
                try { swatch.Background = box.Text.Length > 0 ? BrushFrom(box.Text) : Brushes.Transparent; }
                catch { swatch.Background = Brushes.Transparent; }
            }
            box.TextChanged += (_, _) => RefreshSwatch();
            swatch.Click += (_, _) =>
            {
                using var picker = new System.Windows.Forms.ColorDialog { FullOpen = true, AnyColor = true, SolidColorOnly = false };
                if (swatch.Background is SolidColorBrush current)
                    picker.Color = System.Drawing.Color.FromArgb(current.Color.A, current.Color.R, current.Color.G, current.Color.B);
                if (picker.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
                box.Text = $"#{picker.Color.R:X2}{picker.Color.G:X2}{picker.Color.B:X2}";
                ApplyInspector();
            };
            RefreshSwatch();
            InspectorHost.Children.Add(row);
        }
        else InspectorHost.Children.Add(box);
        inspectorFields[key] = box;
    }

    private static bool IsColorInspectorKey(string key) =>
        key.Contains("Background", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("Foreground", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("Brush", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("Color", StringComparison.OrdinalIgnoreCase) ||
        key.EndsWith(".Fill", StringComparison.OrdinalIgnoreCase) ||
        key.EndsWith(".Stroke", StringComparison.OrdinalIgnoreCase);

    private void AddInspectorChoice(string key, string label, string value, params string[] choices)
    {
        InspectorHost.Children.Add(new TextBlock { Text = T(label), Foreground = BrushFrom("#B8C8DC"), Margin = new Thickness(0, 3, 0, 0) });
        var box = new ComboBox { Margin = new Thickness(0, 2, 0, 4) };
        if (key.StartsWith("Option.", StringComparison.Ordinal)) box.Items.Add("");
        foreach (var choice in choices) box.Items.Add(choice);
        box.SelectedItem = box.Items.Cast<string>().FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? box.Items[0];
        InspectorHost.Children.Add(box);
        inspectorFields[key] = box;
    }

    private string InspectorValue(string key) => inspectorFields[key] switch
    {
        TextBox box => box.Text.Trim(),
        ComboBox box => box.SelectedItem?.ToString() ?? "",
        _ => ""
    };

    private static double InspectorNumber(string value, double min, double max, string label)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var result) || !double.IsFinite(result) || result < min || result > max)
            throw new InvalidOperationException(label + " trebuie să fie între " + min + " și " + max + ".");
        return result;
    }

    private static void ValidateBrush(string value)
    {
        if (value.Length > 0) _ = new BrushConverter().ConvertFromString(value);
    }

    private void ApplyInspector()
    {
        try
        {
            RequireProject();
            if (selected == null)
            {
                var title = InspectorValue("Title");
                var width = InspectorNumber(InspectorValue("Width"), 300, 3000, "Lățimea");
                var height = InspectorNumber(InspectorValue("Height"), 200, 2000, "Înălțimea");
                var minWidth = InspectorNumber(InspectorValue("MinWidth"), 0, width, "Lățimea minimă");
                var minHeight = InspectorNumber(InspectorValue("MinHeight"), 0, height, "Înălțimea minimă");
                var left = InspectorNumber(InspectorValue("Left"), -3000, 10000, "Poziția X");
                var top = InspectorNumber(InspectorValue("Top"), -3000, 10000, "Poziția Y");
                var background = InspectorValue("Background");
                if (background.Length == 0) background = "White";
                ValidateBrush(background);
                var foreground = InspectorValue("Foreground");
                if (foreground.Length == 0) foreground = "Black";
                ValidateBrush(foreground);
                var fontSize = InspectorNumber(InspectorValue("FontSize"), 6, 96, "Fontul");
                var opacity = InspectorNumber(InspectorValue("Opacity"), 0.1, 1, "Opacitatea");
                Snapshot();
                var form = CurrentForm;
                form.Title = title;
                form.Width = width; form.Height = height; form.MinWidth = minWidth; form.MinHeight = minHeight;
                form.Left = left; form.Top = top;
                form.Background = background; form.Foreground = foreground;
                form.FontFamily = InspectorValue("FontFamily").Length == 0 ? "Segoe UI" : InspectorValue("FontFamily"); form.FontSize = fontSize;
                form.FontWeight = InspectorValue("FontWeight"); form.FlowDirection = InspectorValue("FlowDirection"); form.Opacity = opacity;
                form.ResizeMode = InspectorValue("ResizeMode"); form.WindowStartupLocation = InspectorValue("WindowStartupLocation");
                form.WindowState = InspectorValue("WindowState"); form.Topmost = bool.Parse(InspectorValue("Topmost"));
                form.ShowInTaskbar = bool.Parse(InspectorValue("ShowInTaskbar"));
                form.ShowActivated = bool.Parse(InspectorValue("ShowActivated"));
                form.SizeToContent = InspectorValue("SizeToContent"); form.WindowStyle = InspectorValue("WindowStyle");
            }
            else
            {
                var name = InspectorValue("Name");
                if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$") || CurrentItems.Any(x => x != selected && x.Name == name))
                    throw new InvalidOperationException("Numele componentei trebuie să fie unic și valid în cod.");
                var x = InspectorNumber(InspectorValue("X"), 0, 3000, "X");
                var y = InspectorNumber(InspectorValue("Y"), 0, 3000, "Y");
                var width = InspectorNumber(InspectorValue("Width"), 20, 3000, "Lățimea");
                var height = InspectorNumber(InspectorValue("Height"), 20, 2000, "Înălțimea");
                var background = InspectorValue("Background");
                ValidateBrush(background);
                var foreground = inspectorFields.ContainsKey("Foreground") ? InspectorValue("Foreground") : "";
                ValidateBrush(foreground);
                var fontSize = inspectorFields.ContainsKey("FontSize") ? InspectorNumber(InspectorValue("FontSize"), 6, 96, "Fontul") : 14;
                var options = new Dictionary<string, string>(selected.Options ?? new());
                var visual = DesignCanvas.Children.OfType<FrameworkElement>().FirstOrDefault(c => ReferenceEquals(c.Tag, selected));
                foreach (var key in inspectorFields.Keys.Where(x => x.StartsWith("Option.", StringComparison.Ordinal)))
                {
                    var option = key[7..];
                    var value = InspectorValue(key);
                    if (value.Length == 0) { options.Remove(option); continue; }
                    if (visual != null)
                    {
                        var descriptor = TypeDescriptor.GetProperties(visual)[option] ?? throw new InvalidOperationException("Proprietate indisponibilă: " + option);
                        _ = descriptor.Converter.ConvertFromInvariantString(value) ?? throw new InvalidOperationException("Valoare invalidă pentru " + option);
                    }
                    options[option] = value;
                }
                Snapshot();
                selected.Name = name;
                if (inspectorFields.ContainsKey("Text")) selected.Text = InspectorValue("Text");
                selected.X = x; selected.Y = y; selected.Width = width; selected.Height = height;
                selected.Background = background; selected.Foreground = foreground; selected.FontSize = fontSize;
                selected.ToolTip = InspectorValue("ToolTip"); selected.Options = options;
            }
            RenderDesigner(); GenerateApp(); SaveProject(); RefreshInspector();
            Status("Proprietățile au fost aplicate.");
        }
        catch (Exception ex) { Error(ex); }
    }
}
