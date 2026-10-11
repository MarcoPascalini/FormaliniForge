// Copyright (c) 2026 Marco Irinel Pascalini (https://formalini.com)
// SPDX-License-Identifier: MIT
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Button = System.Windows.Controls.Button;
using Panel = System.Windows.Controls.Panel;
using Cursors = System.Windows.Input.Cursors;
using Orientation = System.Windows.Controls.Orientation;
using Point = System.Windows.Point;

namespace FormaliniForge;

public partial class MainWindow
{
    private sealed class ToolPane
    {
        public required Border Frame { get; init; }
        public required FrameworkElement Body { get; init; }
        public required Button MinimizeButton { get; init; }
        public required Button MaximizeButton { get; init; }
        public required TextBlock Caption { get; init; }
        public required string Title { get; init; }
        public string DockSide { get; set; } = "Floating";
        public double RestoreX { get; set; }
        public double RestoreY { get; set; }
        public double RestoreWidth { get; set; }
        public double RestoreHeight { get; set; }
        public bool IsMinimized { get; set; }
        public bool IsMaximized { get; set; }
    }

    private readonly List<ToolPane> toolPanes = new();
    private int topToolZ = 10;

    private void InitializeToolWindows()
    {
        if (toolPanes.Count != 0) return;
        var width = Math.Max(WorkspaceGrid.ActualWidth, 1100);
        var height = Math.Max(WorkspaceGrid.ActualHeight, 600);
        AddToolPane("Explorator proiect", ExplorerPanel, 8, 8, 245, Math.Max(280, height - 305));
        AddToolPane("Componente", ToolboxPanel, 8, Math.Max(296, height - 287), 245, 278);
        var rightX = width - 336;
        AddToolPane("Asistent AI", AssistantPanel, rightX, 8, 328, Math.Max(250, height - 455));
        AddToolPane("Proprietăți", PropertiesPanel, rightX, Math.Max(266, height - 438), 328, 198);
        AddToolPane("Date și setări", DataPanel, rightX, Math.Max(469, height - 235), 328, 227);
        LeftToolHost.Visibility = Visibility.Collapsed;
        LeftToolSplitter.Visibility = Visibility.Collapsed;
        RightToolHost.Visibility = Visibility.Collapsed;
        RightToolSplitter.Visibility = Visibility.Collapsed;
        for (var i = 0; i < toolPanes.Count; i++) toolPanes[i].DockSide = i < 2 ? "Left" : "Right";
        ToolCanvas.SizeChanged += (_, _) => { LayoutDockedPanes(); KeepToolPanesVisible(); };
        LayoutDockedPanes();
    }

    private void AddToolPane(string title, FrameworkElement body, double x, double y, double width, double height)
    {
        if (body.Parent is Panel parent) parent.Children.Remove(body);
        RemoveDuplicateHeading(body, title);
        body.Margin = new Thickness(0);
        var frame = new Border
        {
            Width = width, Height = height, MinWidth = 205, MinHeight = 34,
            Background = Brush("#172033"), BorderBrush = Brush("#4A6080"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5), SnapsToDevicePixels = true
        };
        var panel = new Grid();
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var header = new Grid { Background = Brush("#253B59"), Cursor = Cursors.SizeAll };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var caption = new TextBlock
        {
            Text = title.ToUpperInvariant(), Foreground = Brush("#E6F0FC"), FontWeight = FontWeights.SemiBold,
            FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0)
        };
        header.Children.Add(caption);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        var minimize = ToolButton("—", "Minimizează panoul");
        var maximize = ToolButton("□", "Maximizează panoul");
        buttons.Children.Add(minimize);
        buttons.Children.Add(maximize);
        Grid.SetColumn(buttons, 1);
        header.Children.Add(buttons);
        panel.Children.Add(header);
        Grid.SetRow(body, 1);
        panel.Children.Add(body);
        var resize = new Thumb
        {
            Width = 13, Height = 13, HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom, Cursor = Cursors.SizeNWSE,
            Background = Brush("#6982A5"), Opacity = 0.65
        };
        Grid.SetRow(resize, 1);
        panel.Children.Add(resize);
        frame.Child = panel;
        frame.PreviewMouseWheel += (_, e) =>
        {
            var viewer = FindScrollViewer(e.OriginalSource as DependencyObject) ?? FindScrollViewer(body);
            if (viewer == null || viewer.ScrollableHeight <= 0) return;
            viewer.ScrollToVerticalOffset(Math.Clamp(viewer.VerticalOffset - e.Delta / 3.0, 0, viewer.ScrollableHeight));
            e.Handled = true;
        };
        var pane = new ToolPane
        {
            Frame = frame, Body = body, MinimizeButton = minimize, MaximizeButton = maximize, Caption = caption, Title = title,
            RestoreX = x, RestoreY = y, RestoreWidth = width, RestoreHeight = height
        };
        toolPanes.Add(pane);
        Canvas.SetLeft(frame, x);
        Canvas.SetTop(frame, y);
        Panel.SetZIndex(frame, ++topToolZ);
        ToolCanvas.Children.Add(frame);

        Point mouseOrigin = default;
        double leftOrigin = 0, topOrigin = 0;
        header.MouseLeftButtonDown += (_, e) =>
        {
            if (e.OriginalSource is Button || IsInsideButton(e.OriginalSource as DependencyObject)) return;
            if (pane.IsMaximized) return;
            BringToFront(pane);
            if (pane.DockSide != "Floating") { pane.DockSide = "Floating"; LayoutDockedPanes(); }
            mouseOrigin = e.GetPosition(ToolCanvas);
            leftOrigin = Canvas.GetLeft(frame);
            topOrigin = Canvas.GetTop(frame);
            header.CaptureMouse();
            e.Handled = true;
        };
        header.MouseMove += (_, e) =>
        {
            if (!header.IsMouseCaptured) return;
            var now = e.GetPosition(ToolCanvas);
            Canvas.SetLeft(frame, Math.Clamp(leftOrigin + now.X - mouseOrigin.X, 0, Math.Max(0, ToolCanvas.ActualWidth - 80)));
            Canvas.SetTop(frame, Math.Clamp(topOrigin + now.Y - mouseOrigin.Y, 0, Math.Max(0, ToolCanvas.ActualHeight - 32)));
        };
        header.MouseLeftButtonUp += (_, _) =>
        {
            header.ReleaseMouseCapture();
            pane.RestoreX = Canvas.GetLeft(frame);
            pane.RestoreY = Canvas.GetTop(frame);
            if (pane.RestoreX < 34) pane.DockSide = "Left";
            else if (pane.RestoreX + frame.Width > ToolCanvas.ActualWidth - 34) pane.DockSide = "Right";
            LayoutDockedPanes();
        };
        frame.PreviewMouseLeftButtonDown += (_, _) => BringToFront(pane);
        minimize.Click += (_, _) => ToggleMinimize(pane);
        maximize.Click += (_, _) => ToggleMaximize(pane);
        resize.DragDelta += (_, e) =>
        {
            if (pane.IsMaximized || pane.IsMinimized) return;
            if (pane.DockSide != "Floating") { pane.DockSide = "Floating"; LayoutDockedPanes(); }
            frame.Width = Math.Max(205, frame.Width + e.HorizontalChange);
            frame.Height = Math.Max(130, frame.Height + e.VerticalChange);
            pane.RestoreWidth = frame.Width;
            pane.RestoreHeight = frame.Height;
        };
    }

    private static Brush Brush(string color) => (Brush)new BrushConverter().ConvertFromString(color)!;

    private static ScrollViewer? FindScrollViewer(DependencyObject? source)
    {
        for (var node = source; node != null; node = VisualTreeHelper.GetParent(node))
            if (node is ScrollViewer viewer) return viewer;
        if (source == null) return null;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(source); i++)
            if (FindScrollViewer(VisualTreeHelper.GetChild(source, i)) is { } viewer) return viewer;
        return null;
    }

    private static void RemoveDuplicateHeading(FrameworkElement body, string title)
    {
        StackPanel? stack = body switch
        {
            DockPanel dock => dock.Children.OfType<StackPanel>().FirstOrDefault(),
            ScrollViewer scroll => scroll.Content as StackPanel,
            _ => null
        };
        if (stack?.Children.Count > 0 && stack.Children[0] is TextBlock heading &&
            heading.Text.StartsWith(title.Split(' ')[0], StringComparison.OrdinalIgnoreCase))
            stack.Children.RemoveAt(0);
    }

    private static Button ToolButton(string label, string tip) => new()
    {
        Content = label, ToolTip = tip, Width = 27, Height = 25, Margin = new Thickness(1, 2, 2, 2),
        Padding = new Thickness(0), FontSize = 14
    };

    private static bool IsInsideButton(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is Button) return true;
            source = VisualTreeHelper.GetParent(source);
        }
        return false;
    }

    private void BringToFront(ToolPane pane) => Panel.SetZIndex(pane.Frame, ++topToolZ);

    private void ToggleMinimize(ToolPane pane)
    {
        if (pane.IsMaximized) ToggleMaximize(pane);
        pane.IsMinimized = !pane.IsMinimized;
        pane.Body.Visibility = pane.IsMinimized ? Visibility.Collapsed : Visibility.Visible;
        pane.Frame.Height = pane.IsMinimized ? 34 : pane.RestoreHeight;
        pane.MinimizeButton.Content = pane.IsMinimized ? "▣" : "—";
        LayoutDockedPanes();
        BringToFront(pane);
    }

    private void LayoutDockedPanes()
    {
        if (toolPanes.Count == 0 || ToolCanvas.ActualHeight < 100 || ToolCanvas.ActualWidth < 500) return;
        foreach (var side in new[] { "Left", "Right" })
        {
            var panes = toolPanes.Where(p => p.DockSide == side && !p.IsMaximized).ToList();
            if (panes.Count == 0) continue;
            var gap = 6.0;
            var available = Math.Max(100, ToolCanvas.ActualHeight - 12 - gap * (panes.Count - 1));
            var fixedHeight = panes.Count(p => p.IsMinimized) * 34.0;
            var flexible = panes.Count(p => !p.IsMinimized);
            var y = 6.0;
            foreach (var pane in panes)
            {
                var height = pane.IsMinimized ? 34 : Math.Max(100, (available - fixedHeight) / Math.Max(1, flexible));
                pane.Frame.Width = side == "Left" ? 245 : 328;
                pane.Frame.Height = height;
                Canvas.SetLeft(pane.Frame, side == "Left" ? 6 : Math.Max(6, ToolCanvas.ActualWidth - pane.Frame.Width - 6));
                Canvas.SetTop(pane.Frame, y);
                y += height + gap;
            }
        }
    }

    private void ToggleMaximize(ToolPane pane)
    {
        if (!pane.IsMaximized)
        {
            if (pane.IsMinimized) ToggleMinimize(pane);
            pane.RestoreX = Canvas.GetLeft(pane.Frame);
            pane.RestoreY = Canvas.GetTop(pane.Frame);
            pane.RestoreWidth = pane.Frame.Width;
            pane.RestoreHeight = pane.Frame.Height;
            Canvas.SetLeft(pane.Frame, 6);
            Canvas.SetTop(pane.Frame, 6);
            pane.Frame.Width = Math.Max(300, ToolCanvas.ActualWidth - 12);
            pane.Frame.Height = Math.Max(200, ToolCanvas.ActualHeight - 12);
            pane.IsMaximized = true;
            pane.MaximizeButton.Content = "❐";
        }
        else
        {
            Canvas.SetLeft(pane.Frame, pane.RestoreX);
            Canvas.SetTop(pane.Frame, pane.RestoreY);
            pane.Frame.Width = pane.RestoreWidth;
            pane.Frame.Height = pane.RestoreHeight;
            pane.IsMaximized = false;
            pane.MaximizeButton.Content = "□";
        }
        BringToFront(pane);
        LayoutDockedPanes();
    }

    private void KeepToolPanesVisible()
    {
        foreach (var pane in toolPanes)
        {
            if (pane.IsMaximized)
            {
                pane.Frame.Width = Math.Max(300, ToolCanvas.ActualWidth - 12);
                pane.Frame.Height = Math.Max(200, ToolCanvas.ActualHeight - 12);
            }
            else if (pane.DockSide == "Floating")
            {
                Canvas.SetLeft(pane.Frame, Math.Clamp(Canvas.GetLeft(pane.Frame), 0, Math.Max(0, ToolCanvas.ActualWidth - 80)));
                Canvas.SetTop(pane.Frame, Math.Clamp(Canvas.GetTop(pane.Frame), 0, Math.Max(0, ToolCanvas.ActualHeight - 32)));
            }
        }
    }

    private void ResetToolLayout_Click(object sender, RoutedEventArgs e)
    {
        if (toolPanes.Count != 5) return;
        for (var i = 0; i < toolPanes.Count; i++)
        {
            var pane = toolPanes[i];
            if (pane.IsMaximized) ToggleMaximize(pane);
            if (pane.IsMinimized) ToggleMinimize(pane);
            pane.DockSide = i < 2 ? "Left" : "Right";
        }
        LayoutDockedPanes();
        Status("Panourile au revenit la pozițiile inițiale.");
    }

    private void SetToolPaneGeometry(ToolPane pane, double x, double y, double width, double height)
    {
        if (pane.IsMaximized) ToggleMaximize(pane);
        if (pane.IsMinimized) ToggleMinimize(pane);
        Canvas.SetLeft(pane.Frame, x);
        Canvas.SetTop(pane.Frame, y);
        pane.Frame.Width = width;
        pane.Frame.Height = height;
        pane.RestoreX = x;
        pane.RestoreY = y;
        pane.RestoreWidth = width;
        pane.RestoreHeight = height;
    }
}
