using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ItemFilterLibrary;
using Newtonsoft.Json;
using MyLittleCrafter.Enums;

namespace MyLittleCrafter.IFL;

public static class JsonFileParser
{
    /// <summary>
    /// Asynchronously loads a crafting file from disk
    /// </summary>
    public static async Task<LoadResult> LoadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            // Clear token cache before deserializing to ensure clean state
            QueryConverter.ClearTokenCache();
            
            // Read file asynchronously
            var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            
            // Deserialize JSON (synchronous - Newtonsoft.Json doesn't have true async deserialization)
            var craftingFileJson = JsonConvert.DeserializeObject<CraftingFileJson>(jsonContent);

            if (craftingFileJson == null)
            {
                return LoadResult.Fail("Failed to deserialize JSON file.");
            }

            // Convert to CraftCondition list
            var allConditions = ConvertToCraftConditions(craftingFileJson);

            if (allConditions == null || allConditions.Count == 0)
            {
                return LoadResult.Fail("No conditions found in file.");
            }

            // Validate conditions
            var validationError = ValidateConditions(allConditions);
            if (validationError != null)
            {
                return LoadResult.Fail(validationError);
            }

            // Compile queries
            var compilationError = CompileConditions(allConditions);
            if (compilationError != null)
            {
                return LoadResult.Fail(compilationError);
            }

            // Create CraftingFile domain object
            var itemSelectionCondition = allConditions.FirstOrDefault(c => c.ConditionType == ConditionType.ItemSelection);
            var craftingConditions = allConditions.Where(c => c.ConditionType != ConditionType.ItemSelection).ToList();

            var craftingFile = new CraftingFile
            {
                Name = craftingFileJson.Name,
                Description = craftingFileJson.Description,
                ItemSelectionCondition = itemSelectionCondition,
                CraftingConditions = craftingConditions
            };

            return LoadResult.Ok(craftingFile);
        }
        catch (OperationCanceledException)
        {
            return LoadResult.Fail("File loading was cancelled.");
        }
        catch (JsonException ex)
        {
            return LoadResult.Fail($"JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return LoadResult.Fail($"Error loading file: {ex.Message}");
        }
    }

    private static List<CraftCondition> ConvertToCraftConditions(CraftingFileJson craftingFile)
    {
        var conditions = new List<CraftCondition>();

        // Add ItemSelection condition first
        if (!string.IsNullOrEmpty(craftingFile.ItemSelection))
        {
            // ItemSelection now uses QueryConverter, so retrieve its token
            // The token for ItemSelection is at index 0 (parsed first)
            var itemSelectionToken = QueryConverter.GetTokenAtIndex(0);
            
            // Compile the query
            var globalQuery = ItemQuery.Load(craftingFile.ItemSelection.Replace("\n", ""));
            if (globalQuery == null)
            {
                throw new InvalidOperationException($"Failed to compile ItemSelection query: {craftingFile.ItemSelection}");
            }

            conditions.Add(new CraftCondition(
                "ItemSelection",
                ConditionType.ItemSelection,
                false,
                craftingFile.ItemSelection,
                globalQuery,
                itemSelectionToken
            ));
        }

        // Convert ConditionJson to CraftCondition with compiled queries
        if (craftingFile.Conditions != null)
        {
            for (int i = 0; i < craftingFile.Conditions.Count; i++)
            {
                var conditionJson = craftingFile.Conditions[i];
                
                // Retrieve the original JSON token for this condition using its index
                // Add 1 to account for ItemSelection being at index 0
                conditionJson.OriginalQueryJson = QueryConverter.GetTokenAtIndex(i + 1);
                
                var compiledQuery = ItemQuery.Load(conditionJson.Query.Replace("\n", ""));
                if (compiledQuery == null)
                {
                    throw new InvalidOperationException($"Failed to compile query for '{conditionJson.Type}': {conditionJson.Query}");
                }

                conditions.Add(conditionJson.ToRuntimeCondition(compiledQuery));
            }
        }

        return conditions;
    }

    private static string ValidateConditions(List<CraftCondition> conditions)
    {
        // Ensure there is at least one non-ItemSelection condition
        if (!conditions.Any(c => c.ConditionType != ConditionType.ItemSelection))
        {
            return "Must contain at least one condition besides ItemSelection.";
        }

        // Ensure there is exactly one ItemSelection condition
        if (conditions.Count(c => c.ConditionType == ConditionType.ItemSelection) != 1)
        {
            return "Must contain exactly one ItemSelection condition.";
        }

        // Ensure using shift is only used with stackable currency use
        if (conditions.Any(c => c.ConditionType != ConditionType.StackableCurrencyUse && c.UseShift))
        {
            return "UseShift is only allowed with stackable currency (not bench/harvest crafts).";
        }

        // Ensure CraftingBenchCraft and HarvestBenchCraft are mutually exclusive
        bool hasCraftingBench = conditions.Any(c => c.ConditionType == ConditionType.CraftingBenchCraft);
        bool hasHarvestBench = conditions.Any(c => c.ConditionType == ConditionType.HarvestBenchCraft);
        if (hasCraftingBench && hasHarvestBench)
        {
            return "Cannot contain both CraftingBench and HarvestBench crafts in the same file.";
        }

        return null; // No errors
    }

    private static string CompileConditions(List<CraftCondition> allConditions)
    {
        // Validate that all queries are compiled
        foreach (var condition in allConditions)
        {
            if (condition.CompiledQuery == null)
            {
                return $"Query for '{condition.Type}' is not compiled. This should not happen.";
            }
        }

        return null; // No errors
    }
}
