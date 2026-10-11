// Copyright (c) 2026 Marco Irinel Pascalini (https://formalini.com)
// SPDX-License-Identifier: MIT
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace FormaliniForge;

public partial class MainWindow
{
    private bool closingAfterSave;
    private bool checkingClose;

    private string[] UnsavedFileNames() => openEditors.Values
        .Where(document => document.Dirty)
        .Select(document => projectDir == null ? Path.GetFileName(document.Path) : Path.GetRelativePath(projectDir, document.Path))
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (closingAfterSave) return;
        if (!openEditors.Values.Any(document => document.Dirty || document.HtmlVisual?.Ready == true)) return;
        e.Cancel = true;
        if (checkingClose) return;
        checkingClose = true;
        try
        {
            foreach (var document in openEditors.Values.ToArray())
                await SynchronizeHtmlVisualBeforeCloseAsync(document);

            var unsaved = UnsavedFileNames();
            if (unsaved.Length > 0)
            {
                var list = string.Join("\n", unsaved.Take(8).Select(name => "• " + name));
                if (unsaved.Length > 8) list += "\n…";
                var message = english
                    ? "Save these files before closing Formalini Forge?\n\n" + list + "\n\nChoose No to return to the editor."
                    : "Save these files before closing Formalini Forge?\n\n" + list + "\n\nChoose No to return to the editor.";
                if (System.Windows.MessageBox.Show(this, message, T("Fișiere nesalvate"), MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes) != MessageBoxResult.Yes)
                    return;
                SaveProject();
                if (UnsavedFileNames().Length > 0) return;
            }
            closingAfterSave = true;
            _ = Dispatcher.BeginInvoke(new Action(Close), DispatcherPriority.Background);
        }
        catch (Exception ex) { Error(ex); }
        finally { checkingClose = false; }
    }
}
