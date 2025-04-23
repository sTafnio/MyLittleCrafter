using SharpDX;
using static MyLittleCrafter.Enums.MyLittleCrafter;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter;

public static class Logger
{
    private static readonly int errorDuration = 5;
    private static readonly int infoDuration = 5;
    private static readonly int successDuration = 5;
    private static readonly int craftingStateDuration = 5;
    private static readonly Color successColor = Color.Green;
    private static readonly Color craftingStateColor = Color.YellowGreen;

    public static void Log(LogType logType, string msg)
    {
        switch (logType)
        {
            case LogType.Error:
                Main.LogError(msg, errorDuration);
                break;
            case LogType.Info:
                Main.LogMessage(msg, infoDuration);
                break;
            case LogType.Success:
                Main.LogMessage(msg, successDuration, successColor);
                break;
            case LogType.CraftingState:
                Main.LogMessage(msg, craftingStateDuration, craftingStateColor);
                break;
            case LogType.Debug:
                if (Main.Settings.Debug.EnableDebug) Main.LogMessage(msg);
                break;
        }
    }
}
