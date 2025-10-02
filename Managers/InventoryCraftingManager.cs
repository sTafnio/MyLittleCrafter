using System;
using System.Threading;
using ExileCore.Shared;
using MyLittleCrafter.Handlers;
using MyLittleCrafter.Enums;
using static MyLittleCrafter.MyLittleCrafter;
using MyLittleCrafter.Utils;

namespace MyLittleCrafter.Managers;

public static class InventoryCraftingManager
{
    public static async SyncTask<bool> CraftItems(CancellationToken token)
    {
        try
        {
            Log.CraftingState( $"Started {Main.CurrentCraftingFile.Name} with {Main.ItemsToCraftOnList.Count} bases.");
            int itemIndex = 0;

            foreach (var craftingBase in Main.ItemsToCraftOnList)
            {
                itemIndex++;
                Log.CraftingState( $"Started Item {itemIndex}.");

                while (true && !token.IsCancellationRequested)
                {
                    var itemEvaluation = EvaluationHandler.EvaluateCraftingBase(craftingBase);

                    // If the item is finished, break the loop
                    if (itemEvaluation.IsItemFinished)
                    {
                        Tracker.Tracker.FinishItem();
                        Log.CraftingState( $"Item {itemIndex} is finished.");
                        break;
                    }

                    // If item evaluation type is currency, apply it
                    else if (itemEvaluation.ConditionType == ConditionType.StackableCurrencyUse)
                    {
                        if (!await CraftingHandler.ApplyCurrency(craftingBase, itemEvaluation, ItemLocation.PlayerInventory, token)) return false;
                    }

                    // Update the crafting base 
                    if (!await craftingBase.UpdateItemDataAsync(token)) return false;
                }
            }

            Log.Success( $"Finished crafting all {itemIndex} items for {Main.CurrentCraftingFile.Name}.");
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
