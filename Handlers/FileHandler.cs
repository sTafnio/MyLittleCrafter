using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyLittleCrafter.IFL;
using static MyLittleCrafter.Enums.MyLittleCrafter;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Handlers;

public static class FileHandler
{
    public static void LoadCraftingFile(string fileName)
    {
        Main.CurrentCraftingConditionsList = [];

        var filterFilePath = Path.Combine(Main.ConfigDirectory, $"{fileName}.craft");
        if (!File.Exists(filterFilePath))
        {
            Logger.Log(LogType.Error, $"{fileName} not found");
            return;
        }

        var craftConditions = FileParser.LoadFileAndCompileConditions(filterFilePath);

        if (!VerifyCraftingConditions(craftConditions, fileName))
        {
            return;
        }

        Main.CurrentCraftingConditionsList = craftConditions;
        Logger.Log(LogType.Info, $"{fileName} loaded.");
    }

    private static bool VerifyCraftingConditions(List<CraftCondition> craftConditions, string fileName)
    {
        // Ensure there is at least one non-global condition
        if (!craftConditions.Any(condition => condition.ConditionType != ConditionType.Global))
        {
            Logger.Log(LogType.Error, $"{fileName} is invalid: Must contain at least one non-global condition.");
            return false;
        }

        // Ensure there is exactly one global condition 
        if (craftConditions.Count(condition => condition.ConditionType == ConditionType.Global) != 1)
        {
            Logger.Log(LogType.Error, $"{fileName} is invalid: Must contain exactly one global condition.");
            return false;
        }

        // Ensure using shift is only used with stackable currency use  
        if (craftConditions.Any(condition => condition.ConditionType != ConditionType.StackableCurrencyUse && condition.UseShift))
        {
            Logger.Log(LogType.Error, $"{fileName} is invalid: Using shift is only allowed with stackable currency use.");
            return false;
        }

        // Ensure CraftingBenchCraft and HarvestBenchCraft are mutually exclusive
        bool hasCraftingBenchCraft = craftConditions.Any(condition => condition.ConditionType == ConditionType.CraftingBenchCraft);
        bool hasHarvestBenchCraft = craftConditions.Any(condition => condition.ConditionType == ConditionType.HarvestBenchCraft);
        if (hasCraftingBenchCraft && hasHarvestBenchCraft)
        {
            Logger.Log(LogType.Error, $"{fileName} is invalid: Cannot contain both CraftingBenchCraft and HarvestBenchCraft conditions.");
            return false;
        }

        return true;
    }

    // Not sure if I want to have this
    // private static void AutoSelectCraftingMethod()
    // {
    //     if (Main.CurrentCraftingConditionsList.Any(condition => condition.ConditionType == ConditionType.CraftingBenchCraft))
    //     {
    //         Main.Settings.General.SelectedMethod = CraftingMethod.CraftingBench;
    //         Logger.Log(LogType.Debug, $"CraftingBench condition found, automatically selected CraftingBench method.");
    //     }
    //     else if (Main.CurrentCraftingConditionsList.Any(condition => condition.ConditionType == ConditionType.HarvestBenchCraft))
    //     {
    //         Main.Settings.General.SelectedMethod = CraftingMethod.HarvestBench;
    //         Logger.Log(LogType.Debug, $"HarvestBench condition found, automatically selected HarvestBench method.");
    //     }
    // }
}