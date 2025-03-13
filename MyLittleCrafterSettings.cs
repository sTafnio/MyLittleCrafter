using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using Newtonsoft.Json;
using MyLittleCrafter.Handlers;
using static MyLittleCrafter.Enums.MyLittleCrafter;
using static MyLittleCrafter.MyLittleCrafter;
using MyLittleCrafter.Tracker;
using ExileCore;

namespace MyLittleCrafter;

public class MyLittleCrafterSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(true);

    public General General { get; set; } = new();
    public FileSelectionOptions FileOptions { get; set; } = new();
    public StashOptions StashOptions { get; set; } = new();
    public SelectedCraftFileDisplay ConditionsDisplay { get; set; } = new();
    public StatTracker Tracker { get; set; } = new();
    public Debug Debug { get; set; } = new();
    public Help Help { get; set; } = new();
}

[Submenu(EnableCollapsing = false)]
public class General
{
    [Menu("Toggle", "Toggle to start/stop the crafting process.")]
    public HotkeyNode ToggleButton { get; set; } = new HotkeyNode(Keys.None);

    [JsonIgnore]
    [Menu(null, "Select crafting type.")]
    public CustomNode CraftTypeSelection { get; set; } = new();
    public CraftingMethod SelectedMethod { get; set; }

    public General()
    {
        CraftTypeSelection.DrawDelegate = () =>
        {
            var craftingMethods = Enum.GetValues(typeof(CraftingMethod))
                .Cast<CraftingMethod>()
                .Where(method => method != CraftingMethod.None)
                .ToArray();
            var currentMethod = SelectedMethod;

            var methodCount = craftingMethods.Length;
            var i = 0;

            foreach (var method in craftingMethods)
            {
                var isSelected = currentMethod == method;
                if (ImGui.RadioButton(method.ToString(), isSelected))
                {
                    SelectedMethod = method;
                    Logger.Log(LogType.Debug, $"Selected crafting method: {method}");
                }

                if (i < methodCount - 1)
                {
                    ImGui.SameLine();
                }
                i++;
            }
        };
    }
}


[Submenu(EnableCollapsing = false)]
public class FileSelectionOptions
{
    [Menu("Crafting File", "Select the crafting file to use.")]
    public ListNode SelectedCraftingFile { get; set; } = new();

    [JsonIgnore]
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
                FileHandler.LoadCraftingFile(SelectedCraftingFile);
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
                    Logger.Log(LogType.Error, $"{directory} not found.");
                }
            }
        };
    }
}


[Submenu(EnableCollapsing = false)]
public class StashOptions
{
    public int InputStashIndex { get; set; } = -1;
    public int OutputStashIndex { get; set; } = -1;
    public int CurrencyStashIndex { get; set; } = -1;

    [JsonIgnore]
    [Menu(null, null)]
    public CustomNode StashButtons { get; set; } = new();

    public StashOptions()
    {
        StashButtons.DrawDelegate = () =>
        {
            if (Main?.Settings?.General != null && Main.Settings.General.SelectedMethod == CraftingMethod.FullStash)
            {
                var stashNames = StashHandler.AllStashNames;

                if (stashNames != null && stashNames.Count > 0)
                {
                    if (ImGui.BeginCombo("Input Stash", InputStashIndex >= 0 && InputStashIndex < stashNames.Count ? stashNames[InputStashIndex] : "Select Input Stash"))
                    {
                        for (int i = 0; i < stashNames.Count; i++)
                        {
                            bool isSelected = InputStashIndex == i;
                            if (ImGui.Selectable(stashNames[i], isSelected))
                            {
                                InputStashIndex = i;
                                Logger.Log(LogType.Debug, $"Input Stash selected: {stashNames[i]}");
                            }

                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }

                    if (ImGui.BeginCombo("Output Stash", OutputStashIndex >= 0 && OutputStashIndex < stashNames.Count ? stashNames[OutputStashIndex] : "Select Output Stash"))
                    {
                        for (int i = 0; i < stashNames.Count; i++)
                        {
                            bool isSelected = OutputStashIndex == i;
                            if (ImGui.Selectable(stashNames[i], isSelected))
                            {
                                OutputStashIndex = i;
                                Logger.Log(LogType.Debug, $"Output Stash selected: {stashNames[i]}");
                            }

                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }

                    if (ImGui.BeginCombo("Currency Stash", CurrencyStashIndex >= 0 && CurrencyStashIndex < stashNames.Count ? stashNames[CurrencyStashIndex] : "Select Currency Stash"))
                    {
                        for (int i = 0; i < stashNames.Count; i++)
                        {
                            bool isSelected = CurrencyStashIndex == i;
                            if (ImGui.Selectable(stashNames[i], isSelected))
                            {
                                CurrencyStashIndex = i;
                                Logger.Log(LogType.Debug, $"Currency Stash selected: {stashNames[i]}");
                            }

                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }
                }
            }
        };
    }
}


[Submenu(CollapsedByDefault = true)]
public class SelectedCraftFileDisplay
{
    [JsonIgnore]
    public CustomNode SelectedCraftConditions { get; set; } = new();
    public SelectedCraftFileDisplay()
    {
        SelectedCraftConditions.DrawDelegate = () =>
        {
            if (Main == null) return;

            if (Main.CurrentCraftingConditionsList.Count > 0)
            {
                foreach (var filter in Main.CurrentCraftingConditionsList)
                {
                    ImGui.TextColored(new Vector4(0f, 1f, 0.022f, 1f), filter.Header + (filter.UseShift ? " - Shift" : string.Empty));
                    ImGui.SetNextItemWidth(399);
                    ImGui.Separator();
                    ImGui.TextUnformatted(filter.RawQuery);
                    ImGui.Spacing();
                }
            }

            else
            {
                ImGui.Text("No valid crafting file selected.");
            }
        };
    }
}

[Submenu(CollapsedByDefault = true)]
public class StatTracker
{
    public CraftingStats SessionStats { get; set; } = new();
    public CraftingStats AllTimeStats { get; set; } = new();

    [JsonIgnore]
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

[Submenu(CollapsedByDefault = true)]
public class Debug
{
    [Menu("Enable Debug", "Enables some extra logging. Spams the Debug Window, so only really useful when the log file is needed.")]
    public ToggleNode EnableDebug { get; set; } = new ToggleNode(false);

    [Menu("Enable Developing Debug", "Shows random stuff in the settings window for development purposes.")]
    public ToggleNode EnableDevDebug { get; set; } = new ToggleNode(false);

    [JsonIgnore]
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

[Submenu(CollapsedByDefault = true)]
public class Help
{
    [JsonIgnore]
    public CustomNode HelpText { get; set; } = new();

    public Help()
    {
        HelpText.DrawDelegate = () =>
        {
            ImGui.TextWrapped(helpText);
        };
    }
    private readonly string helpText = "Check the example crafting files that I hopefully provided in the source folder of this plugin:)";

}

