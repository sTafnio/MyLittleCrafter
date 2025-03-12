using System;
using System.Threading;
using ExileCore.Shared;
using MyLittleCrafter.Handlers;
using static MyLittleCrafter.Enums.MyLittleCrafter;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Managers;

public static class FullStashCraftingManager
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

                //TODO: Fix this
                // If an item is in the currency stash at the start of the crafting process, that we won't craft on
                if (itemIndex == 1 && craftingBase.ItemLocation != ItemLocation.CurrencyStash && StashHandler.NonCurrencyItemInCurrencyStash != null)
                {
                    Logger.Log(LogType.Debug, "Item in currency stash is not a valid crafting base. Need to remove it.");

                    // Move to currency stash
                    if (!await CraftingHandler.MoveToStashIndex(StashHandler.CurrencyStashIndex, token)) return false;

                    // Get the item in the currency stash and remove it
                    var itemInCurrencyStash = StashHandler.GetFirstCraftableItemInVisibleStash();
                    if (!await CraftingHandler.RemoveItemFromVisibleStash(itemInCurrencyStash.GetClientRect(), token)) return false;
                }

                // If the first item is in the currency stash, its client rect needs to be updated right after moving to the currency stash
                else if (itemIndex == 1 && craftingBase.ItemLocation == ItemLocation.CurrencyStash)
                {
                    if (!await CraftingHandler.MoveToStashIndex(Main.Settings.StashOptions.CurrencyStashIndex, token)) return false;

                    var itemInCurrencyStash = StashHandler.GetFirstCraftableItemInVisibleStash();
                    craftingBase.ClientRect = itemInCurrencyStash.GetClientRect();
                }

                Logger.Log(LogType.CraftingState, $"Started Item {itemIndex}.");

                while (true && !token.IsCancellationRequested)
                {
                    var itemEvaluation = EvaluationHandler.EvaluateCraftingBase(craftingBase);

                    // If in input stash, move it to currency stash and update item
                    if (craftingBase.ItemLocation == ItemLocation.InputStash)
                    {
                        if (!await CraftingHandler.MoveItemFromTo(craftingBase, StashHandler.InputStashIndex, StashHandler.CurrencyStashIndex, token)) return false;
                        craftingBase.OnMovedToCurrencyStash();
                    }

                    // If finished, move it from currency stash to output stash
                    // Item can never be finished outside of the currency stash
                    if (itemEvaluation.IsItemFinished)
                    {
                        Tracker.Tracker.FinishItem();
                        Logger.Log(LogType.CraftingState, $"Item {itemIndex} is finished.");
                        if (!await CraftingHandler.MoveItemFromTo(craftingBase, StashHandler.CurrencyStashIndex, StashHandler.OutputStashIndex, token)) return false;
                        break;
                    }

                    // If item evaluation type is currency, apply it
                    else if (itemEvaluation.ConditionType == ConditionType.StackableCurrencyUse)
                    {
                        // if (!await CraftingHandler.ApplyCurrencyFromCurrencyStash(craftingBase, itemEvaluation, token)) return false;
                        if (!await CraftingHandler.ApplyCurrency(craftingBase, itemEvaluation, ItemLocation.CurrencyStash, token)) return false;
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
