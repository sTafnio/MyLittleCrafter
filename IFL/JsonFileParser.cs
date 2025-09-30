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
            var globalCondition = allConditions.FirstOrDefault(c => c.ConditionType == ConditionType.Global);
            var craftingConditions = allConditions.Where(c => c.ConditionType != ConditionType.Global).ToList();

            var craftingFile = new CraftingFile
            {
                Name = craftingFileJson.Name,
                Description = craftingFileJson.Description,
                GlobalCondition = globalCondition,
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

        // Add Global/ItemSelection condition first
        if (!string.IsNullOrEmpty(craftingFile.ItemSelection))
        {
            // Compile the global query
            var globalQuery = ItemQuery.Load(craftingFile.ItemSelection.Replace("\n", ""));
            if (globalQuery == null)
            {
                throw new InvalidOperationException($"Failed to compile ItemSelection query: {craftingFile.ItemSelection}");
            }

            conditions.Add(new CraftCondition(
                "Global",
                ConditionType.Global,
                false,
                craftingFile.ItemSelection,
                globalQuery
            ));
        }

        // Convert ConditionJson to CraftCondition with compiled queries
        if (craftingFile.Conditions != null)
        {
            foreach (var conditionJson in craftingFile.Conditions)
            {
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
        // Ensure there is at least one non-global condition
        if (!conditions.Any(c => c.ConditionType != ConditionType.Global))
        {
            return "Must contain at least one non-global condition.";
        }

        // Ensure there is exactly one global condition
        if (conditions.Count(c => c.ConditionType == ConditionType.Global) != 1)
        {
            return "Must contain exactly one ItemSelection (global) condition.";
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
