// Copyright (c) 2026 Marco Irinel Pascalini (https://formalini.com)
// SPDX-License-Identifier: MIT
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FormaliniForge;

public sealed class ForgeExtensionManifest
{
    public int ApiVersion { get; set; }
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Publisher { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public List<ForgeExtensionCommand> Commands { get; set; } = new();
    public List<ForgeExtensionSnippet> Snippets { get; set; } = new();
}

public sealed class ForgeExtensionCommand
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string InsertText { get; set; } = "";
}

public sealed class ForgeExtensionSnippet
{
    public string Trigger { get; set; } = "";
    public string Description { get; set; } = "";
    public string Body { get; set; } = "";
    public List<string> Extensions { get; set; } = new();
}

public sealed class InstalledForgeExtension
{
    public required ForgeExtensionManifest Manifest { get; init; }
    public required string DirectoryPath { get; init; }
    public bool Enabled { get; set; }
}

public sealed class ExtensionCatalog
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    public string Root { get; }
    public List<InstalledForgeExtension> Items { get; } = new();

    public ExtensionCatalog(string root) => Root = Path.GetFullPath(root);

    public void Reload()
    {
        Directory.CreateDirectory(Root);
        Items.Clear();
        foreach (var folder in Directory.EnumerateDirectories(Root))
        {
            var manifestPath = Path.Combine(folder, "manifest.json");
            if (!File.Exists(manifestPath)) continue;
            try
            {
                var manifest = ReadManifest(manifestPath);
                if (!string.Equals(manifest.Id, Path.GetFileName(folder), StringComparison.OrdinalIgnoreCase)) continue;
                Items.Add(new InstalledForgeExtension { Manifest = manifest, DirectoryPath = folder,
                    Enabled = File.Exists(Path.Combine(folder, "enabled.flag")) });
            }
            catch { /* An invalid package is ignored, and cannot contribute commands. */ }
        }
    }

    public InstalledForgeExtension Install(string zipPath)
    {
        Directory.CreateDirectory(Root);
        using var archive = ZipFile.OpenRead(zipPath);
        if (archive.Entries.Count is < 1 or > 256) throw new InvalidDataException("The extension archive has too many files.");
        var manifestEntry = archive.GetEntry("manifest.json") ?? throw new InvalidDataException("manifest.json is missing from the archive root.");
        if (manifestEntry.Length > 128_000) throw new InvalidDataException("The manifest is too large.");
        ForgeExtensionManifest manifest;
        using (var stream = manifestEntry.Open()) manifest = Validate(JsonSerializer.Deserialize<ForgeExtensionManifest>(stream, Json));
        var destination = Path.Combine(Root, manifest.Id);
        if (Directory.Exists(destination)) throw new IOException("The extension is already installed. Remove it before replacing it.");
        var stage = Path.Combine(Root, ".install-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        try
        {
            long total = 0;
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in archive.Entries)
            {
                total += entry.Length;
                if (entry.Length > 4_000_000 || total > 8_000_000) throw new InvalidDataException("The extension exceeds the 8 MB limit.");
                var name = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                if (name.Length == 0 || Path.IsPathRooted(name)) throw new InvalidDataException("Invalid path in the archive.");
                var target = Path.GetFullPath(Path.Combine(stage, name));
                if (!target.StartsWith(stage + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The archive contains a path outside the extension folder.");
                if (!paths.Add(target)) throw new InvalidDataException("The archive contains duplicate paths.");
                if (name.EndsWith(Path.DirectorySeparatorChar)) { Directory.CreateDirectory(target); continue; }
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                entry.ExtractToFile(target);
            }
            var extracted = ReadManifest(Path.Combine(stage, "manifest.json"));
            if (extracted.Id != manifest.Id) throw new InvalidDataException("The installed manifest does not match the verified manifest.");
            // Explicit enablement happens in the UI, after inspection of the manifest.
            Directory.Move(stage, destination);
        }
        catch { if (Directory.Exists(stage)) Directory.Delete(stage, true); throw; }
        Reload();
        return Items.Single(x => x.Manifest.Id == manifest.Id);
    }

    public void SetEnabled(InstalledForgeExtension extension, bool enabled)
    {
        var marker = Path.Combine(extension.DirectoryPath, "enabled.flag");
        if (enabled) File.WriteAllText(marker, "enabled"); else if (File.Exists(marker)) File.Delete(marker);
        extension.Enabled = enabled;
    }

    public void Remove(InstalledForgeExtension extension)
    {
        if (!Path.GetFullPath(extension.DirectoryPath).StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The extension path is outside the catalog.");
        Directory.Delete(extension.DirectoryPath, true);
        Reload();
    }

    private static ForgeExtensionManifest ReadManifest(string path)
    {
        if (new FileInfo(path).Length > 128_000) throw new InvalidDataException("The manifest is too large.");
        return Validate(JsonSerializer.Deserialize<ForgeExtensionManifest>(File.ReadAllText(path), Json));
    }

    private static ForgeExtensionManifest Validate(ForgeExtensionManifest? manifest)
    {
        if (manifest == null || manifest.ApiVersion != 1 || !Regex.IsMatch(manifest.Id, "^[a-z][a-z0-9.-]{2,79}$") ||
            string.IsNullOrWhiteSpace(manifest.Name) || manifest.Name.Length > 100 || manifest.Commands.Count > 100 || manifest.Snippets.Count > 200)
            throw new InvalidDataException("The extension manifest is invalid or its API version is unsupported.");
        if (manifest.Commands.Any(x => string.IsNullOrWhiteSpace(x.Title) || x.Title.Length > 120 || x.InsertText.Length > 100_000) ||
            manifest.Snippets.Any(x => !Regex.IsMatch(x.Trigger, "^[A-Za-z_][A-Za-z0-9_]{0,39}$") || x.Body.Length > 100_000))
            throw new InvalidDataException("The extension commands or snippets are invalid.");
        return manifest;
    }
}
