using System.Numerics;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using MyLittleCrafter.Utils;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Settings;

[Submenu(EnableCollapsing = false)]
public class SelectedCraftFileDisplay
{
    [Newtonsoft.Json.JsonIgnore]
    [Menu(null, null)]
    public CustomNode SelectedCraftConditions { get; set; } = new();
    
    public SelectedCraftFileDisplay()
    {
        SelectedCraftConditions.DrawDelegate = () =>
        {
            if (Main == null) return;

            if (Main.CurrentCraftingFile != null)
            {
                // Display file header with styling
                if (!string.IsNullOrEmpty(Main.CurrentCraftingFile.Name))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.3f, 0.9f, 1f, 1f));
                    ImGui.TextUnformatted(Main.CurrentCraftingFile.Name);
                    ImGui.PopStyleColor();
                }
                if (!string.IsNullOrEmpty(Main.CurrentCraftingFile.Description))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.7f, 0.7f, 1f));
                    ImGui.TextWrapped(Main.CurrentCraftingFile.Description);
                    ImGui.PopStyleColor();
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Display ItemSelection condition with tree node
                if (Main.CurrentCraftingFile.ItemSelectionCondition != null)
                {
                    var itemSelCond = Main.CurrentCraftingFile.ItemSelectionCondition;
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.7f, 0.3f, 1f));
                    if (ImGui.TreeNodeEx("ItemSelection", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.PopStyleColor();
                        ImGui.Spacing();
                        
                        // Use the new query formatter for ItemSelection too
                        QueryDisplayFormatter.RenderQuery(itemSelCond.OriginalQueryJson, itemSelCond.RawQuery);
                        
                        ImGui.Spacing();
                        ImGui.TreePop();
                    }
                    else
                    {
                        ImGui.PopStyleColor();
                    }
                    ImGui.Spacing();
                }

                // Display crafting conditions with tree nodes
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 1f, 0.5f, 1f));
                ImGui.TextUnformatted($"Crafting Conditions ({Main.CurrentCraftingFile.CraftingConditions.Count})");
                ImGui.PopStyleColor();
                ImGui.Separator();
                ImGui.Spacing();

                foreach (var condition in Main.CurrentCraftingFile.CraftingConditions)
                {
                    // Build condition header with shift indicator
                    var header = $"{condition.Type}{(condition.UseShift ? " [Shift]" : "")}";
                    
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 1f, 0.4f, 1f));
                    if (ImGui.TreeNode(header))
                    {
                        ImGui.PopStyleColor();
                        ImGui.Spacing();
                        
                        // Use the new query formatter
                        QueryDisplayFormatter.RenderQuery(condition.OriginalQueryJson, condition.RawQuery);
                        
                        ImGui.Spacing();
                        ImGui.TreePop();
                    }
                    else
                    {
                        ImGui.PopStyleColor();
                    }
                }
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), "No valid crafting file selected.");
            }
        };
    }
}
