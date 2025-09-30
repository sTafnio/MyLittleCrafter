using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.Models;
using MyLittleCrafter.Enums;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Tracker;

public static class Tracker
{
    public static Func<BaseItemType, double> GetBaseItemTypeValue { get; set; } = null;
    public static CraftInfo CurrentTrackedCraft { get; private set; } = null;
    public static bool IsTracking => CurrentTrackedCraft != null;
    public static List<CraftInfo> LastTrackedCrafts { get; set; } = [];
    public static CraftingStats SessionStats => Main.Settings.Tracker.SessionStats;
    public static CraftingStats AllTimeStats => Main.Settings.Tracker.AllTimeStats;

    public static void StartCraft()
    {
        if (IsTracking)
        {
            StopCraft();
        }

        CurrentTrackedCraft = new CraftInfo();
    }

    public static void FinishItem(int count = 1)
    {
        if (!IsTracking)
        {
            return;
        }

        CurrentTrackedCraft.FinishedItemCount += count;
    }

    public static void StopCraft()
    {
        if (!IsTracking)
        {
            return;
        }

        // Record the craft
        SessionStats.RecordCraft(CurrentTrackedCraft);
        AllTimeStats.RecordCraft(CurrentTrackedCraft);

        //  Add to the list of last tracked crafts
        LastTrackedCrafts.Insert(0, CurrentTrackedCraft);

        CurrentTrackedCraft = null;
    }

    public static void UseResource(string resourceName, int amount = 1)
    {
        if (!IsTracking)
        {
            return;
        }

        // Track resource usage
        if (!CurrentTrackedCraft.ResourcesUsed.ContainsKey(resourceName))
        {
            CurrentTrackedCraft.ResourcesUsed[resourceName] = 0;
            TryUpdateResourceCost(resourceName);
        }

        CurrentTrackedCraft.ResourcesUsed[resourceName] += amount;
    }

    private static void TryUpdateResourceCost(string resourceName)
    {
        var (actualResourceName, multiplier) = HarvestResourceOverride(resourceName);

        var baseItem = new BaseItemType
        {
            BaseName = actualResourceName,
            ClassName = "StackableCurrency",
            Metadata = string.Empty
        };

        if (GetBaseItemTypeValue != null)
        {
            double valuePerUnit = GetBaseItemTypeValue(baseItem) * multiplier;
            CurrentTrackedCraft.ResourcesCostPerOneUnit[resourceName] = valuePerUnit;
        }
        else
        {
            Logger.Log(LogType.Debug, $"Failed to calculate resource cost - value calculator not initialized");
            CurrentTrackedCraft.ResourcesCostPerOneUnit[resourceName] = 0;
        }
    }

    public static (string, double) HarvestResourceOverride(string resourceName)
    {
        return resourceName switch
        {
            "Reforge Fire" => ("Wild Crystallised Lifeforce", 50),
            "Reforge Lightning" => ("Primal Crystallised Lifeforce", 50),
            "Reforge Cold" => ("Vivid Crystallised Lifeforce", 50),
            "Reforge Physical" => ("Vivid Crystallised Lifeforce", 50),
            "Reforge Life" => ("Wild Crystallised Lifeforce", 75),
            "Reforge Defence" => ("Primal Crystallised Lifeforce", 75),
            "Reforge Chaos" => ("Vivid Crystallised Lifeforce", 100),
            "Reforge Attack" => ("Wild Crystallised Lifeforce", 75),
            "Reforge Caster" => ("Primal Crystallised Lifeforce", 75),
            "Reforge Speed" => ("Vivid Crystallised Lifeforce", 150),
            "Reforge Critical" => ("Vivid Crystallised Lifeforce", 150),
            "Reforge More Likely" => ("Wild Crystallised Lifeforce", 200),
            "Reforge Less Likely" => ("Vivid Crystallised Lifeforce", 200),
            _ => ((string, double))(resourceName, 1),
        };
    }

    public static void ResetLastCrafts()
    {
        LastTrackedCrafts = [];
    }
}