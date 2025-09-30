using System;
using System.Threading;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ItemFilterLibrary;
using MyLittleCrafter.Enums;
using MyLittleCrafter.Handlers;
using SharpDX;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Items;

/// <summary>
/// Represents a craftable item with its location and data.
/// Tracks the item as it moves between different locations (inventory, benches, stash).
/// Uses a state machine to enforce valid location transitions.
/// </summary>
public class CraftingBase
{
    private readonly CraftingBaseStateMachine _stateMachine;
    private IItemLocationHandler _currentHandler;

    public ItemData ItemData { get; set; }
    public ItemLocation ItemLocation => _stateMachine.CurrentLocation;
    public RectangleF ClientRect { get; internal set; }

    public CraftingBase(InventSlotItem inventSlotItem, ItemLocation itemLocation)
    {
        ItemData = new ItemData(inventSlotItem.Item, Main.GameController);
        _stateMachine = new CraftingBaseStateMachine(itemLocation);
        _currentHandler = ItemLocationHandlerFactory.GetHandler(itemLocation);

        // Set initial client rect based on location
        ClientRect = itemLocation switch
        {
            ItemLocation.PlayerInventory => inventSlotItem.GetClientRect(),
            ItemLocation.InputStash => StashHandler.GetClientRectForInventSlotItemInInputStash(inventSlotItem),
            _ => _currentHandler.GetStandardRect()
        };
    }

    public async SyncTask<bool> UpdateItemDataAsync(CancellationToken token)
    {
        Logger.Log(LogType.Info, $"[TRACE] UpdateItemDataAsync called. ItemLocation: {ItemLocation}, ClientRect: {ClientRect}");
        Entity newItem;

        // Special handling for CurrencyStash due to NormalInventoryItem type
        if (ItemLocation == ItemLocation.CurrencyStash)
        {
            Logger.Log(LogType.Info, $"[TRACE] UpdateItemDataAsync: CurrencyStash path - waiting for item");
            if (!await _currentHandler.WaitForItem(ClientRect, token))
            {
                Logger.Log(LogType.Error, $"UpdateItemData: Timeout waiting for craftable item in {ItemLocation}.");
                return false;
            }

            var inventoryItem = StashHandler.GetFirstCraftableItemInVisibleStash();
            newItem = inventoryItem.Item;
        }
        else
        {
            Logger.Log(LogType.Info, $"[TRACE] UpdateItemDataAsync: Standard path - waiting for item at {ItemLocation}");
            // Standard handling using handler interface
            if (!await _currentHandler.WaitForItem(ClientRect, token))
            {
                Logger.Log(LogType.Error, $"UpdateItemData: Timeout waiting for craftable item in {ItemLocation}.");
                return false;
            }

            Logger.Log(LogType.Info, $"[TRACE] UpdateItemDataAsync: WaitForItem succeeded, getting item");
            var item = _currentHandler.GetItem(ClientRect);
            if (item == null)
            {
                Logger.Log(LogType.Error, $"UpdateItemData: Item not found at {ItemLocation}.");
                return false;
            }

            newItem = item.Item;
        }

        if (newItem == null)
        {
            Logger.Log(LogType.Error, $"UpdateItemData: Entity is null at {ItemLocation}.");
            return false;
        }

        ItemData = new ItemData(newItem, Main.GameController);
        Logger.Log(LogType.Info, $"[TRACE] UpdateItemDataAsync: Successfully updated item data at {ItemLocation}.");
        return true;
    }

    /// <summary>
    /// Moves the item to a new location with state machine validation
    /// </summary>
    private bool TransitionTo(ItemLocation targetLocation)
    {
        if (!_stateMachine.TryTransitionTo(targetLocation, out var errorMessage))
        {
            Logger.Log(LogType.Error, $"Invalid state transition: {errorMessage}");
            return false;
        }

        _currentHandler = ItemLocationHandlerFactory.GetHandler(targetLocation);
        return true;
    }

    /// <summary>
    /// Updates the location and rect when the item is moved to the harvest bench
    /// </summary>
    /// <returns>True if transition was successful, false if invalid (should stop crafting)</returns>
    public bool OnMovedToHarvestBench()
    {
        if (!TransitionTo(ItemLocation.HarvestBench))
        {
            Logger.Log(LogType.Error, $"Cannot move to HarvestBench from {ItemLocation}. Stopping crafting.");
            return false;
        }

        ClientRect = _currentHandler.GetStandardRect();
        return true;
    }

    /// <summary>
    /// Updates the location and rect when the item is moved to the crafting bench
    /// </summary>
    /// <returns>True if transition was successful, false if invalid (should stop crafting)</returns>
    public bool OnMovedToCraftingBench()
    {
        if (!TransitionTo(ItemLocation.CraftingBench))
        {
            Logger.Log(LogType.Error, $"Cannot move to CraftingBench from {ItemLocation}. Stopping crafting.");
            return false;
        }

        ClientRect = _currentHandler.GetStandardRect();
        return true;
    }

    /// <summary>
    /// Updates the location and rect when the item is moved to the currency stash
    /// </summary>
    /// <returns>True if transition was successful, false if invalid (should stop crafting)</returns>
    public bool OnMovedToCurrencyStash()
    {
        if (!TransitionTo(ItemLocation.CurrencyStash))
        {
            Logger.Log(LogType.Error, $"Cannot move to CurrencyStash from {ItemLocation}. Stopping crafting.");
            return false;
        }

        var firstItem = StashHandler.GetFirstCraftableItemInVisibleStash();
        if (firstItem == null)
        {
            Logger.Log(LogType.Error, "OnMovedToCurrencyStash: No craftable item found in stash.");
            return false;
        }

        ClientRect = firstItem.GetClientRect();
        return true;
    }

    /// <summary>
    /// Updates the location and rect when the item is moved to player inventory
    /// </summary>
    /// <returns>True if transition was successful, false if invalid (should stop crafting)</returns>
    public bool OnMovedToPlayerInventory(InventSlotItem inventSlotItem)
    {
        if (inventSlotItem == null)
        {
            Logger.Log(LogType.Error, "OnMovedToPlayerInventory: InventSlotItem is null.");
            return false;
        }

        if (!TransitionTo(ItemLocation.PlayerInventory))
        {
            Logger.Log(LogType.Error, $"Cannot move to PlayerInventory from {ItemLocation}. Stopping crafting.");
            return false;
        }

        ClientRect = inventSlotItem.GetClientRect();
        return true;
    }

    /// <summary>
    /// Updates the location when the item is moved to output stash (finished crafting)
    /// </summary>
    /// <returns>True if transition was successful, false if invalid (should stop crafting)</returns>
    public bool OnMovedToOutputStash()
    {
        if (!TransitionTo(ItemLocation.OutputStash))
        {
            Logger.Log(LogType.Error, $"Cannot move to OutputStash from {ItemLocation}. Stopping crafting.");
            return false;
        }

        // No client rect needed - item is finished and won't be retrieved again
        ClientRect = RectangleF.Empty;
        Logger.Log(LogType.Info, "Item moved to OutputStash - crafting complete.");
        return true;
    }
}
