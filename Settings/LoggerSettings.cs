using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace MyLittleCrafter.Settings;

/// <summary>
/// Configuration settings for the logging system.
/// </summary>
[Submenu(CollapsedByDefault = true)]
public class LoggerSettings
{
    [Menu("Minimum Log Level", "Only log messages at or above this level will be displayed.")]
    public ListNode MinimumLogLevel { get; set; } = new ListNode
    {
        Value = "Info",
        Values =
        [
            "Trace",
            "Debug",
            "Info",
            "Warning",
            "Error"
        ]
    };

    [Menu("", "Display duration and color settings for each log level")]
    public EmptyNode DurationAndColorSettings { get; set; } = new EmptyNode();

    [Menu("Trace Duration (seconds)", "How long trace messages are displayed")]
    public RangeNode<int> TraceDuration { get; set; } = new RangeNode<int>(3, 1, 30);

    [Menu("Trace Color", "Color for trace messages")]
    public ColorNode TraceColor { get; set; } = new ColorNode(Color.Gray);

    [Menu("Debug Duration (seconds)", "How long debug messages are displayed")]
    public RangeNode<int> DebugDuration { get; set; } = new RangeNode<int>(3, 1, 30);

    [Menu("Debug Color", "Color for debug messages")]
    public ColorNode DebugColor { get; set; } = new ColorNode(Color.Cyan);

    [Menu("Info Duration (seconds)", "How long info messages are displayed")]
    public RangeNode<int> InfoDuration { get; set; } = new RangeNode<int>(5, 1, 30);

    [Menu("Info Color", "Color for info messages")]
    public ColorNode InfoColor { get; set; } = new ColorNode(Color.White);

    [Menu("Warning Duration (seconds)", "How long warning messages are displayed")]
    public RangeNode<int> WarningDuration { get; set; } = new RangeNode<int>(7, 1, 30);

    [Menu("Warning Color", "Color for warning messages", 108)]
    public ColorNode WarningColor { get; set; } = new ColorNode(Color.Yellow);

    [Menu("Error Duration (seconds)", "How long error messages are displayed")]
    public RangeNode<int> ErrorDuration { get; set; } = new RangeNode<int>(10, 1, 30);

    [Menu("Error Color", "Color for error messages")]
    public ColorNode ErrorColor { get; set; } = new ColorNode(Color.Red);

    [Menu("", "Special message type settings")]
    public EmptyNode SpecialSettings { get; set; } = new EmptyNode();

    [Menu("Success Duration (seconds)", "How long success messages are displayed")]
    public RangeNode<int> SuccessDuration { get; set; } = new RangeNode<int>(5, 1, 30);

    [Menu("Success Color", "Color for success messages")]
    public ColorNode SuccessColor { get; set; } = new ColorNode(Color.Green);

    [Menu("Crafting State Duration (seconds)", "How long crafting state messages are displayed")]
    public RangeNode<int> CraftingStateDuration { get; set; } = new RangeNode<int>(5, 1, 30);

    [Menu("Crafting State Color", "Color for crafting state messages")]
    public ColorNode CraftingStateColor { get; set; } = new ColorNode(Color.YellowGreen);
}
