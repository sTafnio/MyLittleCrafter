using System;
using ExileCore.PoEMemory.Models;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Tracker;

public static class Tracker
{
    public static Func<BaseItemType, double> GetBaseItemTypeValue { get; set; } = null;
    public static CraftInfo CurrentTrackedCraft { get; private set; } = null;
    public static bool IsTracking => CurrentTrackedCraft != null;

    public static SessionInfo SessionStats => Main.Settings.Tracker.SessionStats;
    public static AllTimeStats AllTimeStats => Main.Settings.Tracker.AllTimeStats;

    public static void StartCraft()
    {
        if (IsTracking)
        {
            StopCraft();
        }

        CurrentTrackedCraft = new CraftInfo
        {
            CraftName = Main.Settings.FileOptions.SelectedCraftingFile.Value,
            ResourcesUsed = [],
            ResourcesCostPerOneUnit = [],
            FinishedItemCount = 0
        };
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

        // Record the craft in session stats before clearing
        SessionStats.RecordCraft(CurrentTrackedCraft);

        // Also record the craft in all-time stats
        AllTimeStats.RecordCraft(CurrentTrackedCraft);

        // Set the last tracked craft
        Main.Settings.Tracker.LastTrackedCraft = CurrentTrackedCraft;

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
        try
        {
            string actualResourceName = resourceName;
            bool applySpecialMultiplier = false;

            // Check for special craft resources and map them to the correct lifeforce type
            if (resourceName == "Reforge Cold")
            {
                actualResourceName = "YellowLifeforce";
                applySpecialMultiplier = true;
            }
            else if (resourceName == "Reforge Lightning")
            {
                actualResourceName = "BlueLifeforce";
                applySpecialMultiplier = true;
            }
            else if (resourceName == "Reforge Fire")
            {
                actualResourceName = "RedLifeforce";
                applySpecialMultiplier = true;
            }

            var baseItem = new BaseItemType
            {
                BaseName = actualResourceName,
                ClassName = "StackableCurrency",
                Metadata = string.Empty
            };

            double valuePerUnit = GetBaseItemTypeValue(baseItem);

            // Apply the 50x multiplier for special craft resources
            if (applySpecialMultiplier)
            {
                valuePerUnit *= 50;
            }

            CurrentTrackedCraft.ResourcesCostPerOneUnit[resourceName] = Math.Round(valuePerUnit, 2);
        }
        catch (Exception)
        {
        }
    }

    public static void Reset()
    {
        CurrentTrackedCraft = null;
    }

    public static void ResetSession()
    {
        SessionStats.Reset();
    }

    public static void ResetAllTimeStats()
    {
        AllTimeStats.Reset();
    }
}