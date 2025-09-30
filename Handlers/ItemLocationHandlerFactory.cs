using System;
using System.Linq;
using System.Threading;
using ExileCore.Shared;
using MyLittleCrafter.Enums;
using SharpDX;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;

namespace MyLittleCrafter.Handlers;

/// <summary>
/// Factory for creating item location handler instances
/// </summary>
public static class ItemLocationHandlerFactory
{
    /// <summary>
    /// Gets the appropriate handler for the specified location
    /// </summary>
    public static IItemLocationHandler GetHandler(ItemLocation location)
    {
        return location switch
        {
            ItemLocation.PlayerInventory => new PlayerInventoryLocationHandler(),
            ItemLocation.HarvestBench => new HarvestBenchLocationHandler(),
            ItemLocation.CraftingBench => new CraftingBenchLocationHandler(),
            ItemLocation.CurrencyStash => new CurrencyStashLocationHandler(),
            ItemLocation.InputStash => new InputStashLocationHandler(),
            ItemLocation.OutputStash => new OutputStashLocationHandler(),
            _ => throw new ArgumentException($"No handler available for location: {location}", nameof(location))
        };
    }
}

/// <summary>
/// Handler for PlayerInventory location
/// </summary>
internal class PlayerInventoryLocationHandler : IItemLocationHandler
{
    public InventSlotItem GetItem(RectangleF clientRect)
    {
        return PlayerInventoryHandler.GetInventSlotItemFromClientRectInPlayerInventory(clientRect);
    }

    public async SyncTask<bool> WaitForItem(RectangleF clientRect, CancellationToken token)
    {
        Logger.Log(LogType.Info, $"[TRACE] PlayerInventoryLocationHandler.WaitForItem called with ClientRect: {clientRect}");
        
        var result = await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            () =>
            {
                var item = GetItem(clientRect);
                var found = item != null && item.Item != null;
                if (!found)
                {
                    // Log what items ARE in inventory
                    var allItems = PlayerInventoryHandler.CraftableInventSlotItemsInPlayerInventory;
                    Logger.Log(LogType.Info, $"[TRACE] PlayerInventory WaitForItem: Looking for item at {clientRect.Center}. Found {allItems.Count} total items in inventory");
                    foreach (var invItem in allItems.Take(3))  // Log first 3 for debugging
                    {
                        var itemRect = invItem.GetClientRect();
                        Logger.Log(LogType.Info, $"[TRACE] PlayerInventory has item at center: {itemRect.Center}, comparing to target: {clientRect.Center}");
                    }
                }
                return found;
            },
            token);
        
        Logger.Log(LogType.Info, $"[TRACE] PlayerInventoryLocationHandler.WaitForItem result: {result}");
        return result;
    }

    public RectangleF GetStandardRect()
    {
        // PlayerInventory doesn't have a standard rect - it varies per item
        return RectangleF.Empty;
    }
}

/// <summary>
/// Handler for HarvestBench location
/// </summary>
internal class HarvestBenchLocationHandler : IItemLocationHandler
{
    public InventSlotItem GetItem(RectangleF clientRect)
    {
        return HarvestBenchHandler.InventSlotItemInHarvestBench;
    }

    public async SyncTask<bool> WaitForItem(RectangleF clientRect, CancellationToken token)
    {
        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            () =>
            {
                var item = HarvestBenchHandler.InventSlotItemInHarvestBench;
                return item != null && item.Item != null;
            },
            token);
    }

    public RectangleF GetStandardRect()
    {
        return HarvestBenchHandler.ItemInHarvestBenchRect;
    }
}

/// <summary>
/// Handler for CraftingBench location
/// </summary>
internal class CraftingBenchLocationHandler : IItemLocationHandler
{
    public InventSlotItem GetItem(RectangleF clientRect)
    {
        return CraftingBenchHandler.InventSlotItemInCraftingBench;
    }

    public async SyncTask<bool> WaitForItem(RectangleF clientRect, CancellationToken token)
    {
        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            () =>
            {
                var item = CraftingBenchHandler.InventSlotItemInCraftingBench;
                return item != null && item.Item != null;
            },
            token);
    }

    public RectangleF GetStandardRect()
    {
        return CraftingBenchHandler.ItemInCraftingBenchRect;
    }
}

/// <summary>
/// Handler for CurrencyStash location
/// Special handling for stash items which return NormalInventoryItem instead of InventSlotItem
/// </summary>
internal class CurrencyStashLocationHandler : IItemLocationHandler
{
    public InventSlotItem GetItem(RectangleF clientRect)
    {
        // Currency stash uses a different retrieval method
        // We need to get the first craftable item and convert it
        var item = StashHandler.GetFirstCraftableItemInVisibleStash();
        if (item == null) return null;
        
        // Note: This returns null because NormalInventoryItem can't be directly converted to InventSlotItem
        // The calling code should handle this special case
        return null;
    }

    public async SyncTask<bool> WaitForItem(RectangleF clientRect, CancellationToken token)
    {
        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            () =>
            {
                var item = StashHandler.GetFirstCraftableItemInVisibleStash();
                return item != null && item.Item != null;
            },
            token);
    }

    public RectangleF GetStandardRect()
    {
        var item = StashHandler.GetFirstCraftableItemInVisibleStash();
        return item?.GetClientRect() ?? RectangleF.Empty;
    }
}

/// <summary>
/// Handler for InputStash location
/// </summary>
internal class InputStashLocationHandler : IItemLocationHandler
{
    public InventSlotItem GetItem(RectangleF clientRect)
    {
        return StashHandler.CraftableInventSlotItemsInInputStash
            .FirstOrDefault(item => item.GetClientRect().Center == clientRect.Center);
    }

    public async SyncTask<bool> WaitForItem(RectangleF clientRect, CancellationToken token)
    {
        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            () =>
            {
                var item = GetItem(clientRect);
                return item != null && item.Item != null;
            },
            token);
    }

    public RectangleF GetStandardRect()
    {
        // InputStash doesn't have a single standard rect
        return RectangleF.Empty;
    }
}

/// <summary>
/// Handler for OutputStash location
/// Items moved here are finished and don't need further retrieval
/// </summary>
internal class OutputStashLocationHandler : IItemLocationHandler
{
    public InventSlotItem GetItem(RectangleF clientRect)
    {
        // Output stash doesn't need item retrieval - items are finished
        return null;
    }

    public async SyncTask<bool> WaitForItem(RectangleF clientRect, CancellationToken token)
    {
        // Output stash doesn't need waiting - items are done crafting
        return await System.Threading.Tasks.Task.FromResult(true);
    }

    public RectangleF GetStandardRect()
    {
        // Output stash doesn't have a tracked rect - items are finished
        return RectangleF.Empty;
    }
}

