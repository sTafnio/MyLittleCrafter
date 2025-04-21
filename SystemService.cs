using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static MyLittleCrafter.Enums.MyLittleCrafter;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter;

public static class SystemService
{
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private const uint WM_CLOSE = 0x0010;

    public static void CloseExileAPI(bool sendNotification = false)
    {
        try
        {
            // Get the current process
            Process currentProcess = Process.GetCurrentProcess();

            // Attempt to close the ExileAPI gracefully
            currentProcess.CloseMainWindow();

            if (sendNotification && Main?.Settings?.DiscordNotifications?.EnableDiscordNotifications?.Value == true)
            {
                DiscordService.SendDiscordNotification("ExileAPI has been closed by MyLittleCrafter.");
            }

            Logger.Log(LogType.Info, "ExileAPI has been closed.");
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, $"Failed to close ExileAPI: {ex.Message}");
        }
    }

    public static void CloseGame(bool sendNotification = false)
    {
        try
        {
            // Find PoE window and send a close message
            IntPtr poeWindow = FindWindow("POEWindowClass", "Path of Exile");
            if (poeWindow != IntPtr.Zero)
            {
                // Send WM_CLOSE message to the window
                PostMessage(poeWindow, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

                if (sendNotification && Main?.Settings?.DiscordNotifications?.EnableDiscordNotifications?.Value == true)
                {
                    DiscordService.SendDiscordNotification("Path of Exile has been closed by MyLittleCrafter.");
                }

                Logger.Log(LogType.Info, "Path of Exile has been closed.");
            }
            else
            {
                Logger.Log(LogType.Error, "Path of Exile window not found.");
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, $"Failed to close Path of Exile: {ex.Message}");
        }
    }

    public static void ShutdownComputer(bool sendNotification = false)
    {
        try
        {
            if (sendNotification && Main?.Settings?.DiscordNotifications?.EnableDiscordNotifications?.Value == true)
            {
                // Send notification before shutdown
                DiscordService.SendDiscordNotification("Computer is being shut down by MyLittleCrafter.");
                // Give time for the notification to be sent
                Task.Delay(2000).Wait();
            }

            Logger.Log(LogType.Info, "Computer is being shut down.");

            // Execute the shutdown command
            Process.Start("shutdown", "/s /t 5 /c \"Shutdown initiated by MyLittleCrafter\"");
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, $"Failed to shutdown computer: {ex.Message}");
        }
    }
}