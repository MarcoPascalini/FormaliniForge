// Copyright (c) 2026 Marco Irinel Pascalini (https://formalini.com)
// SPDX-License-Identifier: MIT
using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace FormaliniForge;

public partial class MainWindow
{
    private void FocusFind_Click(object sender, RoutedEventArgs e)
    {
        MainTabs.SelectedIndex = 1;
        FindBox.Focus();
        FindBox.SelectAll();
    }

    private void FocusReplace_Click(object sender, RoutedEventArgs e)
    {
        MainTabs.SelectedIndex = 1;
        ReplaceBox.Focus();
        ReplaceBox.SelectAll();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            if (HandleDesignerShortcut(e)) { e.Handled = true; return; }
        }
        catch (Exception ex) { Error(ex); e.Handled = true; return; }
        var control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        if (control && !shift && e.Key == Key.N) NewProject_Click(sender, new RoutedEventArgs());
        else if (control && !shift && e.Key == Key.O) OpenProject_Click(sender, new RoutedEventArgs());
        else if (control && !shift && e.Key == Key.S) Save_Click(sender, new RoutedEventArgs());
        else if (control && !shift && e.Key == Key.F) FocusFind_Click(sender, new RoutedEventArgs());
        else if (control && !shift && e.Key == Key.H) FocusReplace_Click(sender, new RoutedEventArgs());
        else if (control && shift && e.Key == Key.F) SearchProject_Click(sender, new RoutedEventArgs());
        else if (control && shift && e.Key == Key.H) History_Click(sender, new RoutedEventArgs());
        else if (control && shift && e.Key == Key.N) NewFile_Click(sender, new RoutedEventArgs());
        else if (control && shift && e.Key == Key.P) CommandPalette_Click(sender, new RoutedEventArgs());
        else if (!control && (Keyboard.Modifiers & ModifierKeys.Alt) != 0 && shift && e.Key == Key.F) FormatDocument_Click(sender, new RoutedEventArgs());
        else if (!control && e.Key == Key.F5) Run_Click(sender, new RoutedEventArgs());
        else if (!control && e.Key == Key.F6) Build_Click(sender, new RoutedEventArgs());
        else if (!control && e.Key == Key.F7) Debug_Click(sender, new RoutedEventArgs());
        else return;
        e.Handled = true;
    }
}
