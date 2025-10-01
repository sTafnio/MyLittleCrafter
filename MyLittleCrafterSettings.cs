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
using MyLittleCrafter.Enums;
using static MyLittleCrafter.MyLittleCrafter;
using MyLittleCrafter.Tracker;

namespace MyLittleCrafter;

public class MyLittleCrafterSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(true);

    public General General { get; set; } = new();
    public FileSelectionOptions FileOptions { get; set; } = new();
    public StashOptions StashOptions { get; set; } = new();
    public SelectedCraftFileDisplay ConditionsDisplay { get; set; } = new();
    public StatTracker Tracker { get; set; } = new();
    public DiscordNotifications DiscordNotifications { get; set; } = new();
    public SystemOptions SystemOptions { get; set; } = new();
    public Debug Debug { get; set; } = new();
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


[Submenu(EnableCollapsing = false)]
public class SelectedCraftFileDisplay
{
    [JsonIgnore]
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
                        UI.QueryDisplayFormatter.RenderQuery(itemSelCond.OriginalQueryJson, itemSelCond.RawQuery);
                        
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
                        UI.QueryDisplayFormatter.RenderQuery(condition.OriginalQueryJson, condition.RawQuery);
                        
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
public class DiscordNotifications
{
    [Menu("Enable Discord Notifications", "Send notifications to Discord when the crafter stops.")]
    public ToggleNode EnableDiscordNotifications { get; set; } = new ToggleNode(false);

    [Menu("Webhook URL", "Discord webhook URL for sending notifications.")]
    public TextNode WebhookUrl { get; set; } = new TextNode("");

    [Menu("Username", "Override the webhook's default username.")]
    public TextNode Username { get; set; } = new TextNode("MyLittleCrafter");

    [Menu("Avatar URL", "Override the webhook's default avatar.")]
    public TextNode AvatarUrl { get; set; } = new TextNode("");

    [Menu("Message Content", "The message content to send. Use {stats} to include the total number of crafted items.")]
    public TextNode MessageContent { get; set; } = new TextNode("Crafting has stopped! {stats}");

    [Menu("Test Webhook", "Send a test message to verify your webhook configuration.")]
    public ButtonNode TestWebhook { get; set; } = new ButtonNode();
}

[Submenu(CollapsedByDefault = true)]
public class SystemOptions
{
    [Menu("Close ExileAPI when crafting stops", "Close the ExileAPI application when crafting stops.")]
    public ToggleNode CloseExileAPIOnStop { get; set; } = new ToggleNode(false);

    [Menu("Close Game when crafting stops", "Close the Path of Exile game when crafting stops.")]
    public ToggleNode CloseGameOnStop { get; set; } = new ToggleNode(false);

    [Menu("Shutdown PC when crafting stops", "Shutdown the computer when crafting stops.")]
    public ToggleNode ShutdownPCOnStop { get; set; } = new ToggleNode(false);

    [Menu("Send Notification Before Action", "Send a Discord notification before executing the selected action.")]
    public ToggleNode SendNotificationBeforeAction { get; set; } = new ToggleNode(true);

    [Menu("Test Close ExileAPI", "Test closing the ExileAPI.")]
    public ButtonNode TestCloseExileAPI { get; set; } = new ButtonNode();

    [Menu("Test Close Game", "Test closing the Path of Exile game.")]
    public ButtonNode TestCloseGame { get; set; } = new ButtonNode();

    [Menu("Test Shutdown", "Test shutting down the PC.")]
    public ButtonNode TestShutdown { get; set; } = new ButtonNode();
}

