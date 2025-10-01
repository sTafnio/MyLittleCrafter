namespace MyLittleCrafter.Enums;

/// <summary>
/// Defines the severity level of log messages.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Most detailed logging level for tracing program flow.
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Detailed debugging information.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Informational messages about normal application flow.
    /// </summary>
    Info = 2,

    /// <summary>
    /// Warning messages for potentially harmful situations.
    /// </summary>
    Warning = 3,

    /// <summary>
    /// Error messages for failures in the application.
    /// </summary>
    Error = 4
}
