using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ImGuiNET;
using MyLittleCrafter.IFL;
using MyLittleCrafter.Items;
using SharpDX;
using static MyLittleCrafter.Enums.MyLittleCrafter;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Handlers;

public static class CraftingHandler
{
    public static async SyncTask<bool> ShiftUpIfDown(CancellationToken token)
    {
        // Release Shift if held down
        if (Input.IsKeyDown(Keys.LShiftKey))
        {
            if (!await Main.InputController.KeyUp(Keys.LShiftKey, false, token)) return false;
            StateHandler.CurrentlySelectedCurrency = string.Empty;
        }

        return true;
    }

    public static async SyncTask<bool> ApplyCurrency(
        CraftingBase craftingBase,
        EvaluationResult evaluation,
        ItemLocation currencySource,
        CancellationToken token)
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
                if (!PlayerInventoryHandler.IsCurrencyAvailableInPlayerInventory(evaluation.CurrencyOrCraftName))
                {
                    Logger.Log(LogType.Error, $"Currency {evaluation.CurrencyOrCraftName} not available in player inventory.");
                    return false;
                }
            }
            else if (currencySource == ItemLocation.CurrencyStash)
            {
                // Get initial server request counter for currency stash
                initialServerRequestCounter = StashHandler.CurrencyStashServerRequestCounter;

                // Currency availability check in currency stash
                if (!StashHandler.IsCurrencyAvailableInCurrencyStash(evaluation.CurrencyOrCraftName))
                {
                    Logger.Log(LogType.Error, $"Currency {evaluation.CurrencyOrCraftName} not available in currency stash.");
                    return false;
                }
            }
            else
            {
                Logger.Log(LogType.Error, $"Unsupported currency source: {currencySource}");
                return false;
            }

            // If wrong currency is selected, deselect it
            if (StateHandler.CurrentlySelectedCurrency != string.Empty && StateHandler.CurrentlySelectedCurrency != evaluation.CurrencyOrCraftName)
            {
                if (!await DeselectCurrency(StateHandler.CurrentlySelectedCurrency, token)) return false;
            }

            // If nothing selected
            if (StateHandler.CurrentlySelectedCurrency == string.Empty)
            {
                // If using shift before selecting currency
                if (evaluation.UseShift)
                {
                    if (!await Main.InputController.KeyDown(Keys.LShiftKey, token)) return false;
                }

                // Select currency based on source
                if (currencySource == ItemLocation.PlayerInventory)
                {
                    if (!await SelectCurrency(PlayerInventoryHandler.PlayerInventoryServerInventory, craftingBase, evaluation.CurrencyOrCraftName, token)) return false;
                }
                else // CurrencyStash
                {
                    if (!await SelectCurrencyInCurrencyStash(evaluation.CurrencyOrCraftName, token)) return false;
                }
            }

            // Correct currency selected
            if (StateHandler.CurrentlySelectedCurrency == evaluation.CurrencyOrCraftName)
            {
                if (!await ClickOnItemOrUI(craftingBase.ClientRect, token)) return false;
            }

            // Wait for the server request counter to be updated based on source

            if (currencySource == ItemLocation.PlayerInventory)
            {
                if (!await InventoryHandler.WaitForInventoryToUpdate(PlayerInventoryHandler.PlayerInventoryServerInventory, initialServerRequestCounter, 2, token))
                {
                    Logger.Log(LogType.Error, $"Timeout while waiting for player inventory to update.");
                    return false;
                }
            }
            else // CurrencyStash
            {
                if (!await StashHandler.WaitForCurrencyStashToUpdate(initialServerRequestCounter, StateHandler.Timeout, token))
                {
                    Logger.Log(LogType.Error, $"Timeout while waiting for server request counter to update.");
                    return false;
                }
            }

            Logger.Log(LogType.Debug, $"Successfully applied {evaluation.CurrencyOrCraftName}.");
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public static async SyncTask<bool> UseCraft(
    EvaluationResult evaluationResult,
    ConditionType craftType,
    CancellationToken token = default)
    {
        try
        {
            int initialServerRequestCounter;
            string searchString = evaluationResult.CurrencyOrCraftName;

            // Get appropriate parameters based on craft type
            if (craftType == ConditionType.HarvestBenchCraft)
            {
                initialServerRequestCounter = HarvestBenchHandler.HarvestBenchServerRequestCounter;
            }
            else if (craftType == ConditionType.CraftingBenchCraft)
            {
                initialServerRequestCounter = CraftingBenchHandler.CraftingBenchServerRequestCounter;
                searchString = evaluationResult.CurrencyOrCraftName.Replace("Craft", "").Trim();
            }
            else
            {
                Logger.Log(LogType.Error, $"Unsupported craft type: {craftType}.");
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
                Logger.Log(LogType.Debug, $"Successfully selected {evaluationResult.CurrencyOrCraftName}.");
            }

            // Use the craft
            var craftButtonRect = craftType == ConditionType.HarvestBenchCraft
                                ? HarvestBenchHandler.HarvestCraftButtonRect
                                : CraftingBenchHandler.CraftingBenchCraftButtonRect;
            if (!await ClickOnItemOrUI(craftButtonRect, token)) return false;


            // Wait for the server request counter to be updated
            var inventory = craftType == ConditionType.HarvestBenchCraft
                            ? HarvestBenchHandler.HarvestBenchServerInventory
                            : CraftingBenchHandler.CraftingBenchServerInventory;
            if (!await InventoryHandler.WaitForInventoryToUpdate(
                inventory,
                initialServerRequestCounter,
                StateHandler.Timeout,
                token))
            {
                Logger.Log(LogType.Error, $"Timeout while waiting for server request counter to update.");
                return false;
            }

            Logger.Log(LogType.Debug, $"Successfully used {evaluationResult.CurrencyOrCraftName}.");
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public static async SyncTask<bool> RemoveItemFromInventory(ServerInventory serverInventory, RectangleF rect, CancellationToken token)
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
            if (!await InventoryHandler.WaitForInventoryToUpdate(serverInventory, initialServerRequestCounter, StateHandler.Timeout, token))
            {
                Logger.Log(LogType.Error, $"Timeout while waiting for player inventory to update. Could not remove item.");
                return false;
            }

            // Release Ctrl at the end
            if (!await Main.InputController.KeyUp(Keys.LControlKey, false, token)) return false;

            Logger.Log(LogType.Debug, $"Successfully removed item.");
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
            if (!await InventoryHandler.WaitForInventoryToUpdate(PlayerInventoryHandler.PlayerInventoryServerInventory, initialServerRequestCounter, StateHandler.Timeout, token))
            {
                Logger.Log(LogType.Error, $"Timeout while waiting for server request counter to update.");
                return false;
            }

            Logger.Log(LogType.Debug, $"Successfully removed item from visible stash.");
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
        if (!await RemoveItemFromInventory(PlayerInventoryHandler.PlayerInventoryServerInventory, craftingBase.ClientRect, token)) return false;

        Logger.Log(LogType.Debug, $"Successfully move crafting base from {startIndex} to {endIndex}.");
        return true;
    }

    public static async SyncTask<bool> MoveToStashIndex(int targetStashIndex, CancellationToken token)
    {
        try
        {
            if (StashHandler.IsStashAtIndexVisible(targetStashIndex))
            {
                Logger.Log(LogType.Debug, $"Already on desired stash tab at index {targetStashIndex}.");
                return true;
            }

            while (!StashHandler.IsStashAtIndexVisible(targetStashIndex) && !token.IsCancellationRequested)
            {
                var initialIndex = StashHandler.CurrentVisibleStashIndex;

                // If visible index is less than target index, move right
                if (StashHandler.CurrentVisibleStashIndex < targetStashIndex)
                {
                    if (!await Main.InputController.KeyDown(Keys.Right, token)) return false;
                    if (!await Main.InputController.KeyUp(Keys.Right, false, token)) return false;
                }

                // If visible index is greater than target index, move left
                else
                {
                    if (!await Main.InputController.KeyDown(Keys.Left, token)) return false;
                    if (!await Main.InputController.KeyUp(Keys.Left, false, token)) return false;
                }

                // Wait for the visible stash index to change
                if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                    () => StashHandler.CurrentVisibleStashIndex != initialIndex,
                    StateHandler.Timeout,
                    token
                ))
                {
                    Logger.Log(LogType.Error, $"Failed to move to next stash tab. Timeout while waiting for stash index to change.");
                    return false;
                }

                Logger.Log(LogType.Debug, $"Moved to stash tab at index {StashHandler.CurrentVisibleStashIndex}.");
            }

            Logger.Log(LogType.Debug, $"Successfully moved to desired stash tab at index {targetStashIndex}.");
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

            if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(StateHandler.IsCursorFree, StateHandler.Timeout, token))
            {
                Logger.Log(LogType.Error, $"Failed to deselect {currency}: Timeout while waiting for cursor to be free.");
                return false;
            }

            Logger.Log(LogType.Debug, $"Successfully deselected {currency}.");
            StateHandler.CurrentlySelectedCurrency = string.Empty;

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    // This whole chain of methods relies on the fact that availability was already checked
    // Do NOT use if availability is not checked
    public static async SyncTask<bool> SelectCurrency(ServerInventory serverInventory, CraftingBase craftingBase, string currency, CancellationToken token)
    {
        var currencyLocation = InventoryHandler.GetRandomPointForClosestCurrencyInAnInventory(serverInventory, craftingBase, currency);

        if (!await Main.InputController.Click(MouseButtons.Right, currencyLocation, token)) return false;

        if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(StateHandler.IsAnItemRightClicked, StateHandler.Timeout, token))
        {
            Logger.Log(LogType.Error, $"Failed to select {currency}: Timeout while waiting for currency to be right clicked.");
            return false;
        }

        Logger.Log(LogType.Debug, $"Successfully selected {currency}.");
        StateHandler.CurrentlySelectedCurrency = currency;

        return true;
    }

    public static async SyncTask<bool> SelectCurrencyInCurrencyStash(string currency, CancellationToken token)
    {
        var currencyLocation = HelperHandler.GetRandomPointInRectangleF(StashHandler.GetClientRectForCurrencyInStash(currency));

        if (!await Main.InputController.Click(MouseButtons.Right, currencyLocation, token)) return false;

        if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(StateHandler.IsAnItemRightClicked, StateHandler.Timeout, token))
        {
            Logger.Log(LogType.Error, $"Failed to select {currency}: Timeout while waiting for currency to be right clicked.");
            return false;
        }

        Logger.Log(LogType.Debug, $"Successfully selected {currency}.");
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

        Logger.Log(LogType.Debug, $"Successfully clicked on item or UI at {clientRect}.");
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
                () => HarvestBenchHandler.HarvestSearchFieldText == stringToEnter || CraftingBenchHandler.CraftingBenchSearchFieldText == stringToEnter,
                StateHandler.Timeout,
                token
            ))
            {
                Logger.Log(LogType.Error, $"Failed to enter \"{stringToEnter}\" in the search field.");
                return false;
            }

            Logger.Log(LogType.Debug, $"Successfully entered \"{stringToEnter}\" in the search field.");
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }








    // public static async SyncTask<bool> ApplyCurrencyFromInventory(InventorySlotE invSlot, CraftingBase craftingBase, EvaluationResult evaluation, CancellationToken token)
    // {
    //     try
    //     {
    //         // Get initial server request counter
    //         if (!InventoryHandler.TryGetServerRequestCounterForAnInventory(invSlot, out var initialServerRequestCounter)) return false;

    //         // Currency availability check
    //         if (!PlayerInventoryHandler.IsCurrencyAvailableInPlayerInventory(evaluation.CurrencyOrCraftName)) return false;

    //         // If wrong currency is selected, deselect it
    //         if (StateHandler.CurrentlySelectedCurrency != string.Empty && StateHandler.CurrentlySelectedCurrency != evaluation.CurrencyOrCraftName)
    //         {
    //             if (!await DeselectCurrency(StateHandler.CurrentlySelectedCurrency, token)) return false;
    //         }

    //         // If nothing selected
    //         if (StateHandler.CurrentlySelectedCurrency == string.Empty)
    //         {
    //             // If using shift before selecting currency
    //             if (evaluation.UseShift)
    //             {
    //                 if (!await Main.InputController.KeyDown(Keys.LShiftKey, token)) return false;
    //             }

    //             // Select currency
    //             if (!await SelectCurrency(invSlot, craftingBase, evaluation.CurrencyOrCraftName, token)) return false;
    //         }

    //         // Correct currency selected
    //         if (StateHandler.CurrentlySelectedCurrency == evaluation.CurrencyOrCraftName)
    //         {
    //             if (!await ClickOnItemOrUI(craftingBase.ClientRect, token)) return false;
    //         }

    //         // Wait for the server request counter to be updated
    //         if (!await InventoryHandler.WaitForInventoryToUpdate(invSlot, initialServerRequestCounter, 2, token))
    //         {
    //             Logger.Log(LogType.Error, $"Timeout while waiting for player inventory to update. Could not apply {evaluation.CurrencyOrCraftName}.");
    //             return false;
    //         }

    //         Logger.Log(LogType.Debug, $"Successfully applied {evaluation.CurrencyOrCraftName}.");
    //         return true;
    //     }
    //     catch (OperationCanceledException)
    //     {
    //         return false;
    //     }
    // }

    // public static async SyncTask<bool> ApplyCurrencyFromCurrencyStash(CraftingBase craftingBase, EvaluationResult evaluation, CancellationToken token)
    // {
    //     try
    //     {
    //         // Get initial server request counter for currency stash
    //         var initialServerRequestCounter = StashHandler.CurrencyStashServerRequestCounter;

    //         // Currency availability check
    //         if (!StashHandler.IsCurrencyAvailableInCurrencyStash(evaluation.CurrencyOrCraftName)) return false;

    //         // If wrong currency is selected, deselect it
    //         if (StateHandler.CurrentlySelectedCurrency != string.Empty && StateHandler.CurrentlySelectedCurrency != evaluation.CurrencyOrCraftName)
    //         {
    //             if (!await DeselectCurrency(StateHandler.CurrentlySelectedCurrency, token)) return false;
    //         }

    //         // If nothing selected
    //         if (StateHandler.CurrentlySelectedCurrency == string.Empty)
    //         {
    //             // If using shift before selecting currency
    //             if (evaluation.UseShift)
    //             {
    //                 if (!await Main.InputController.KeyDown(Keys.LShiftKey, token)) return false;
    //             }

    //             // Select currency
    //             if (!await SelectCurrencyInCurrencyStash(evaluation.CurrencyOrCraftName, token)) return false;
    //         }

    //         // Correct currency selected
    //         if (StateHandler.CurrentlySelectedCurrency == evaluation.CurrencyOrCraftName)
    //         {
    //             if (!await ClickOnItemOrUI(craftingBase.ClientRect, token)) return false;
    //         }

    //         // Wait for the server request counter to be updated
    //         if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(
    //                 () => StashHandler.CurrencyStashServerRequestCounter != initialServerRequestCounter,
    //                 StateHandler.Timeout,
    //                 token
    //             ))
    //         {
    //             Logger.Log(LogType.Error, $"Timeout while waiting for server request counter to update.");
    //             return false;
    //         }

    //         Logger.Log(LogType.Debug, $"Successfully applied {evaluation.CurrencyOrCraftName}.");
    //         return true;
    //     }
    //     catch (OperationCanceledException)
    //     {
    //         return false;
    //     }
    // }

    // public static async SyncTask<bool> UseHarvestCraft(EvaluationResult evaluationResult, CancellationToken token)
    // {
    //     try
    //     {
    //         // Get initial server request counter
    //         var initialServerRequestCounter = HarvestBenchHandler.HarvestBenchServerRequestCounter;

    //         // Release Shift if held down
    //         if (Input.IsKeyDown(Keys.LShiftKey))
    //         {
    //             if (!await Main.InputController.KeyUp(Keys.LShiftKey, false, token)) return false;
    //             StateHandler.CurrentlySelectedCurrency = string.Empty;
    //         }

    //         // Search string check
    //         if (string.IsNullOrEmpty(HarvestBenchHandler.HarvestSearchFieldText) || HarvestBenchHandler.HarvestSearchFieldText != evaluationResult.CurrencyOrCraftName)
    //         {
    //             if (!await EnterSearchString(evaluationResult.CurrencyOrCraftName, token)) return false;
    //             StateHandler.IsCraftSelected = false; // false here in case we use multiple reforge methods
    //         }

    //         // If craft not selected, select it
    //         if (!StateHandler.IsCraftSelected)
    //         {
    //             // xOffset was used here in a previous version
    //             if (!await ClickOnItemOrUI(HarvestBenchHandler.FirstHarvestCraftRect, token)) return false;
    //             StateHandler.IsCraftSelected = true;
    //             Logger.Log(LogType.Debug, $"Successfully selected {evaluationResult.CurrencyOrCraftName}.");
    //         }

    //         // Use the craft
    //         if (!await ClickOnItemOrUI(HarvestBenchHandler.HarvestCraftButtonRect, token)) return false;

    //         // Wait for the server request counter to be updated
    //         if (!await InventoryHandler.WaitForInventoryToUpdate(HarvestBenchHandler.HarvestBenchServerInventory, initialServerRequestCounter, StateHandler.Timeout, token))
    //         {
    //             Logger.Log(LogType.Error, $"Timeout while waiting for harvest bench inventory to update. Could not use {evaluationResult.CurrencyOrCraftName}.");
    //             return false;
    //         }

    //         Logger.Log(LogType.Debug, $"Successfully used {evaluationResult.CurrencyOrCraftName}.");
    //         return true;
    //     }
    //     catch (OperationCanceledException)
    //     {
    //         return false;
    //     }
    // }

    // public static async SyncTask<bool> UseCraftingBenchCraft(EvaluationResult evaluationResult, CancellationToken token)
    // {
    //     try
    //     {
    //         // Get initial server request counter
    //         var initialServerRequestCounter = CraftingBenchHandler.CraftingBenchServerRequestCounter;

    //         // Release Shift if held down
    //         if (Input.IsKeyDown(Keys.LShiftKey))
    //         {
    //             if (!await Main.InputController.KeyUp(Keys.LShiftKey, false, token)) return false;
    //             StateHandler.CurrentlySelectedCurrency = string.Empty;
    //         }

    //         // Search string check
    //         var searchString = evaluationResult.CurrencyOrCraftName.Replace("Craft", "").Trim();
    //         if (string.IsNullOrEmpty(CraftingBenchHandler.CraftingBenchSearchFieldText) || CraftingBenchHandler.CraftingBenchSearchFieldText != searchString)
    //         {
    //             if (!await EnterSearchString(searchString, token)) return false;
    //             StateHandler.IsCraftingBenchCraftSelected = false; // false here in case we use multiple bench crafts
    //         }

    //         // If craft not selected, select it
    //         if (!StateHandler.IsCraftingBenchCraftSelected)
    //         {
    //             if (!await ClickOnItemOrUI(CraftingBenchHandler.FirstCraftingBenchCraftRect, token)) return false;
    //             StateHandler.IsCraftingBenchCraftSelected = true;
    //             Logger.Log(LogType.Debug, $"Successfully selected {evaluationResult.CurrencyOrCraftName}.");
    //         }

    //         // Use the craft
    //         if (!await ClickOnItemOrUI(CraftingBenchHandler.CraftingBenchCraftButtonRect, token)) return false;

    //         // Wait for the server request counter to be updated
    //         if (!await InventoryHandler.WaitForInventoryToUpdate(CraftingBenchHandler.CraftingBenchServerInventory, initialServerRequestCounter, StateHandler.Timeout, token))
    //         {
    //             Logger.Log(LogType.Error, $"Timeout while waiting for crafting bench inventory to update. Could not use {evaluationResult.CurrencyOrCraftName}.");
    //             return false;
    //         }

    //         Logger.Log(LogType.Debug, $"Successfully used {evaluationResult.CurrencyOrCraftName}.");
    //         return true;
    //     }
    //     catch (OperationCanceledException)
    //     {
    //         return false;
    //     }
    // }
}




