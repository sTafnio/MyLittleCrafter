using System;
using System.Threading;
using ExileCore.Shared;
using MyLittleCrafter.Handlers;
using static MyLittleCrafter.Enums.MyLittleCrafter;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Managers;

public static class BenchCraftingManager
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
                if (itemIndex == 1 && craftingBase.ItemLocation != ItemLocation.CraftingBench)
                {
                    if (CraftingBenchHandler.InventSlotItemInCraftingBench != null)
                    {
                        Logger.Log(LogType.Debug, "Item in crafting bench is not a valid crafting base. Need to remove it.");
                        if (!await CraftingHandler.RemoveItemFromInventory(CraftingBenchHandler.CraftingBenchServerInventory, CraftingBenchHandler.ItemInCraftingBenchRect, token)) return false;
                    }
                }

                Logger.Log(LogType.CraftingState, $"Started Item {itemIndex}.");

                while (true && !token.IsCancellationRequested)
                {
                    // If the item is not in the crafting bench, move it to the crafting bench
                    if (craftingBase.ItemLocation != ItemLocation.CraftingBench)
                    {
                        if (!await CraftingHandler.RemoveItemFromInventory(PlayerInventoryHandler.PlayerInventoryServerInventory, craftingBase.ClientRect, token)) return false;
                        craftingBase.OnMovedToCraftingBench();
                    }

                    var itemEvaluation = EvaluationHandler.EvaluateCraftingBase(craftingBase);

                    // If the item is finished, move it back to the player inventory and break the loop
                    if (itemEvaluation.IsItemFinished)
                    {
                        Tracker.Tracker.FinishItem();
                        Logger.Log(LogType.CraftingState, $"Item {itemIndex} is finished.");
                        if (!await CraftingHandler.RemoveItemFromInventory(CraftingBenchHandler.CraftingBenchServerInventory, craftingBase.ClientRect, token)) return false;
                        break;
                    }

                    // If item evaluation type is currency, apply it
                    else if (itemEvaluation.ConditionType == ConditionType.StackableCurrencyUse)
                    {
                        // if (!await CraftingHandler.ApplyCurrencyFromInventory(PlayerInventoryHandler.PlayerInventory, craftingBase, itemEvaluation, token)) return false;
                        if (!await CraftingHandler.ApplyCurrency(craftingBase, itemEvaluation, ItemLocation.PlayerInventory, token)) return false;
                    }

                    // If item evaluation type is a bench craft, apply it
                    else if (itemEvaluation.ConditionType == ConditionType.CraftingBenchCraft)
                    {
                        if (!await CraftingHandler.UseCraft(itemEvaluation, ConditionType.CraftingBenchCraft, token)) return false;
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
