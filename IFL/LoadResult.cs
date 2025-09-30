namespace MyLittleCrafter.IFL;

/// <summary>
/// Immutable result of loading and compiling a crafting file
/// </summary>
public record LoadResult(
    bool Success,
    CraftingFile? CraftingFile,
    string? ErrorMessage)
{
    public static LoadResult Ok(CraftingFile craftingFile) =>
        new(Success: true, CraftingFile: craftingFile, ErrorMessage: null);

    public static LoadResult Fail(string errorMessage) =>
        new(Success: false, CraftingFile: null, ErrorMessage: errorMessage);
}
