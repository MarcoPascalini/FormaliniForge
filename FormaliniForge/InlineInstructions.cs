// Copyright (c) 2026 Marco Irinel Pascalini (https://formalini.com)
// SPDX-License-Identifier: MIT
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;

namespace FormaliniForge;

public partial class MainWindow
{
    private readonly HashSet<TextEditor> inlineRequestsInFlight = new();
    private bool applyingInlineCode;

    private void AttachInlineAssistant(EditorDocument document)
    {
        if (!document.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) return;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1700) };
        timer.Tick += async (_, _) =>
        {
            timer.Stop();
            if (AutoCodeToggle.IsChecked != true || applyingInlineCode || inlineRequestsInFlight.Contains(document.Editor)) return;
            await TryConvertInlineInstruction(document);
        };
        document.Editor.TextChanged += (_, _) =>
        {
            if (applyingInlineCode || AutoCodeToggle.IsChecked != true) return;
            timer.Stop(); timer.Start();
        };
        document.Tab.Unloaded += (_, _) => timer.Stop();
    }

    private static bool IsNaturalInstruction(string line)
    {
        var words = Regex.Matches(line, @"\p{L}[\p{L}\p{N}_-]*");
        if (words.Count < 2 || line.Length < 10 || line.Length > 300) return false;
        if (line.IndexOfAny(new[] { ';', '{', '}', '(', ')', '=', '<', '>', '#', '[', ']', '\\', '/' }) >= 0) return false;
        if (Regex.IsMatch(line, @"^(using|namespace|public|private|protected|internal|static|class|interface|enum|var|int|long|double|string|bool|return|if|for|foreach|while|else|try|catch|throw|await|new|this|base)\b", RegexOptions.IgnoreCase)) return false;
        return true;
    }

    private async Task TryConvertInlineInstruction(EditorDocument document)
    {
        if (projectDir == null || state.Language != "C#" || !openEditors.ContainsKey(document.Path)) return;
        var editor = document.Editor;
        var line = editor.Document.GetLineByOffset(editor.CaretOffset);
        var sourceLine = editor.Document.GetText(line);
        var indent = Regex.Match(sourceLine, @"^\s*").Value;
        var instruction = sourceLine.Trim();
        if (indent.Length < 8 || !IsNaturalInstruction(instruction)) return;
        inlineRequestsInFlight.Add(editor);
        try
        {
            var generated = TryOfflineInstruction(instruction);
            if (generated == null)
            {
                if (Provider == "Fără AI" || ApiKeyBox.Password.Trim().Length == 0)
                {
                    Status(english ? "Add an AI key to convert this request, or use a quick action in Properties." : "Adaugă o cheie AI pentru această cerere sau folosește o acțiune rapidă din Proprietăți.");
                    return;
                }
                Status(english ? "Turning your request into code..." : "Transform cererea în cod...");
                generated = await RequestInlineCode(instruction, editor.Text, line.LineNumber);
            }
            if (!openEditors.ContainsKey(document.Path) || line.LineNumber > editor.Document.LineCount) return;
            var currentLine = editor.Document.GetLineByNumber(line.LineNumber);
            if (editor.Document.GetText(currentLine) != sourceLine) return;
            var clean = generated.Trim().Trim('`').Trim();
            if (clean.StartsWith("csharp", StringComparison.OrdinalIgnoreCase)) clean = clean[6..].Trim();
            if (clean.Length == 0) return;
            var replacement = string.Join(Environment.NewLine, clean.Replace("\r\n", "\n").Split('\n').Select(x => indent + x.TrimEnd()));
            Snapshot();
            applyingInlineCode = true;
            editor.Document.Replace(currentLine.Offset, currentLine.Length, replacement);
            SaveAllEditors();
            Status(english ? "Instruction replaced with code. Undo is available in History." : "Instrucțiunea a fost înlocuită cu cod. Poți reveni din Istoric.");
        }
        catch (Exception ex) { Status(ex.Message); }
        finally { applyingInlineCode = false; inlineRequestsInFlight.Remove(editor); }
    }

    private static string? TryOfflineInstruction(string instruction)
    {
        if (Regex.IsMatch(instruction, @"^(închide|inchide|close)\s+(fereastra|window)(\s+.*)?$", RegexOptions.IgnoreCase)) return "Close();";
        var message = Regex.Match(instruction, "[\"“]([^\"”]+)[\"”]");
        if (message.Success && Regex.IsMatch(instruction, @"^(afișează|afiseaza|arată|arata|show|display)\b", RegexOptions.IgnoreCase))
            return "System.Windows.MessageBox.Show(" + JsonSerializer.Serialize(message.Groups[1].Value) + ");";
        return null;
    }

    private async Task<string> RequestInlineCode(string instruction, string source, int line)
    {
        var prompt = "Replace only the natural-language instruction on line " + line + " with executable C# statements inside the existing method. Return ONLY the statements, without markdown or explanation. Do not invent unavailable variables. Instruction: " + instruction + "\n\nCurrent file:\n" + (source.Length > 15000 ? source[..15000] : source);
        var model = ModelBox.Text.Trim();
        if (model.Length == 0) throw new InvalidOperationException("Choose an AI model first.");
        if (Provider == "OpenAI")
        {
            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://api.openai.com/v1/responses");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKeyBox.Password.Trim());
            request.Content = new System.Net.Http.StringContent(JsonSerializer.Serialize(new { model, instructions = "You write safe, concise C# statements for a WPF event handler. Return only code.", input = prompt, store = false }), Encoding.UTF8, "application/json");
            using var response = await http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException("OpenAI: " + body);
            using var json = JsonDocument.Parse(body);
            return string.Concat(json.RootElement.GetProperty("output").EnumerateArray()
                .Where(x => x.TryGetProperty("type", out var type) && type.GetString() == "message")
                .SelectMany(x => x.GetProperty("content").EnumerateArray())
                .Where(x => x.TryGetProperty("type", out var type) && type.GetString() == "output_text")
                .Select(x => x.GetProperty("text").GetString()));
        }
        else
        {
            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post,
                "https://generativelanguage.googleapis.com/v1beta/models/" + Uri.EscapeDataString(model) + ":generateContent");
            request.Headers.Add("x-goog-api-key", ApiKeyBox.Password.Trim());
            request.Content = new System.Net.Http.StringContent(JsonSerializer.Serialize(new
            {
                system_instruction = new { parts = new[] { new { text = "Return only C# statements, no markdown." } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } }
            }), Encoding.UTF8, "application/json");
            using var response = await http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Gemini: " + body);
            using var json = JsonDocument.Parse(body);
            return json.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
        }
    }
}
