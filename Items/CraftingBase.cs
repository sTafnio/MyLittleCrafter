using ExileCore.PoEMemory.MemoryObjects;
using ItemFilterLibrary;
using MyLittleCrafter.Handlers;
using SharpDX;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using static MyLittleCrafter.Enums.MyLittleCrafter;
using static MyLittleCrafter.MyLittleCrafter;
using System.Threading;
using ExileCore.Shared;

namespace MyLittleCrafter.Items;

public class CraftingBase
{
    public ItemData ItemData { get; set; }
    public ItemLocation ItemLocation { get; set; }
    public RectangleF ClientRect { get; set; }

    public CraftingBase(InventSlotItem inventSlotItem, ItemLocation itemLocation)
    {
        ItemData = new ItemData(inventSlotItem.Item, Main.GameController);
        ItemLocation = itemLocation;

        switch (itemLocation)
        {
            case ItemLocation.PlayerInventory:
                ClientRect = inventSlotItem.GetClientRect();
                break;
            case ItemLocation.HarvestBench:
                ClientRect = HarvestBenchHandler.ItemInHarvestBenchRect;
                break;
            case ItemLocation.CraftingBench:
                ClientRect = CraftingBenchHandler.ItemInCraftingBenchRect;
                break;
            case ItemLocation.InputStash:
                ClientRect = StashHandler.GetClientRectForInventSlotItemInInputStash(inventSlotItem);
                break;
        }
    }

    public async SyncTask<bool> UpdateItemDataAsync(CancellationToken token)
    {
        Entity newItem;

        switch (ItemLocation)
        {
            case ItemLocation.PlayerInventory:
                // Wait for a valid item to be available in player inventory
                if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                    () =>
                    {
                        var item = PlayerInventoryHandler.GetInventSlotItemFromClientRectInPlayerInventory(ClientRect);
                        return item != null && item.Item != null;
                    },
                    StateHandler.Timeout,
                    token))
                {
                    Logger.Log(LogType.Error, "UpdateItemData: Timeout waiting for craftable item in player inventory.");
                    return false;
                }

                newItem = PlayerInventoryHandler.GetInventSlotItemFromClientRectInPlayerInventory(ClientRect).Item;
                break;

            case ItemLocation.HarvestBench:
                // Wait for a valid item to be available in harvest bench
                if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                    () =>
                    {
                        var item = HarvestBenchHandler.InventSlotItemInHarvestBench;
                        return item != null && item.Item != null;
                    },
                    StateHandler.Timeout,
                    token))
                {
                    Logger.Log(LogType.Error, "UpdateItemData: Timeout waiting for craftable item in harvest bench.");
                    return false;
                }

                newItem = HarvestBenchHandler.InventSlotItemInHarvestBench.Item;
                break;

            case ItemLocation.CraftingBench:
                // Wait for a valid item to be available in crafting bench
                if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                    () =>
                    {
                        var item = CraftingBenchHandler.InventSlotItemInCraftingBench;
                        return item != null && item.Item != null;
                    },
                    StateHandler.Timeout,
                    token))
                {
                    Logger.Log(LogType.Error, "UpdateItemData: Timeout waiting for craftable item in crafting bench.");
                    return false;
                }

                newItem = CraftingBenchHandler.InventSlotItemInCraftingBench.Item;
                break;

            case ItemLocation.CurrencyStash:
                // Wait for a valid item to be available in the currency stash
                if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                    () =>
                    {
                        var item = StashHandler.GetFirstCraftableItemInVisibleStash();
                        return item != null && item.Item != null;
                    },
                    StateHandler.Timeout,
                    token))
                {
                    Logger.Log(LogType.Error, "UpdateItemData: Timeout waiting for craftable item in currency stash.");
                    return false;
                }

                var inventSlotItem = StashHandler.GetFirstCraftableItemInVisibleStash();
                newItem = inventSlotItem.Item;
                break;

            default:
                Logger.Log(LogType.Error, $"UpdateItemData: Invalid ItemLocation value: {ItemLocation}");
                return false;
        }

        ItemData = new ItemData(newItem, Main.GameController);
        Logger.Log(LogType.Debug, $"Successfully updated item data.");
        return true;
    }

    public void OnMovedToHarvestBench()
    {
        ItemLocation = ItemLocation.HarvestBench;
        ClientRect = HarvestBenchHandler.ItemInHarvestBenchRect;
    }

    public void OnMovedToCraftingBench()
    {
        ItemLocation = ItemLocation.CraftingBench;
        ClientRect = CraftingBenchHandler.ItemInCraftingBenchRect;
    }

    public void OnMovedToCurrencyStash()
    {
        ItemLocation = ItemLocation.CurrencyStash;
        ClientRect = StashHandler.GetFirstCraftableItemInVisibleStash().GetClientRect();
    }

    public void OnMovedToPlayerInventory(InventSlotItem inventSlotItem)
    {
        ItemLocation = ItemLocation.PlayerInventory;
        ClientRect = inventSlotItem.GetClientRect();
    }
}
