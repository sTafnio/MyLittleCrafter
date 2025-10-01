using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;

namespace MyLittleCrafter.Settings;

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
