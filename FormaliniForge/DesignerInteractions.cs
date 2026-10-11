using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using Rectangle = System.Windows.Shapes.Rectangle;
using Brushes = System.Windows.Media.Brushes;
using Cursors = System.Windows.Input.Cursors;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using Panel = System.Windows.Controls.Panel;
using PasswordBox = System.Windows.Controls.PasswordBox;
using ComboBox = System.Windows.Controls.ComboBox;

namespace FormaliniForge;

public partial class MainWindow
{
    private void FormResize_DragStarted(object sender, DragStartedEventArgs e)
    {
        if (projectDir == null) return;
        Snapshot();
    }

    private void FormResize_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (projectDir == null) return;
        var form = CurrentForm;
        form.Width = Math.Clamp(form.Width + e.HorizontalChange, Math.Max(300, form.MinWidth), 3000);
        form.Height = Math.Clamp(form.Height + e.VerticalChange, Math.Max(200, form.MinHeight), 2000);
        DesignerFrame.Width = form.Width;
        DesignerFrame.Height = form.Height;
        DesignCanvas.Width = form.Width - 2;
        DesignCanvas.Height = Math.Max(100, form.Height - PreviewChromeRow.Height.Value - 2);
        Status($"Form: {form.Width:0} × {form.Height:0}");
    }

    private void FormResize_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (projectDir == null) return;
        try { GenerateApp(); SaveProject(); RenderDesigner(); RefreshInspector(); }
        catch (Exception ex) { Error(ex); }
    }

    private readonly Dictionary<string, Thumb> resizeHandles = new();
    private Rectangle? selectionOutline;
    private DesignItem? componentClipboard;

    private void UpdateSelectionAdorner()
    {
        if (selectionOutline != null) DesignCanvas.Children.Remove(selectionOutline);
        foreach (var handle in resizeHandles.Values) DesignCanvas.Children.Remove(handle);
        selectionOutline = null;
        resizeHandles.Clear();
        if (projectDir == null || selected == null) return;

        selectionOutline = new Rectangle
        {
            Stroke = Brushes.DodgerBlue, StrokeThickness = 1.5,
            StrokeDashArray = new DoubleCollection { 3, 2 }, Fill = Brushes.Transparent,
            IsHitTestVisible = false
        };
        Panel.SetZIndex(selectionOutline, 1000);
        DesignCanvas.Children.Add(selectionOutline);
        foreach (var position in new[] { "NW", "N", "NE", "W", "E", "SW", "S", "SE" })
        {
            var handle = new Thumb
            {
                Width = 9, Height = 9, Background = Brushes.White,
                BorderBrush = Brushes.DodgerBlue, BorderThickness = new Thickness(1.5),
                Cursor = position is "N" or "S" ? Cursors.SizeNS : position is "E" or "W" ? Cursors.SizeWE : position is "NW" or "SE" ? Cursors.SizeNWSE : Cursors.SizeNESW,
                ToolTip = "Trage pentru redimensionare"
            };
            handle.DragStarted += (_, _) => { try { Snapshot(); } catch (Exception ex) { Error(ex); } };
            handle.DragDelta += (_, e) => ResizeSelected(position, e.HorizontalChange, e.VerticalChange);
            handle.DragCompleted += (_, _) =>
            {
                try { GenerateApp(); SaveProject(); RenderDesigner(); ShowProperties(); }
                catch (Exception ex) { Error(ex); }
            };
            Panel.SetZIndex(handle, 1001);
            resizeHandles[position] = handle;
            DesignCanvas.Children.Add(handle);
        }
        PositionSelectionAdorner();
    }

    private void PositionSelectionAdorner()
    {
        if (selected == null || selectionOutline == null) return;
        var x = selected.X; var y = selected.Y; var width = selected.Width; var height = selected.Height;
        selectionOutline.Width = width + 4;
        selectionOutline.Height = height + 4;
        Canvas.SetLeft(selectionOutline, x - 2);
        Canvas.SetTop(selectionOutline, y - 2);
        foreach (var (position, handle) in resizeHandles)
        {
            var hx = position.Contains('W') ? x : position.Contains('E') ? x + width : x + width / 2;
            var hy = position.Contains('N') ? y : position.Contains('S') ? y + height : y + height / 2;
            Canvas.SetLeft(handle, hx - 4.5);
            Canvas.SetTop(handle, hy - 4.5);
        }
    }

    private void ResizeSelected(string edge, double dx, double dy)
    {
        if (selected == null) return;
        var item = selected;
        if (edge.Contains('W'))
        {
            var right = item.X + item.Width;
            item.X = Math.Clamp(item.X + dx, 0, right - 20);
            item.Width = right - item.X;
        }
        if (edge.Contains('E')) item.Width = Math.Clamp(item.Width + dx, 20, DesignCanvas.Width - item.X);
        if (edge.Contains('N'))
        {
            var bottom = item.Y + item.Height;
            item.Y = Math.Clamp(item.Y + dy, 0, bottom - 20);
            item.Height = bottom - item.Y;
        }
        if (edge.Contains('S')) item.Height = Math.Clamp(item.Height + dy, 20, DesignCanvas.Height - item.Y);
        var visual = DesignCanvas.Children.OfType<FrameworkElement>().FirstOrDefault(x => ReferenceEquals(x.Tag, item));
        if (visual != null)
        {
            visual.Width = item.Width; visual.Height = item.Height;
            Canvas.SetLeft(visual, item.X); Canvas.SetTop(visual, item.Y);
        }
        PositionSelectionAdorner();
        ShowProperties();
    }

    private void Component_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement visual || visual.Tag is not DesignItem item) return;
        selected = item;
        UpdateSelectionAdorner();
        ShowProperties();
        OpenDesignerMenu(visual, true);
        e.Handled = true;
    }

    private void DesignCanvas_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource != DesignCanvas || projectDir == null) return;
        selected = null;
        UpdateSelectionAdorner();
        ShowProperties();
        OpenDesignerMenu(DesignCanvas, false);
        e.Handled = true;
    }

    private void OpenDesignerMenu(FrameworkElement target, bool onComponent)
    {
        var menu = new ContextMenu { PlacementTarget = target };
        void Add(string text, string shortcut, Action action, bool enabled = true)
        {
            var entry = new MenuItem { Header = text, InputGestureText = shortcut, IsEnabled = enabled };
            entry.Click += (_, _) => { try { action(); } catch (Exception ex) { Error(ex); } };
            menu.Items.Add(entry);
        }
        if (onComponent)
        {
            Add("Proprietăți", "", FocusProperties);
            Add(state.Language == "C#" ? "Editează evenimentele" : "Deschide codul generat", "Dublu clic", () => { if (selected != null) { if (state.Language == "C#") OpenComponentEvents(selected); else OpenEditor(NativeUiSource); } });
            menu.Items.Add(new Separator());
            Add("Decupează", "Ctrl+X", CutComponent);
            Add("Copiază", "Ctrl+C", CopyComponent);
            Add("Duplică", "Ctrl+D", DuplicateComponent);
        }
        Add("Lipește", "Ctrl+V", PasteComponent, componentClipboard != null);
        if (onComponent)
        {
            menu.Items.Add(new Separator());
            Add("Adu în față", "", () => ChangeComponentOrder(true));
            Add("Trimite în spate", "", () => ChangeComponentOrder(false));
            Add("Aliniază la grila de 8 px", "", SnapSelectedToGrid);
            Add("Șterge", "Del", DeleteSelectedComponent);
        }
        else Add("Proprietățile formularului", "", FocusProperties);
        menu.IsOpen = true;
    }

    private void FocusProperties()
    {
        ShowProperties();
        if (toolPanes.Count > 3)
        {
            if (toolPanes[3].IsMinimized) ToggleMinimize(toolPanes[3]);
            BringToFront(toolPanes[3]);
        }
        PropertiesPanel.ScrollToTop();
    }

    private static DesignItem CloneComponent(DesignItem item)
        => JsonSerializer.Deserialize<DesignItem>(JsonSerializer.Serialize(item))!;

    private void CopyComponent()
    {
        if (selected == null) return;
        componentClipboard = CloneComponent(selected);
        Status("Componenta a fost copiată. Folosește Ctrl+V pentru lipire.");
    }

    private void CutComponent()
    {
        CopyComponent();
        DeleteSelectedComponent();
    }

    private void PasteComponent()
    {
        RequireProject();
        if (componentClipboard == null) return;
        Snapshot();
        var item = CloneComponent(componentClipboard);
        var stem = Regex.Replace(item.Name, @"\d+$", "");
        if (stem.Length == 0) stem = item.Type.ToLowerInvariant();
        var next = 1;
        while (CurrentItems.Any(x => x.Name.Equals(stem + next, StringComparison.OrdinalIgnoreCase))) next++;
        item.Name = stem + next;
        item.Events = new();
        item.X = Math.Clamp(item.X + 18, 0, Math.Max(0, DesignCanvas.Width - item.Width));
        item.Y = Math.Clamp(item.Y + 18, 0, Math.Max(0, DesignCanvas.Height - item.Height));
        CurrentItems.Add(item);
        selected = item;
        RenderDesigner(); ShowProperties(); GenerateApp(); SaveProject();
        Status("Componenta a fost lipită.");
    }

    private void DuplicateComponent()
    {
        CopyComponent();
        PasteComponent();
    }

    private void DeleteSelectedComponent()
    {
        if (selected == null) return;
        Snapshot();
        CurrentItems.Remove(selected);
        selected = null;
        RenderDesigner(); ShowProperties(); GenerateApp(); SaveProject();
        Status("Componenta a fost ștearsă. O poți recupera din Istoric.");
    }

    private void ChangeComponentOrder(bool front)
    {
        if (selected == null) return;
        Snapshot();
        CurrentItems.Remove(selected);
        if (front) CurrentItems.Add(selected); else CurrentItems.Insert(0, selected);
        RenderDesigner(); GenerateApp(); SaveProject();
    }

    private void SnapSelectedToGrid()
    {
        if (selected == null) return;
        Snapshot();
        selected.X = Math.Round(selected.X / 8) * 8;
        selected.Y = Math.Round(selected.Y / 8) * 8;
        RenderDesigner(); ShowProperties(); GenerateApp(); SaveProject();
    }

    private bool HandleDesignerShortcut(KeyEventArgs e)
    {
        if (MainTabs.SelectedIndex != 0 || projectDir == null) return false;
        if (Keyboard.FocusedElement is System.Windows.Controls.Primitives.TextBoxBase or PasswordBox or ComboBox) return false;
        var control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        if (control && e.Key == Key.V) { PasteComponent(); return true; }
        if (selected == null) return false;
        if (control && e.Key == Key.C) { CopyComponent(); return true; }
        if (control && e.Key == Key.X) { CutComponent(); return true; }
        if (control && e.Key == Key.D) { DuplicateComponent(); return true; }
        if (!control && e.Key == Key.Delete) { DeleteSelectedComponent(); return true; }
        if (!control && e.Key == Key.Escape) { selected = null; UpdateSelectionAdorner(); ShowProperties(); return true; }
        var amount = shift ? 8 : 1;
        double dx = e.Key == Key.Left ? -amount : e.Key == Key.Right ? amount : 0;
        double dy = e.Key == Key.Up ? -amount : e.Key == Key.Down ? amount : 0;
        if (!control && (dx != 0 || dy != 0))
        {
            Snapshot();
            if (shift)
            {
                selected.Width = Math.Clamp(selected.Width + dx, 20, DesignCanvas.Width - selected.X);
                selected.Height = Math.Clamp(selected.Height + dy, 20, DesignCanvas.Height - selected.Y);
            }
            else
            {
                selected.X = Math.Clamp(selected.X + dx, 0, Math.Max(0, DesignCanvas.Width - selected.Width));
                selected.Y = Math.Clamp(selected.Y + dy, 0, Math.Max(0, DesignCanvas.Height - selected.Height));
            }
            RenderDesigner(); ShowProperties(); GenerateApp(); SaveProject();
            return true;
        }
        return false;
    }
}
