using System;
using System.Threading;
using ExileCore.Shared;
using MyLittleCrafter.Handlers;
using static MyLittleCrafter.Enums.MyLittleCrafter;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Managers;

public static class InventoryCraftingManager
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
                Logger.Log(LogType.CraftingState, $"Started Item {itemIndex}.");

                while (true && !token.IsCancellationRequested)
                {
                    var itemEvaluation = EvaluationHandler.EvaluateCraftingBase(craftingBase);

                    // If the item is finished, break the loop
                    if (itemEvaluation.IsItemFinished)
                    {
                        Tracker.Tracker.FinishItem();
                        Logger.Log(LogType.CraftingState, $"Item {itemIndex} is finished.");
                        break;
                    }

                    // If item evaluation type is currency, apply it
                    else if (itemEvaluation.ConditionType == ConditionType.StackableCurrencyUse)
                    {
                        // if (!await CraftingHandler.ApplyCurrencyFromInventory(PlayerInventoryHandler.PlayerInventory, craftingBase, itemEvaluation, token)) return false;
                        if (!await CraftingHandler.ApplyCurrency(craftingBase, itemEvaluation, ItemLocation.PlayerInventory, token)) return false;
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
