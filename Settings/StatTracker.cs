using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using MyLittleCrafter.Tracker;

namespace MyLittleCrafter.Settings;

[Submenu(CollapsedByDefault = true)]
public class StatTracker
{
    public CraftingStats SessionStats { get; set; } = new();
    public CraftingStats AllTimeStats { get; set; } = new();

    [Newtonsoft.Json.JsonIgnore]
    public CustomNode TrackerNode { get; set; } = new();

    public StatTracker()
    {
        TrackerNode.DrawDelegate = () =>
        {
            if (ImGui.TreeNode("Current Craft"))
            {
                Tracker.Tracker.CurrentTrackedCraft?.ToImGUI();
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Last Crafts"))
            {
                foreach (var craft in Tracker.Tracker.LastTrackedCrafts)
                {
                    if (ImGui.TreeNode(craft.CraftName + " - " + craft.StartTime.ToString("HH:mm") + " - " + $"{craft.FinishedItemCount}"))
                    {
                        craft.ToImGUI();
                        ImGui.TreePop();
                    }
                }

                if (ImGui.Button("Reset Last Crafts"))
                {
                    Tracker.Tracker.ResetLastCrafts();
                }

                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Session Stats"))
            {
                SessionStats.ToImGUI();

                if (ImGui.Button("Reset Session Stats"))
                {
                    SessionStats.Reset();
                }

                ImGui.TreePop();
            }

            if (ImGui.TreeNode("All-time Stats"))
            {
                AllTimeStats.ToImGUI();

                if (ImGui.Button("Reset All-time Stats"))
                {
                    AllTimeStats.Reset();
                }

                ImGui.TreePop();
            }
        };

    }
}
