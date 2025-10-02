using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Tracker;

public class CraftInfo
{
    public DateTime StartTime { get; set; } = DateTime.Now;
    public string CraftName { get; set; } = Main.CurrentCraftingFile?.Name ?? "Unknown";
    public Dictionary<string, int> ResourcesUsed { get; set; } = [];
    public Dictionary<string, double> ResourcesCostPerOneUnit { get; set; } = [];
    public int FinishedItemCount { get; set; } = 0;

    public double TotalCost => ResourcesUsed
        .Sum(resource => ResourcesCostPerOneUnit.GetValueOrDefault(resource.Key, 0) * resource.Value);

    public double CostPerItem => FinishedItemCount > 0 ? TotalCost / FinishedItemCount : TotalCost;

    public Dictionary<string, double> CostPerResource => ResourcesUsed
        .ToDictionary(
            resource => resource.Key,
            resource => ResourcesCostPerOneUnit.GetValueOrDefault(resource.Key, 0) * resource.Value
        );

    public void ToImGUI()
    {
        // Overview Table
        if (ImGui.BeginTable("OverviewTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.PadOuterX))
        {
            // Set up columns
            ImGui.TableSetupColumn($"{CraftName}", ImGuiTableColumnFlags.None, 0.7f);
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.None, 0.3f);

            ImGui.TableHeadersRow();

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Text($"Finished Items");
            ImGui.TableSetColumnIndex(1);
            ImGuiHelper.RightAlignText($"{FinishedItemCount}");

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Text($"Cost Per Item");
            ImGui.TableSetColumnIndex(1);
            ImGuiHelper.RightAlignText($"{Math.Round(CostPerItem, 2):F2}c");

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Text($"Total Cost");
            ImGui.TableSetColumnIndex(1);
            ImGuiHelper.RightAlignText($"{Math.Round(TotalCost, 2):F2}c");

            ImGui.EndTable();
        }


        // Resources Table
        if (ResourcesUsed.Count > 0 && ImGui.BeginTable("ResourcesTable", 4, ImGuiTableFlags.Sortable | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.PadOuterX))
        {
            // Set up columns
            ImGui.TableSetupColumn("Resource", ImGuiTableColumnFlags.DefaultSort, 0.4f);
            ImGui.TableSetupColumn("Amount Used", ImGuiTableColumnFlags.None, 0.2f);
            ImGui.TableSetupColumn("Cost Per Unit", ImGuiTableColumnFlags.None, 0.2f);
            ImGui.TableSetupColumn("Total Cost", ImGuiTableColumnFlags.None, 0.2f);
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
                var sortedResources = sortedColumn switch
                {
                    0 => sortDescending ? ResourcesUsed.OrderByDescending(x => x.Key) : ResourcesUsed.OrderBy(x => x.Key),
                    1 => sortDescending ? ResourcesUsed.OrderByDescending(x => x.Value) : ResourcesUsed.OrderBy(x => x.Value),
                    2 => sortDescending ? ResourcesUsed.OrderByDescending(x => ResourcesCostPerOneUnit.GetValueOrDefault(x.Key, 0)) : ResourcesUsed.OrderBy(x => ResourcesCostPerOneUnit.GetValueOrDefault(x.Key, 0)),
                    3 => sortDescending ? ResourcesUsed.OrderByDescending(x => CostPerResource.GetValueOrDefault(x.Key, 0)) : ResourcesUsed.OrderBy(x => CostPerResource.GetValueOrDefault(x.Key, 0)),
                    _ => ResourcesUsed.OrderBy(x => x.Key)
                };

                // Display sorted resources
                foreach (var resource in sortedResources)
                {
                    DisplayResourceRow(resource);
                }
            }

            ImGui.EndTable();
        }

        // Helper method to display a resource row
        void DisplayResourceRow(KeyValuePair<string, int> resource)
        {
            string resourceName = resource.Key;
            int amount = resource.Value;
            double costPerUnit = ResourcesCostPerOneUnit.GetValueOrDefault(resourceName, 0);
            double totalCost = CostPerResource.GetValueOrDefault(resourceName, 0);

            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.Text(resourceName);

            ImGui.TableSetColumnIndex(1);
            ImGuiHelper.RightAlignText($"{amount}");

            ImGui.TableSetColumnIndex(2);
            ImGuiHelper.RightAlignText($"{Math.Round(costPerUnit, 2):F2}c");

            ImGui.TableSetColumnIndex(3);
            ImGuiHelper.RightAlignText($"{Math.Round(totalCost, 2):F2}c");
        }
    }
}