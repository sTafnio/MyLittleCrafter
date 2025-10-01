using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MyLittleCrafter.IFL;
using static MyLittleCrafter.MyLittleCrafter;
using MyLittleCrafter.Utils;

namespace MyLittleCrafter.Handlers;

public static class FileHandler
{
    /// <summary>
    /// Asynchronously loads a crafting file by name
    /// </summary>
    public static async Task LoadCraftingFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(Main.ConfigDirectory, $"{fileName}.json");
        if (!File.Exists(filePath))
        {
            Log.Error( $"{fileName}.json not found");
            return;
        }

        Log.Info( $"Loading {fileName} asynchronously...");
        var result = await JsonFileParser.LoadFileAsync(filePath, cancellationToken);

        if (!result.Success)
        {
            Log.Error( $"Failed to load {fileName}: {result.ErrorMessage}");
            return;
        }

        // Only assign on success - atomic operation
        Main.CurrentCraftingFile = result.CraftingFile;
        Log.Info( $"{fileName} loaded successfully.");
    }
}
