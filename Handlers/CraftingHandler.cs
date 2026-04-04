using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ImGuiNET;
using MyLittleCrafter.IFL;
using MyLittleCrafter.Items;
using SharpDX;
using MyLittleCrafter.Enums;
using static MyLittleCrafter.MyLittleCrafter;
using MyLittleCrafter.Utils;

namespace MyLittleCrafter.Handlers;

public static class CraftingHandler
{
    public static async SyncTask<bool> ShiftUpIfDown(CancellationToken token)
    {
        if (Input.IsKeyDown(Keys.LShiftKey))
        {
            if (!await Main.InputController.KeyUp(Keys.LShiftKey, false, token)) return false;
            StateHandler.CurrentlySelectedCurrency = string.Empty;
        }

        return true;
    }

    public static async SyncTask<bool> ApplyCurrency(CraftingBase craftingBase, EvaluationResult evaluationResult, ItemLocation currencySource, CancellationToken token)
    {
        try
        {
            int initialServerRequestCounter;

            // Check currency availability and get the initial server request counter based on the currency source
            if (currencySource == ItemLocation.PlayerInventory)
            {
                // Get initial server request counter for player inventory
                initialServerRequestCounter = PlayerInventoryHandler.PlayerInventoryServerRequestCounter;

                // Currency availability check in player inventory
                if (!PlayerInventoryHandler.IsCurrencyAvailableInPlayerInventory(evaluationResult.CurrencyOrCraftName))
                {
                    Log.Error( $"Currency {evaluationResult.CurrencyOrCraftName} not available in player inventory.");
                    return false;
                }
            }
            else if (currencySource == ItemLocation.CurrencyStash)
            {
                // Get initial server request counter for currency stash
                initialServerRequestCounter = StashHandler.CurrencyStashServerRequestCounter;

                // Currency availability check in currency stash
                if (!StashHandler.IsCurrencyAvailableInCurrencyStash(evaluationResult.CurrencyOrCraftName))
                {
                    Log.Error( $"Currency {evaluationResult.CurrencyOrCraftName} not available in currency stash.");
                    return false;
                }
            }
            else
            {
                Log.Error( $"Unsupported currency source: {currencySource}");
                return false;
            }

            // If wrong currency is selected, deselect it
            if (StateHandler.CurrentlySelectedCurrency != string.Empty && StateHandler.CurrentlySelectedCurrency != evaluationResult.CurrencyOrCraftName)
            {
                if (!await DeselectCurrency(StateHandler.CurrentlySelectedCurrency, token)) return false;
            }

            // If nothing selected
            if (StateHandler.CurrentlySelectedCurrency == string.Empty)
            {
                // If using shift before selecting currency
                if (evaluationResult.UseShift)
                {
                    if (!await Main.InputController.KeyDown(Keys.LShiftKey, token)) return false;
                }

                // Select currency based on source
                if (currencySource == ItemLocation.PlayerInventory)
                {
                    if (!await SelectCurrency(PlayerInventoryHandler.PlayerInventoryServerInventory, craftingBase, evaluationResult.CurrencyOrCraftName, token)) return false;
                }
                else // CurrencyStash
                {
                    if (!await SelectCurrencyInCurrencyStash(evaluationResult.CurrencyOrCraftName, token)) return false;
                }
            }

            // Correct currency selected
            if (StateHandler.CurrentlySelectedCurrency == evaluationResult.CurrencyOrCraftName)
            {
                if (!await ClickOnItemOrUI(craftingBase.ClientRect, token)) return false;
            }

            // Wait for the server request counter to be updated based on source

            if (currencySource == ItemLocation.PlayerInventory)
            {
                if (!await InventoryHandler.WaitForInventoryToUpdate(PlayerInventoryHandler.PlayerInventoryServerInventory, initialServerRequestCounter, token))
                {
                    Log.Error( $"Timeout while waiting for player inventory to update.");
                    return false;
                }
            }
            else // CurrencyStash
            {
                if (!await StashHandler.WaitForCurrencyStashToUpdate(initialServerRequestCounter, StateHandler.Timeout, token))
                {
                    Log.Error( $"Timeout while waiting for server request counter to update.");
                    return false;
                }
            }

            Tracker.Tracker.UseResource(evaluationResult.CurrencyOrCraftName);
            Log.Debug( $"Successfully applied {evaluationResult.CurrencyOrCraftName}.");

            // Reset selected currency if shift was not held down
            if (!evaluationResult.UseShift)
            {
                StateHandler.CurrentlySelectedCurrency = string.Empty;
            }

            await Task.Delay(StateHandler.ServerLatency, token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public static async SyncTask<bool> UseCraft(EvaluationResult evaluationResult, ConditionType craftType, CancellationToken token)
    {
        try
        {
            string searchString = evaluationResult.CurrencyOrCraftName;

            // Get appropriate parameters based on craft type
            int initialServerRequestCounter = 0;
            if (craftType == ConditionType.HarvestBenchCraft)
            {
                // no counter needed — harvest crafts modify the item in-place
            }
            else if (craftType == ConditionType.CraftingBenchCraft)
            {
                initialServerRequestCounter = CraftingBenchHandler.CraftingBenchServerRequestCounter;
                searchString = evaluationResult.CurrencyOrCraftName.Replace("Craft", "").Trim();
            }
            else
            {
                Log.Error( $"Unsupported craft type: {craftType}.");
                return false;
            }

            if (!await ShiftUpIfDown(token)) return false;

            // Search string check based on craft type
            if ((string.IsNullOrEmpty(HarvestBenchHandler.HarvestSearchFieldText) ||
                HarvestBenchHandler.HarvestSearchFieldText != searchString) &&
                (string.IsNullOrEmpty(CraftingBenchHandler.CraftingBenchSearchFieldText) ||
                CraftingBenchHandler.CraftingBenchSearchFieldText != searchString))
            {
                if (!await EnterSearchString(searchString, token)) return false;
                StateHandler.IsCraftSelected = false; // false here in case we use multiple reforge methods
            }

            // Select the craft if not selected
            if (!StateHandler.IsCraftSelected)
            {
                var craftRect = craftType == ConditionType.HarvestBenchCraft
                                ? HarvestBenchHandler.FirstHarvestCraftRect
                                : CraftingBenchHandler.FirstCraftingBenchCraftRect;
                if (!await ClickOnItemOrUI(craftRect, token)) return false;
                StateHandler.IsCraftSelected = true;
                Log.Debug( $"Successfully selected {evaluationResult.CurrencyOrCraftName}.");
            }

            // Use the craft
            var craftButtonRect = craftType == ConditionType.HarvestBenchCraft
                                ? HarvestBenchHandler.HarvestCraftButtonRect
                                : CraftingBenchHandler.CraftingBenchCraftButtonRect;

            // For harvest bench, capture the item address before clicking — the ServerRequestCounter
            // doesn't update because harvest crafts modify the item in-place (no move in/out of slot).
            long? harvestItemAddressBeforeCraft = craftType == ConditionType.HarvestBenchCraft
                ? HarvestBenchHandler.InventSlotItemInHarvestBench?.Item?.Address
                : null;

            if (!await ClickOnItemOrUI(craftButtonRect, token)) return false;

            // Wait for confirmation that the craft was applied
            if (craftType == ConditionType.HarvestBenchCraft)
            {
                // Wait for the item entity to be replaced (server sends updated item after in-place mod change)
                if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                    () => HarvestBenchHandler.InventSlotItemInHarvestBench?.Item?.Address != harvestItemAddressBeforeCraft, token))
                {
                    Log.Error($"Timeout while waiting for harvest craft to apply.");
                    return false;
                }
            }
            else
            {
                // Wait for the server request counter to be updated
                if (!await InventoryHandler.WaitForInventoryToUpdate(CraftingBenchHandler.CraftingBenchServerInventory, initialServerRequestCounter, token))
                {
                    Log.Error($"Timeout while waiting for server request counter to update.");
                    return false;
                }
            }

            Tracker.Tracker.UseResource(evaluationResult.CurrencyOrCraftName);
            Log.Debug( $"Successfully used {evaluationResult.CurrencyOrCraftName}.");

            await Task.Delay(StateHandler.ServerLatency, token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public static async SyncTask<bool> RemoveItemFromAnInventory(ServerInventory serverInventory, RectangleF rect, CancellationToken token)
    {
        try
        {
            // Get initial server request counter
            var initialServerRequestCounter = serverInventory.ServerRequestCounter;

            if (!await ShiftUpIfDown(token)) return false;

            //Press Ctrl 
            if (!await Main.InputController.KeyDown(Keys.LControlKey, token)) return false;

            // Click on the item
            if (!await ClickOnItemOrUI(rect, token)) return false;

            // Wait for the server request counter to be updated
            if (!await InventoryHandler.WaitForInventoryToUpdate(serverInventory, initialServerRequestCounter, token))
            {
                Log.Error( $"Timeout while waiting for player inventory to update. Could not remove item.");
                return false;
            }

            // Release Ctrl at the end
            if (!await Main.InputController.KeyUp(Keys.LControlKey, false, token)) return false;

            Log.Debug( $"Successfully removed item.");

            await Task.Delay(StateHandler.ServerLatency, token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public static async SyncTask<bool> RemoveItemFromVisibleStash(RectangleF clientRect, CancellationToken token)
    {
        try
        {
            // Get initial server request counter for player inventory
            var initialServerRequestCounter = PlayerInventoryHandler.PlayerInventoryServerRequestCounter;

            if (!await ShiftUpIfDown(token)) return false;

            //Press Ctrl 
            if (!await Main.InputController.KeyDown(Keys.LControlKey, token)) return false;

            // Click on the item
            if (!await ClickOnItemOrUI(clientRect, token)) return false;

            // Release Ctrl
            if (!await Main.InputController.KeyUp(Keys.LControlKey, false, token)) return false;

            // Wait for the server request counter to be updated
            if (!await InventoryHandler.WaitForInventoryToUpdate(PlayerInventoryHandler.PlayerInventoryServerInventory, initialServerRequestCounter, token))
            {
                Log.Error( $"Timeout while waiting for server request counter to update.");
                return false;
            }

            Log.Debug( $"Successfully removed item from visible stash.");

            await Task.Delay(StateHandler.ServerLatency, token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public static async SyncTask<bool> MoveItemFromTo(CraftingBase craftingBase, int startIndex, int endIndex, CancellationToken token)
    {
        if (!await ShiftUpIfDown(token)) return false;

        // Move to start stash
        if (!await MoveToStashIndex(startIndex, token)) return false;

        // Get the initial items in player inventory
        var itemsInPlayerInv = PlayerInventoryHandler.CraftableInventSlotItemsInPlayerInventory;

        // Move item to player inventory
        if (!await RemoveItemFromVisibleStash(craftingBase.ClientRect, token)) return false;

        // Update item
        var newInventSlotItem = InventoryHandler.GetRecentlyAddedInventSlotItem(itemsInPlayerInv, PlayerInventoryHandler.CraftableInventSlotItemsInPlayerInventory);
        craftingBase.OnMovedToPlayerInventory(newInventSlotItem);

        // Move to end stash
        if (!await MoveToStashIndex(endIndex, token)) return false;

        // Move item to end stash
        if (!await RemoveItemFromAnInventory(PlayerInventoryHandler.PlayerInventoryServerInventory, craftingBase.ClientRect, token)) return false;

        Log.Debug( $"Successfully move crafting base from {startIndex} to {endIndex}.");
        return true;
    }

    public static async SyncTask<bool> MoveToStashIndex(int targetStashIndex, CancellationToken token)
    {
        try
        {
            if (StashHandler.IsStashAtIndexVisible(targetStashIndex))
            {
                Log.Debug( $"Already on desired stash tab at index {targetStashIndex}.");
                return true;
            }

            while (!StashHandler.IsStashAtIndexVisible(targetStashIndex) && !token.IsCancellationRequested)
            {
                var initialIndex = StashHandler.CurrentVisibleStashIndex;

                if (StashHandler.CurrentVisibleStashIndex < targetStashIndex)
                {
                    if (!await Main.InputController.KeyDown(Keys.Right, token)) return false;
                    if (!await Main.InputController.KeyUp(Keys.Right, false, token)) return false;
                }
                else
                {
                    if (!await Main.InputController.KeyDown(Keys.Left, token)) return false;
                    if (!await Main.InputController.KeyUp(Keys.Left, false, token)) return false;
                }

                // Wait for the visible stash index to change
                if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(() => StashHandler.CurrentVisibleStashIndex != initialIndex, token))
                {
                    Log.Error( $"Failed to move to next stash tab. Timeout while waiting for stash index to change.");
                    return false;
                }

                Log.Debug( $"Moved to stash tab at index {StashHandler.CurrentVisibleStashIndex}.");
            }

            Log.Debug( $"Successfully moved to desired stash tab at index {targetStashIndex}.");
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public static async SyncTask<bool> DeselectCurrency(string currency, CancellationToken token)
    {
        try
        {
            if (!await ShiftUpIfDown(token)) return false;

            // if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(StateHandler.IsCursorFree, token))
            // {
            //     Log.Error( $"Failed to deselect {currency}: Timeout while waiting for cursor to be free.");
            //     return false;
            // }

            Log.Debug( $"Successfully deselected {currency}.");
            StateHandler.CurrentlySelectedCurrency = string.Empty;
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    // Selecting currency relies on the fact that availability was already checked
    public static async SyncTask<bool> SelectCurrency(ServerInventory serverInventory, CraftingBase craftingBase, string currency, CancellationToken token)
    {
        var currencyLocation = InventoryHandler.GetRandomPointForClosestCurrencyInAnInventory(serverInventory, craftingBase, currency);

        if (!await Main.InputController.Click(MouseButtons.Right, currencyLocation, token)) return false;

        // if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(StateHandler.IsAnItemRightClicked, token))
        // {
        //     Log.Error( $"Failed to select {currency}: Timeout while waiting for currency to be right clicked.");
        //     return false;
        // }

        Log.Debug( $"Successfully selected {currency}.");
        StateHandler.CurrentlySelectedCurrency = currency;
        return true;
    }

    public static async SyncTask<bool> SelectCurrencyInCurrencyStash(string currency, CancellationToken token)
    {
        var currencyLocation = HelperHandler.GetRandomPointInRectangleF(StashHandler.GetClientRectForCurrencyInStash(currency));

        if (!await Main.InputController.Click(MouseButtons.Right, currencyLocation, token)) return false;

        // if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(StateHandler.IsAnItemRightClicked, token))
        // {
        //     Log.Error( $"Failed to select {currency}: Timeout while waiting for currency to be right clicked.");
        //     return false;
        // }

        Log.Debug( $"Successfully selected {currency}.");
        StateHandler.CurrentlySelectedCurrency = currency;
        return true;
    }

    public static async SyncTask<bool> ClickOnItemOrUI(RectangleF clientRect, CancellationToken token)
    {
        if (StateHandler.IsCursorOverRectangle(clientRect))
        {
            await Task.Delay(Main.InputController.GenerateDelay(), token); // Delay here to not spam too quickly if at the right position

            if (!await Main.InputController.Click(MouseButtons.Left, token)) return false;
        }
        else
        {
            var pointToClick = HelperHandler.GetRandomPointInRectangleF(clientRect);

            if (!await Main.InputController.Click(MouseButtons.Left, pointToClick, token)) return false;
        }

        Log.Debug( $"Successfully clicked on item or UI at {clientRect}.");
        return true;
    }

    public static async SyncTask<bool> EnterSearchString(string stringToEnter, CancellationToken token)
    {
        try
        {
            ImGui.SetClipboardText(stringToEnter);

            // Idk how to set the value without simulating button presses
            if (!await Main.InputController.KeyDown(Keys.LControlKey, token)) return false;

            if (!await Main.InputController.KeyDown(Keys.F, token)) return false;

            if (!await Main.InputController.KeyUp(Keys.F, false, token)) return false;

            if (!await Main.InputController.KeyDown(Keys.V, token)) return false;

            if (!await Main.InputController.KeyUp(Keys.V, false, token)) return false;

            if (!await Main.InputController.KeyUp(Keys.LControlKey, false, token)) return false;

            ImGui.SetClipboardText(string.Empty);

            // Wait for the search field to be filled
            if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                () => HarvestBenchHandler.HarvestSearchFieldText == stringToEnter || CraftingBenchHandler.CraftingBenchSearchFieldText == stringToEnter, token))
            {
                Log.Error( $"Failed to enter \"{stringToEnter}\" in the search field.");
                return false;
            }

            Log.Debug( $"Successfully entered \"{stringToEnter}\" in the search field.");
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}