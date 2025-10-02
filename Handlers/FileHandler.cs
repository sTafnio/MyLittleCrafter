using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyLittleCrafter.IFL;
using static MyLittleCrafter.MyLittleCrafter;
using MyLittleCrafter.Utils;

namespace MyLittleCrafter.Handlers;

public static class FileHandler
{
    /// <summary>
    /// Asynchronously loads one or more crafting files by name and adds them to the list if not already present
    /// </summary>
    /// <param name="fileNames">One or more file names to load (without .json extension)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public static async Task LoadCraftingFilesAsync(string[] fileNames, CancellationToken cancellationToken = default)
    {
        if (fileNames == null || fileNames.Length == 0)
        {
            Log.Error("No file names provided to load.");
            return;
        }

        Log.Info($"Loading {fileNames.Length} crafting file(s) asynchronously...");
        
        // Clear the list before loading new files
        Main.SelectedCraftingFiles.Clear();
        
        var loadedCount = 0;
        var failedCount = 0;

        foreach (var fileName in fileNames)
        {
            var filePath = Path.Combine(Main.ConfigDirectory, $"{fileName}.json");
            
            if (!File.Exists(filePath))
            {
                Log.Error($"{fileName}.json not found");
                failedCount++;
                continue;
            }

            var result = await JsonFileParser.LoadFileAsync(filePath, cancellationToken);

            if (!result.Success)
            {
                Log.Error($"Failed to load {fileName}: {result.ErrorMessage}");
                failedCount++;
                continue;
            }

            // Add the CraftingFile object to SelectedCraftingFiles
            Main.SelectedCraftingFiles.Add(result.CraftingFile);
            loadedCount++;
        }

        // Summary log
        var summary = new List<string>();
        if (loadedCount > 0) summary.Add($"{loadedCount} loaded");
        if (failedCount > 0) summary.Add($"{failedCount} failed");
        
        Log.Info($"Crafting files: {string.Join(", ", summary)}. Total selected: {Main.SelectedCraftingFiles.Count}");
    }

    /// <summary>
    /// Convenience overload for loading a single file
    /// </summary>
    public static Task LoadCraftingFilesAsync(string fileName, CancellationToken cancellationToken = default)
    {
        return LoadCraftingFilesAsync([fileName], cancellationToken);
    }
}
