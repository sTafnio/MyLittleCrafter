using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using MyLittleCrafter.Settings;

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
