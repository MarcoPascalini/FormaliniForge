// Copyright (c) 2026 Marco Irinel Pascalini (https://formalini.com)
// SPDX-License-Identifier: MIT
using System.IO;
using System.Text.Json;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace FormaliniForge;

public sealed class ProviderUsage
{
    public long Requests { get; set; }
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long TokenBudget { get; set; }
}

public partial class MainWindow
{
    private string UsagePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FormaliniForge", "usage.json");
    private Dictionary<string, ProviderUsage> usageLedger = new(StringComparer.OrdinalIgnoreCase);

    private void LoadUsage()
    {
        try { if (File.Exists(UsagePath)) usageLedger = JsonSerializer.Deserialize<Dictionary<string, ProviderUsage>>(File.ReadAllText(UsagePath)) ?? new(); }
        catch { usageLedger = new(); }
        RefreshCreditBalance();
    }

    private ProviderUsage CurrentUsage()
    {
        if (!usageLedger.TryGetValue(Provider, out var usage)) usageLedger[Provider] = usage = new();
        return usage;
    }

    private void RefreshCreditBalance()
    {
        if (CreditBalanceText == null || CreditBudgetBox == null) return;
        var usage = CurrentUsage();
        var consumed = usage.InputTokens + usage.OutputTokens;
        CreditBalanceText.Text = english
            ? $"{Provider}: {usage.Requests:N0} requests · {consumed:N0} tokens (↑{usage.InputTokens:N0} ↓{usage.OutputTokens:N0})" +
                (usage.TokenBudget > 0 ? $" · budget left: {Math.Max(0, usage.TokenBudget - consumed):N0}" : " · real balance unavailable")
            : $"{Provider}: {usage.Requests:N0} cereri · {consumed:N0} tokenuri (↑{usage.InputTokens:N0} ↓{usage.OutputTokens:N0})" +
                (usage.TokenBudget > 0 ? $" · rămași din buget: {Math.Max(0, usage.TokenBudget - consumed):N0}" : " · sold real indisponibil");
        CreditBudgetBox.Text = usage.TokenBudget == 0 ? "" : usage.TokenBudget.ToString();
    }

    private void CreditBudget_LostFocus(object sender, RoutedEventArgs e)
    {
        var value = CreditBudgetBox.Text.Trim();
        if (value.Length == 0) CurrentUsage().TokenBudget = 0;
        else if (long.TryParse(value, out var budget) && budget >= 0) CurrentUsage().TokenBudget = budget;
        else { MessageBox.Show(english ? "Enter a whole number of tokens." : "Introdu un număr întreg de tokenuri.", "Formalini Forge"); RefreshCreditBalance(); return; }
        SaveUsage(); RefreshCreditBalance();
    }

    private void RecordUsage(JsonElement response, bool openAi)
    {
        response.TryGetProperty("usage", out var data);
        if (data.ValueKind != JsonValueKind.Object) data = default;
        long Number(string name) => data.ValueKind == JsonValueKind.Object && data.TryGetProperty(name, out var value) && value.TryGetInt64(out var number) ? number : 0;
        var input = Number(openAi ? "input_tokens" : "promptTokenCount");
        var output = Number(openAi ? "output_tokens" : "candidatesTokenCount");
        var current = CurrentUsage();
        current.Requests++;
        current.InputTokens += input;
        current.OutputTokens += output;
        SaveUsage(); RefreshCreditBalance();
    }

    private void SaveUsage()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(UsagePath)!);
        File.WriteAllText(UsagePath, JsonSerializer.Serialize(usageLedger, new JsonSerializerOptions { WriteIndented = true }));
    }
}
