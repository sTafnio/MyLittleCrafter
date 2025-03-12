using System.Collections.Generic;
using System.Linq;
using ImGuiNET;

namespace MyLittleCrafter.Tracker;

public class CraftInfo
{
    public string CraftName { get; set; } = string.Empty;
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
        ImGui.Indent(10);

        ImGui.Text($"Craft: {CraftName}");
        ImGui.Text($"Finished Items: {FinishedItemCount}");
        ImGui.Text($"Total Cost: {TotalCost:F2}c");
        ImGui.Text($"Cost Per Item: {CostPerItem:F2}c");
        ImGui.Separator();

        // Resources Table
        if (ResourcesUsed.Count > 0 && ImGui.BeginTable("ResourcesTable", 4, ImGuiTableFlags.Sortable | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.PadOuterX))
        {
            // Set up columns
            ImGui.TableSetupColumn("Resource", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultSort);
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
            else
            {
                // No sorting, display in default order
                foreach (var resource in ResourcesUsed)
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
            ImGuiHelper.RightAlignText(amount.ToString());

            ImGui.TableSetColumnIndex(2);
            ImGuiHelper.RightAlignText($"{costPerUnit:F2}c");

            ImGui.TableSetColumnIndex(3);
            ImGuiHelper.RightAlignText($"{totalCost:F2}c");
        }

        ImGui.Unindent(10);
    }
}