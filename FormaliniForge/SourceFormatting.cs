// Copyright (c) 2026 Marco Irinel Pascalini (https://formalini.com)
// SPDX-License-Identifier: MIT
using System.Text;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FormaliniForge;

public partial class MainWindow
{
    private static string FormatReadableSource(string path, string source)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        try
        {
            return ext switch
            {
                ".html" or ".htm" => FormatHtmlSource(source),
                ".json" => JsonSerializer.Serialize(JsonDocument.Parse(source).RootElement, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine,
                ".xml" or ".xaml" or ".csproj" or ".vbproj" => XDocument.Parse(source, LoadOptions.PreserveWhitespace).ToString() + Environment.NewLine,
                _ => source
            };
        }
        catch { return source; }
    }

    private static string FormatHtmlSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return source;
        var newline = source.Contains("\r\n") ? "\r\n" : "\n";
        var tokens = Regex.Matches(source,
            @"<script\b[^>]*>[\s\S]*?</script\s*>|<style\b[^>]*>[\s\S]*?</style\s*>|<pre\b[^>]*>[\s\S]*?</pre\s*>|<textarea\b[^>]*>[\s\S]*?</textarea\s*>|<!--([\s\S]*?)-->|<[^>]+>|[^<]+",
            RegexOptions.IgnoreCase);
        var blockTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "html", "head", "body", "main", "section", "article", "header", "footer", "nav", "div", "form", "ul", "ol", "li", "table", "thead", "tbody", "tfoot", "tr", "p", "h1", "h2", "h3", "h4", "h5", "h6", "pre", "blockquote" };
        var voidTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr" };
        var output = new StringBuilder();
        var line = new StringBuilder();
        var level = 0;
        void Flush()
        {
            if (line.Length == 0) return;
            output.Append(' ', Math.Max(0, level) * 2).Append(line.ToString().Trim()).Append(newline);
            line.Clear();
        }
        foreach (Match match in tokens)
        {
            var token = match.Value;
            if (token.Length == 0) continue;
            if (token.StartsWith("<pre", StringComparison.OrdinalIgnoreCase) || token.StartsWith("<textarea", StringComparison.OrdinalIgnoreCase))
            {
                Flush(); line.Append(token); Flush(); continue;
            }
            var tagMatch = Regex.Match(token, @"^<\s*(/?)\s*([A-Za-z][A-Za-z0-9-]*)", RegexOptions.IgnoreCase);
            if (tagMatch.Success)
            {
                var name = tagMatch.Groups[2].Value;
                var closing = tagMatch.Groups[1].Length > 0;
                var block = blockTags.Contains(name);
                if (block)
                {
                    Flush();
                    if (closing) level = Math.Max(0, level - 1);
                    line.Append(token);
                    Flush();
                    if (!closing && !voidTags.Contains(name) && !token.EndsWith("/>", StringComparison.Ordinal)) level++;
                    continue;
                }
            }
            if (token.StartsWith("<!", StringComparison.Ordinal) || token.StartsWith("<script", StringComparison.OrdinalIgnoreCase) || token.StartsWith("<style", StringComparison.OrdinalIgnoreCase) || token.StartsWith("<pre", StringComparison.OrdinalIgnoreCase) || token.StartsWith("<textarea", StringComparison.OrdinalIgnoreCase))
            {
                Flush(); line.Append(token); Flush(); continue;
            }
            if (token[0] != '<' && string.IsNullOrWhiteSpace(token) && line.Length == 0) continue;
            line.Append(token);
            if (tagMatch.Success && (voidTags.Contains(tagMatch.Groups[2].Value) || token.EndsWith("/>", StringComparison.Ordinal)) && line.ToString().TrimStart().StartsWith('<')) Flush();
        }
        Flush();
        return output.ToString();
    }
}
