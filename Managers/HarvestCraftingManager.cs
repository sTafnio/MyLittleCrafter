using System;
using System.Threading;
using ExileCore.Shared;
using MyLittleCrafter.Handlers;
using MyLittleCrafter.Enums;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Managers;

public static class HarvestCraftingManager
{
    public static async SyncTask<bool> CraftItems(CancellationToken token)
    {
        try
        {
            Logger.Log(LogType.CraftingState, $"Started {Main.Settings.FileOptions.SelectedCraftingFile.Value} with {Main.ItemsToCraftOnList.Count} bases.");
            int itemIndex = 0;

            foreach (var craftingBase in Main.ItemsToCraftOnList)
            {
                itemIndex++;

                // If an item is in the bench at the start of the crafting process, that we won't craft on
                if (itemIndex == 1 && craftingBase.ItemLocation != ItemLocation.HarvestBench)
                {
                    if (HarvestBenchHandler.InventSlotItemInHarvestBench != null)
                    {
                        Logger.Log(LogType.Debug, "Item in harvest bench is not a valid crafting base. Need to remove it.");
                        if (!await CraftingHandler.RemoveItemFromAnInventory(HarvestBenchHandler.HarvestBenchServerInventory, HarvestBenchHandler.ItemInHarvestBenchRect, token)) return false;
                    }
                }

                Logger.Log(LogType.CraftingState, $"Started Item {itemIndex}.");

                while (true && !token.IsCancellationRequested)
                {
                    // If the item is not in the harvest bench, move it to the harvest bench
                    if (craftingBase.ItemLocation != ItemLocation.HarvestBench)
                    {
                        if (!await CraftingHandler.RemoveItemFromAnInventory(PlayerInventoryHandler.PlayerInventoryServerInventory, craftingBase.ClientRect, token)) return false;
                        if (!craftingBase.OnMovedToHarvestBench()) return false; // Stop if invalid transition
                    }

                    var itemEvaluation = EvaluationHandler.EvaluateCraftingBase(craftingBase);

                    // If the item is finished, move it back to the player inventory and break the loop
                    if (itemEvaluation.IsItemFinished)
                    {
                        Tracker.Tracker.FinishItem();
                        Logger.Log(LogType.CraftingState, $"Item {itemIndex} is finished.");
                        if (!await CraftingHandler.RemoveItemFromAnInventory(HarvestBenchHandler.HarvestBenchServerInventory, craftingBase.ClientRect, token)) return false;
                        break;
                    }

                    // If item evaluation type is currency, apply it
                    else if (itemEvaluation.ConditionType == ConditionType.StackableCurrencyUse)
                    {
                        if (!await CraftingHandler.ApplyCurrency(craftingBase, itemEvaluation, ItemLocation.PlayerInventory, token)) return false;
                    }

                    // If item evaluation type is a harvest craft (reforge), apply it
                    else if (itemEvaluation.ConditionType == ConditionType.HarvestBenchCraft)
                    {
                        if (!await CraftingHandler.UseCraft(itemEvaluation, ConditionType.HarvestBenchCraft, token)) return false;
                    }

                    // Update the crafting base 
                    if (!await craftingBase.UpdateItemDataAsync(token)) return false;
                }
            }

            Logger.Log(LogType.Success, $"Finished crafting all {itemIndex} items for {Main.Settings.FileOptions.SelectedCraftingFile.Value}.");
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
