using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using MyLittleCrafter.Handlers;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Settings;

[Submenu(CollapsedByDefault = true)]
public class Debug
{
    [Menu("Enable Debug", "Enables some extra logging. Spams the Debug Window, so only really useful when the log file is needed.")]
    public ToggleNode EnableDebug { get; set; } = new ToggleNode(false);

    [Menu("Enable Developing Debug", "Shows random stuff in the settings window for development purposes.")]
    public ToggleNode EnableDevDebug { get; set; } = new ToggleNode(false);

    [Newtonsoft.Json.JsonIgnore]
    [ConditionalDisplay(nameof(EnableDevDebug), true)]
    public CustomNode DebugNode { get; set; } = new();

    public Debug()
    {
        DebugNode.DrawDelegate = () =>
        {
            if (Main == null) return;

            ImGui.Separator();
            ImGui.Text($"Items to Craft: {Main.ItemsToCraftOnList.Count}");
            foreach (var item in Main.ItemsToCraftOnList)
            {
                ImGui.Text($"Rect: {item.ClientRect}");
            }

            ImGui.Separator();
            ImGui.Text($"IsCraftSelected: {StateHandler.IsCraftSelected}");

            ImGui.Separator();
            var testCursorRect = Main?.GameController?.Game?.IngameState?.IngameUi?.Cursor?.GetClientRect();
            ImGui.Text($"Cursor Rect: {testCursorRect.Value.X} {testCursorRect.Value.Y} {testCursorRect.Value.Width} {testCursorRect.Value.Height}");
        };
    }
}
