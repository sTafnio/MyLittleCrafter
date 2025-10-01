using SharpDX;
using MyLittleCrafter.Enums;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Utils;

/// <summary>
/// Centralized logging system with configurable log levels and formatting.
/// </summary>
public static class Log
{
    /// <summary>
    /// Logs a trace message. Use for detailed flow tracing during development.
    /// </summary>
    /// <param name="message">The message to log</param>
    public static void Trace(string message)
    {
        LogMessage(LogLevel.Trace, message);
    }

    /// <summary>
    /// Logs a debug message. Use for detailed debugging information.
    /// </summary>
    /// <param name="message">The message to log</param>
    public static void Debug(string message)
    {
        LogMessage(LogLevel.Debug, message);
    }

    /// <summary>
    /// Logs an informational message. Use for general application flow.
    /// </summary>
    /// <param name="message">The message to log</param>
    public static void Info(string message)
    {
        LogMessage(LogLevel.Info, message);
    }

    /// <summary>
    /// Logs a warning message. Use for potentially harmful situations.
    /// </summary>
    /// <param name="message">The message to log</param>
    public static void Warning(string message)
    {
        LogMessage(LogLevel.Warning, message);
    }

    /// <summary>
    /// Logs an error message. Use for application failures and exceptions.
    /// </summary>
    /// <param name="message">The message to log</param>
    public static void Error(string message)
    {
        LogMessage(LogLevel.Error, message);
    }

    /// <summary>
    /// Logs a success message with configured success color.
    /// </summary>
    /// <param name="message">The message to log</param>
    public static void Success(string message)
    {
        if (!ShouldLog(LogLevel.Info)) return;

        var settings = Main?.Settings?.Logger;
        if (settings == null)
        {
            Main?.LogMessage(message);
            return;
        }

        Main.LogMessage(message, settings.SuccessDuration.Value, settings.SuccessColor.Value);
    }

    /// <summary>
    /// Logs a crafting state message with configured crafting state color.
    /// </summary>
    /// <param name="message">The message to log</param>
    public static void CraftingState(string message)
    {
        if (!ShouldLog(LogLevel.Info)) return;

        var settings = Main?.Settings?.Logger;
        if (settings == null)
        {
            Main?.LogMessage(message);
            return;
        }

        Main.LogMessage(message, settings.CraftingStateDuration.Value, settings.CraftingStateColor.Value);
    }

    /// <summary>
    /// Core logging method that checks log level and routes to appropriate output.
    /// </summary>
    /// <param name="level">The severity level of the message</param>
    /// <param name="message">The message to log</param>
    private static void LogMessage(LogLevel level, string message)
    {
        if (!ShouldLog(level)) return;

        var settings = Main?.Settings?.Logger;
        if (settings == null)
        {
            // Fallback if settings not available
            if (level == LogLevel.Error)
            {
                Main?.LogError(message);
            }
            else
            {
                Main?.LogMessage(message);
            }
            return;
        }

        // Get appropriate duration and color based on log level
        var (duration, color) = GetLogDisplaySettings(level, settings);

        // Log based on level
        if (level == LogLevel.Error)
        {
            Main.LogError(message, duration);
        }
        else
        {
            Main.LogMessage(message, duration, color);
        }
    }

    /// <summary>
    /// Determines if a message at the given log level should be logged based on settings.
    /// </summary>
    /// <param name="level">The log level to check</param>
    /// <returns>True if the message should be logged, false otherwise</returns>
    private static bool ShouldLog(LogLevel level)
    {
        if (Main?.Settings?.Logger == null) return true; // Log everything if settings unavailable

        var minimumLevelStr = Main.Settings.Logger.MinimumLogLevel.Value as string ?? "Info";
        if (!System.Enum.TryParse<LogLevel>(minimumLevelStr, out var minimumLevel))
        {
            minimumLevel = LogLevel.Info;
        }

        return level >= minimumLevel;
    }

    /// <summary>
    /// Gets the display duration and color for a given log level from settings.
    /// </summary>
    /// <param name="level">The log level</param>
    /// <param name="settings">The logger settings</param>
    /// <returns>A tuple containing duration and color</returns>
    private static (int duration, Color color) GetLogDisplaySettings(LogLevel level, Settings.LoggerSettings settings)
    {
        return level switch
        {
            LogLevel.Trace => (settings.TraceDuration.Value, settings.TraceColor.Value),
            LogLevel.Debug => (settings.DebugDuration.Value, settings.DebugColor.Value),
            LogLevel.Info => (settings.InfoDuration.Value, settings.InfoColor.Value),
            LogLevel.Warning => (settings.WarningDuration.Value, settings.WarningColor.Value),
            LogLevel.Error => (settings.ErrorDuration.Value, settings.ErrorColor.Value),
            _ => (5, Color.White) // Default fallback
        };
    }
}
