using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;

namespace MyLittleCrafter.Tracker;

public class CraftingStats
{

    public Dictionary<string, int> FinishedItemsPerCraft { get; set; } = [];
    public Dictionary<string, int> TotalCurrencyUsed { get; set; } = [];
    public Dictionary<string, double> TotalCostPerCurrency { get; set; } = [];

    public void RecordCraft(CraftInfo craft)
    {
        if (craft == null) return;

        // Update finished items count for this craft type
        string craftName = craft.CraftName;
        FinishedItemsPerCraft[craftName] = FinishedItemsPerCraft.GetValueOrDefault(craftName, 0) + craft.FinishedItemCount;

        // Update total currency used and costs
        foreach (var resource in craft.ResourcesUsed)
        {
            string resourceName = resource.Key;
            int amount = resource.Value;

            // Update currency amount
            TotalCurrencyUsed[resourceName] = TotalCurrencyUsed.GetValueOrDefault(resourceName, 0) + amount;

            // Update currency cost
            double costPerUnit = craft.ResourcesCostPerOneUnit.GetValueOrDefault(resourceName, 0);
            double totalCost = costPerUnit * amount;
            TotalCostPerCurrency[resourceName] = TotalCostPerCurrency.GetValueOrDefault(resourceName, 0) + totalCost;
        }
    }

    public void Reset()
    {
        FinishedItemsPerCraft.Clear();
        TotalCurrencyUsed.Clear();
        TotalCostPerCurrency.Clear();
    }

    public void ToImGUI()
    {
        // Display crafts summary 
        if (FinishedItemsPerCraft.Count > 0 && ImGui.BeginTable($"CraftsTable", 2, ImGuiTableFlags.Sortable | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("Craft", ImGuiTableColumnFlags.DefaultSort, 0.75f);
            ImGui.TableSetupColumn("Items Finished", ImGuiTableColumnFlags.None, 0.25f);
            ImGui.TableHeadersRow();

            if (ImGui.TableGetSortSpecs() is { } sortSpecs)
            {
                // Apply sorting whenever there's an active sort spec
                int sortedColumn = sortSpecs.Specs.ColumnIndex;
                var sortDescending = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Descending;

                // If sorting specs changed, mark as handled
                if (sortSpecs.SpecsDirty)
                {
                    sortSpecs.SpecsDirty = false;
                }

                // Create sorted collection
                var sortedCrafts = sortedColumn switch
                {
                    0 => sortDescending ? FinishedItemsPerCraft.OrderByDescending(x => x.Key) : FinishedItemsPerCraft.OrderBy(x => x.Key),
                    1 => sortDescending ? FinishedItemsPerCraft.OrderByDescending(x => x.Value) : FinishedItemsPerCraft.OrderBy(x => x.Value),
                    _ => FinishedItemsPerCraft.OrderBy(x => x.Key)
                };

                foreach (var craft in sortedCrafts)
                {
                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(craft.Key);

                    ImGui.TableSetColumnIndex(1);
                    ImGuiHelper.RightAlignText($"{craft.Value}");
                }

                // If more than 1 craft, show total amount of items finished
                if (FinishedItemsPerCraft.Count > 1)
                {
                    // Empty row for spacing
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("");

                    // Show total amount of items finished
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Total");
                    ImGui.TableSetColumnIndex(1);
                    ImGuiHelper.RightAlignText($"{FinishedItemsPerCraft.Values.Sum()}");
                }
            }
            ImGui.EndTable();
        }

        ImGui.Spacing();

        // Display currency usage
        if (TotalCurrencyUsed.Count > 0 && ImGui.BeginTable("CurrencyTable", 3, ImGuiTableFlags.Sortable | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("Currency", ImGuiTableColumnFlags.DefaultSort, 0.5f);
            ImGui.TableSetupColumn("Amount Used", ImGuiTableColumnFlags.None, 0.25f);
            ImGui.TableSetupColumn("Total Cost", ImGuiTableColumnFlags.None, 0.25f);
            ImGui.TableHeadersRow();

            if (ImGui.TableGetSortSpecs() is { } sortSpecs)
            {
                // Apply sorting whenever there's an active sort spec
                int sortedColumn = sortSpecs.Specs.ColumnIndex;
                var sortDescending = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Descending;

                // If sorting specs changed, mark as handled
                if (sortSpecs.SpecsDirty)
                {
                    sortSpecs.SpecsDirty = false;
                }

                // Create sorted collection
                var sortedCurrency = sortedColumn switch
                {
                    0 => sortDescending ? TotalCurrencyUsed.OrderByDescending(x => x.Key) : TotalCurrencyUsed.OrderBy(x => x.Key),
                    1 => sortDescending ? TotalCurrencyUsed.OrderByDescending(x => x.Value) : TotalCurrencyUsed.OrderBy(x => x.Value),
                    2 => sortDescending ? TotalCurrencyUsed.OrderByDescending(x => TotalCostPerCurrency.GetValueOrDefault(x.Key, 0)) : TotalCurrencyUsed.OrderBy(x => TotalCostPerCurrency.GetValueOrDefault(x.Key, 0)),
                    _ => TotalCurrencyUsed.OrderBy(x => x.Key)
                };

                foreach (var currency in sortedCurrency)
                {
                    string currencyName = currency.Key;
                    int amount = currency.Value;
                    double totalCost = TotalCostPerCurrency.GetValueOrDefault(currencyName, 0);

                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(currencyName);

                    ImGui.TableSetColumnIndex(1);
                    ImGuiHelper.RightAlignText($"{amount}");

                    ImGui.TableSetColumnIndex(2);
                    ImGuiHelper.RightAlignText($"{Math.Round(totalCost, 2)}c");
                }

                // If more than 1 currency, show total amount of currency used
                if (TotalCurrencyUsed.Count > 1)
                {
                    // Empty row for spacing
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("");

                    // Show total amount of currency used
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Total");
                    ImGui.TableSetColumnIndex(1);
                    ImGuiHelper.RightAlignText($"{TotalCurrencyUsed.Values.Sum()}");
                    ImGui.TableSetColumnIndex(2);
                    ImGuiHelper.RightAlignText($"{Math.Round(TotalCostPerCurrency.Values.Sum(), 2)}c");
                }
            }
            ImGui.EndTable();
        }
    }
}
