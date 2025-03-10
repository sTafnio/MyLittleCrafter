using System.Collections.Generic;
using System.IO;
using ItemFilterLibrary;
using static MyLittleCrafter.Enums.MyLittleCrafter;

namespace MyLittleCrafter.IFL;

public static class FileParser
{
    public static List<CraftCondition> LoadFileAndCompileConditions(string filePath)
    {
        List<CraftCondition> allConditions = ParseFileLines(filePath);

        // If parsing fails e.g. file not found
        if (allConditions == null)
        {
            return [];
        }

        // If compiling of any conditions fails
        if (CompileConditions(allConditions) == null)
        {
            return [];
        }

        return allConditions;
    }

    private static List<CraftCondition> ParseFileLines(string filePath)
    {
        var fileContents = File.ReadAllLines(filePath);
        var allConditions = new List<CraftCondition>();
        CraftCondition currentCondition = null;

        foreach (var line in fileContents)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue; // Skip empty lines and comments

            var craftConditionLine = RemoveCommentFromLine(line); // Remove comments from line

            if (IsConditionHeader(craftConditionLine)) // Check if line is a condition header
            {
                if (currentCondition != null) // Add previous condition to list if it exists
                {
                    allConditions.Add(currentCondition);
                }
                currentCondition = CreateCraftConditionFromHeaderLine(craftConditionLine); // Create new condition from header line
            }
            else if (currentCondition != null) // Add line to current condition if it exists
            {
                currentCondition.RawQuery += craftConditionLine + "\n";
            }
        }

        if (currentCondition != null) // Add last condition to list if it exists
        {
            allConditions.Add(currentCondition);
        }

        return allConditions;
    }

    private static string RemoveCommentFromLine(string line)
    {
        var commentIndex = line.IndexOf("//");
        return commentIndex == -1 ? line : line.Substring(0, commentIndex).TrimEnd();
    }

    private static bool IsConditionHeader(string line)
    {
        return line.StartsWith("#");
    }

    private static CraftCondition CreateCraftConditionFromHeaderLine(string headerLine)
    {
        var useShiftIndicator = headerLine.Contains("$");
        var header = headerLine.Substring(1).Trim();
        if (useShiftIndicator) header = header.Replace("$", "").Trim();

        ConditionType craftingType = header switch
        {
            string s when s.Contains("Global") => ConditionType.Global,
            string s when s.Contains("Craft") => ConditionType.CraftingBenchCraft,
            string s when s.Contains("Reforge") => ConditionType.HarvestBenchCraft,
            _ => ConditionType.StackableCurrencyUse,
        };

        return new CraftCondition(header, craftingType, useShiftIndicator, string.Empty);
    }

    private static List<CraftCondition> CompileConditions(List<CraftCondition> allConditions)
    {
        // Compile CompiledQuery for each CraftCondition
        foreach (var condition in allConditions)
        {
            condition.CompiledQuery = ItemQuery.Load(condition.RawQuery.Replace("\n", ""));
            if (condition.CompiledQuery == null)
            {
                Logger.Log(LogType.Error, $"Failed to compile ItemQuery for '{condition.Header}'. \nRawQuery: \n'{condition.RawQuery}' \nError \n {condition.CompiledQuery.Error}");
                return [];
            }
        }

        return allConditions;
    }
}
