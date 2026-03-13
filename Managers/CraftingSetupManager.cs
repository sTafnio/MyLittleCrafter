using System.Collections.Generic;
using System.Linq;
using ExileCore.Shared.Enums;
using MyLittleCrafter.Handlers;
using MyLittleCrafter.Items;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using MyLittleCrafter.Enums;
using static MyLittleCrafter.MyLittleCrafter;
using ExileCore.Shared;
using System.Threading;
using MyLittleCrafter.Utils;
using System;

namespace MyLittleCrafter.Managers;

public static class CraftingSetupManager
{
    public static bool SetUpCrafting()
    {
        if (!BaseCraftingSetupCheck()) return false;

        CraftingBase itemInBench;
        List<InventSlotItem> potentialCraftingBases;

        switch (Main.Settings.General.SelectedMethod)
        {
            case CraftingMethod.Inventory:
                potentialCraftingBases = PlayerInventoryHandler.CraftableInventSlotItemsInPlayerInventory;
                if (!PlayerInventoryHandler.PlayerInventoryCurrencyListCheck()) return false;

                ProcessPotentialCraftingBases(potentialCraftingBases, ItemLocation.PlayerInventory);
                break;

            case CraftingMethod.HarvestBench:
                potentialCraftingBases = PlayerInventoryHandler.CraftableInventSlotItemsInPlayerInventory;
                if (!PlayerInventoryHandler.PlayerInventoryCurrencyListCheck()) return false;

                // Check Bench
                itemInBench = ProcessInventSlotItem(HarvestBenchHandler.InventSlotItemInHarvestBench, ItemLocation.HarvestBench);
                if (itemInBench != null) Main.ItemsToCraftOnList.Add(itemInBench);

                ProcessPotentialCraftingBases(potentialCraftingBases, ItemLocation.PlayerInventory);
                break;

            case CraftingMethod.CraftingBench:
                potentialCraftingBases = PlayerInventoryHandler.CraftableInventSlotItemsInPlayerInventory;
                if (!PlayerInventoryHandler.PlayerInventoryCurrencyListCheck()) return false;

                // Check Bench
                itemInBench = ProcessInventSlotItem(CraftingBenchHandler.InventSlotItemInCraftingBench, ItemLocation.CraftingBench);
                if (itemInBench != null) Main.ItemsToCraftOnList.Add(itemInBench);

                ProcessPotentialCraftingBases(potentialCraftingBases, ItemLocation.PlayerInventory);
                break;

            case CraftingMethod.FullStash:
                if (!FullStashSetupCheck()) return false;
                if (!StashHandler.CurrencyStashCurrencyListCheck()) return false;
                potentialCraftingBases = StashHandler.CraftableInventSlotItemsInInputStash;

                // Check currency stash item slot
                var craftingBaseInStash = ProcessInventSlotItem(StashHandler.NonCurrencyItemInCurrencyStash, ItemLocation.CurrencyStash);
                if (craftingBaseInStash != null) Main.ItemsToCraftOnList.Add(craftingBaseInStash);

                ProcessPotentialCraftingBases(potentialCraftingBases, ItemLocation.InputStash);
                break;
            default:
                Log.Error($"Invalid crafting method: {Main.Settings.General.SelectedMethod}.");
                return false;
        }

        // Check if any crafting bases exist in the list after processing
        if (Main.ItemsToCraftOnList.Count == 0)
        {
            Log.Error("Could not find any crafting bases to craft on.");
            return false;
        }
        Log.Debug($"Found {Main.ItemsToCraftOnList.Count} crafting bases to craft on.");

        Log.Debug($"Full crafting setup is valid.");
        return true;
    }

    private static bool BaseCraftingSetupCheck()
    {
        // Check if a crafting file is selected
        var enabledFiles = Main.Settings.CraftFileRules.Where(x => x.Enabled).Count();
        if (enabledFiles < 1)
        {
            Log.Error("No Crafting File selected.");
            return false;
        }

        // Check if crafting file is loaded
        if (Main.CurrentCraftingFile == null)
        {
            Log.Error("No crafting file loaded.");
            return false;
        }

        // Check if any crafting conditions exist
        if (Main.CurrentCraftingFile.CraftingConditions == null || Main.CurrentCraftingFile.CraftingConditions.Count == 0)
        {
            Log.Error("No crafting conditions exist.");
            return false;
        }

        // Check if the crafting method and the crafting file match
        if (!CraftingMethodAndFileMatchCheck()) return false;

        Log.Debug("Base crafting setup is valid.");
        return true;
    }

    private static bool CraftingMethodAndFileMatchCheck()
    {
        var selectedMethod = Main.Settings.General.SelectedMethod;
        var isValid = false;

        switch (selectedMethod)
        {
            // Inventory and Full Stash can only have Stackable Currency Use and ItemSelection conditions
            case CraftingMethod.Inventory or CraftingMethod.FullStash:
                isValid = Main.CurrentCraftingFile.GetAllConditions().All(condition =>
                    condition.ConditionType == ConditionType.StackableCurrencyUse || condition.ConditionType == ConditionType.ItemSelection);
                if (!isValid)
                {
                    Log.Error($"Invalid condition type(s) exist for {selectedMethod}. Only Stackable Currency Use and ItemSelection conditions are allowed.");
                    return false;
                }
                break;

            // Crafting Bench can only have Stackable Currency Use, Crafting Bench Craft and ItemSelection conditions
            case CraftingMethod.CraftingBench:
                isValid = Main.CurrentCraftingFile.GetAllConditions().All(condition =>
                    condition.ConditionType == ConditionType.StackableCurrencyUse || condition.ConditionType == ConditionType.CraftingBenchCraft || condition.ConditionType == ConditionType.ItemSelection);
                if (!isValid)
                {
                    Log.Error($"Invalid condition type(s) exist for {selectedMethod}. Only Stackable Currency Use, Crafting Bench Craft and ItemSelection conditions are allowed.");
                    return false;
                }
                break;

            // Harvest Bench can only have Stackable Currency Use, Harvest Bench Craft and ItemSelection conditions
            case CraftingMethod.HarvestBench:
                isValid = Main.CurrentCraftingFile.GetAllConditions().All(condition =>
                    condition.ConditionType == ConditionType.StackableCurrencyUse || condition.ConditionType == ConditionType.HarvestBenchCraft || condition.ConditionType == ConditionType.ItemSelection);
                if (!isValid)
                {
                    Log.Error($"Invalid condition type(s) exist for {selectedMethod}. Only Stackable Currency Use, Harvest Bench Craft and ItemSelection conditions are allowed.");
                    return false;
                }
                break;

            default:
                Log.Error($"No crafting method selected: {selectedMethod}.");
                return false;
        }

        Log.Debug($"{selectedMethod} is valid for the loaded crafting conditions.");
        return true;
    }

    public static void ProcessPotentialCraftingBases(List<InventSlotItem> potentialCraftingBases, ItemLocation itemLocation)
    {
        foreach (var potentialBase in potentialCraftingBases)
        {
            var craftingBase = ProcessInventSlotItem(potentialBase, itemLocation);
            if (craftingBase != null) Main.ItemsToCraftOnList.Add(craftingBase);
        }
    }

    public static CraftingBase ProcessInventSlotItem(InventSlotItem inventSlotItem, ItemLocation itemLocation)
    {
        if (inventSlotItem == null) return null;
        if (!EvaluationHandler.IsItemMatchingItemSelection(inventSlotItem)) return null;
        if (EvaluationHandler.IsItemFinished(inventSlotItem)) return null;

        return new CraftingBase(inventSlotItem, itemLocation);
    }

    private static bool FullStashSetupCheck()
    {
        // Check if stashes are selected
        if (StashHandler.InputStashIndex == -1 || StashHandler.OutputStashIndex == -1 || StashHandler.CurrencyStashIndex == -1)
        {
            Log.Error("Not all stash tabs are selected in settings.");
            return false;
        }

        // Check if currency and input stash tabs are loaded, no need to load output stash
        if (StashHandler.CurrencyStashServerInventory == null || StashHandler.InputStashServerInventory == null)
        {
            Log.Error("Stash tabs are not loaded. Select the stash tabs once to load them.");
            return false;
        }

        // Check for correct stash tab types
        if (StashHandler.InputStashTabType != InventoryType.NormalStash && StashHandler.InputStashTabType != InventoryType.QuadStash)
        {
            Log.Error("Input stash tab is not a valid type. Only Normal or Quad stash tabs are supported.");
            return false;
        }

        // if (StashHandler.OutputStashTabType != InventoryType.NormalStash && StashHandler.OutputStashTabType != InventoryType.QuadStash)
        // {
        //     Log.Error("Output stash tab is not a valid stash tab type. Only Normal or Quad Stash are supported.");
        //     return false;
        // }

        if (StashHandler.CurrencyStashTabType != InventoryType.CurrencyStash)
        {
            Log.Error("Currency stash tab is not a valid stash tab type. Only Currency Stash is supported.");
            return false;
        }

        Log.Debug("Selected stash tabs are valid.");
        return true;
    }

    public static async SyncTask<bool> LoadStashes(CancellationToken token)
    {
        if (Main.Settings.General.SelectedMethod != CraftingMethod.FullStash)
            return true;
            
        if (!await CraftingHandler.MoveToStashIndex(Main.Settings.StashOptions.CurrencyStashIndex, token))
        {
            Log.Error("Could not move to Currency Stash to load it.");
        }
        if (!await CraftingHandler.MoveToStashIndex(Main.Settings.StashOptions.InputStashIndex, token))
        {
            Log.Error("Could not move to Input Stash to load it.");
        }

        return true;
    }
}