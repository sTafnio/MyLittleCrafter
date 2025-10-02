namespace MyLittleCrafter.Settings;

/// <summary>
/// Represents a craft file rule with enable/disable and ordering support.
/// </summary>
public class CraftFileRule
{
    public string FileName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}
