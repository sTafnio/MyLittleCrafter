using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static MyLittleCrafter.MyLittleCrafter;
using MyLittleCrafter.Tracker;
using MyLittleCrafter.Utils;

namespace MyLittleCrafter;

public static class DiscordService
{
    private static readonly HttpClient HttpClient = new();

    public static void SendDiscordNotification(string message, string statsContent = null, bool isTest = false)
    {
        if (Main?.Settings?.DiscordNotifications == null)
            return;

        if (!isTest && !Main.Settings.DiscordNotifications.EnableDiscordNotifications.Value)
            return;

        string webhookUrl = Main.Settings.DiscordNotifications.WebhookUrl.Value;
        if (string.IsNullOrEmpty(webhookUrl))
        {
            Log.Error( "Discord webhook URL is not configured.");
            return;
        }

        try
        {
            string formattedMessage = message;
            if (statsContent != null)
            {
                formattedMessage = formattedMessage.Replace("{stats}", statsContent);
            }
            else
            {
                formattedMessage = formattedMessage.Replace("{stats}", "");
            }

            var payload = new
            {
                content = formattedMessage,
                username = Main.Settings.DiscordNotifications.Username.Value,
                avatar_url = Main.Settings.DiscordNotifications.AvatarUrl.Value
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            Task.Run(async () =>
            {
                try
                {
                    var response = await HttpClient.PostAsync(webhookUrl, content);
                    if (!response.IsSuccessStatusCode)
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Log.Error( $"Failed to send Discord notification. Status: {response.StatusCode}, Error: {errorContent}");
                    }
                    else
                    {
                        Log.Success( isTest ? "Test Discord notification sent successfully!" : "Discord notification sent successfully!");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error( $"Exception sending Discord notification: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error( $"Exception preparing Discord notification: {ex.Message}");
        }
    }

    public static string FormatCraftStats(CraftInfo craft)
    {
        if (craft == null) return "";

        int totalItems = craft.FinishedItemCount;

        if (totalItems == 0) return "No items completed";

        return $"{totalItems} items completed";
    }
}