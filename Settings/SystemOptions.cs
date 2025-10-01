using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;

namespace MyLittleCrafter.Settings;

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
