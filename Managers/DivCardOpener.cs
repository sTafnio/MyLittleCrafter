using System;
using System.Threading;
using ExileCore.Shared;
using MyLittleCrafter.Handlers;
using MyLittleCrafter.Enums;
using static MyLittleCrafter.MyLittleCrafter;
using MyLittleCrafter.Utils;
using ExileCore.Shared.Helpers;

namespace MyLittleCrafter.Managers;

public static class DivCardOpener
{
    public static async SyncTask<bool> OpenDivCards(CancellationToken token)
    {
        try
        {
            var divCardsToOpen = PlayerInventoryHandler.TradableDivCardsInPlayerInventory;
            if (divCardsToOpen.Count < 1)
            {
                Log.Error($"No Divination Cards found.");
                return false;
            }

            Log.CraftingState($"Started opening {divCardsToOpen.Count} Divination Cards.");

            foreach (var divCard in divCardsToOpen)
            {

                if (!await CraftingHandler.RemoveItemFromAnInventory(PlayerInventoryHandler.PlayerInventoryServerInventory, divCard.GetClientRect(), token))
                    return false;

                if (!await CraftingHandler.ClickOnItemOrUI(CardTradeHandler.DivCardTradeButton, token))
                    return false;

                if (!await CraftingHandler.RemoveItemFromAnInventory(CardTradeHandler.DivCardTradeServerInventory, CardTradeHandler.ItemInDivCardTrade, token))
                    return false;

                Log.Debug("Opened a Divination Card");
            }

            Log.Success($"Finished opening all Divination Cards.");
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
