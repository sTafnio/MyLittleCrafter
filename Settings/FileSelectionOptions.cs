using System.Diagnostics;
using System.IO;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using MyLittleCrafter.Handlers;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Settings;

[Submenu(EnableCollapsing = false)]
public class FileSelectionOptions
{
    [Menu("Crafting File", "Select the crafting file to use.")]
    public ListNode SelectedCraftingFile { get; set; } = new();

    [Newtonsoft.Json.JsonIgnore]
    [Menu(null, null)]
    public CustomNode CraftingFileButtons { get; set; } = new();

    public FileSelectionOptions()
    {
        CraftingFileButtons.DrawDelegate = () =>
        {
            if (ImGui.Button("Update"))
            {
                Main.UpdateAvailableCraftFiles();
            }

            ImGui.SameLine();
            if (ImGui.Button("Reload"))
            {
                _ = FileHandler.LoadCraftingFileAsync(SelectedCraftingFile);
            }

            ImGui.SameLine();
            if (ImGui.Button("Open Folder"))
            {
                var directory = Main.ConfigDirectory;
                if (Directory.Exists(directory))
                {
                    Process.Start("explorer.exe", directory);
                }
                else
                {
                    Logger.Log(Enums.LogType.Error, $"{directory} not found.");
                }
            }
        };
    }
}
