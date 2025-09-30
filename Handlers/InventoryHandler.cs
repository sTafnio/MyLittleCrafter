using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using MyLittleCrafter.Items;
using Vector2 = System.Numerics.Vector2;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using MyLittleCrafter.Enums;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Handlers;

public static class InventoryHandler
{
    public static List<InventSlotItem> GetCraftableInventSlotItemsFromServerInventory(ServerInventory serverInventory) =>
        serverInventory.InventorySlotItems
            .Where(item => item.Item.IsValid
                           && item.Item.TryGetComponent<Base>(out var baseComp)
                           && baseComp != null && baseComp.Address != 0
                           && item.Item.TryGetComponent<Mods>(out var modComp)
                           && modComp != null && modComp.Address != 0)
            .OrderBy(item => item.PosX)
            .ThenBy(item => item.PosY)
            .ToList();

    public static ServerInventory GetServerInventoryFromInventorySlotE(InventorySlotE invSlot) =>
        Main?.GameController?.Game?.IngameState?.ServerData?.PlayerInventories[(int)invSlot]?.Inventory;

    public static List<InventSlotItem> GetAllSpecificCurrencyFromServerInventory(ServerInventory serverInventory, string currency) =>
        serverInventory.InventorySlotItems
            .Where(item => item.Item != null && item.Item.TryGetComponent<Base>(out var baseI) && baseI.Name == currency)
            .ToList();

    public static int GetServerRequestCounterForServerInventory(ServerInventory serverInventory)
    {
        var counter = serverInventory.ServerRequestCounter;
        return counter;
    }

    public static async SyncTask<bool> WaitForInventoryToUpdate(ServerInventory serverInventory, int initialServerRequestCounter, CancellationToken token)
    {        
        var result = await ExecuteHandler.AsyncExecuteWithCancellationHandling(() =>
        {
            var serverRequestCounter = GetServerRequestCounterForServerInventory(serverInventory);
            return serverRequestCounter != initialServerRequestCounter;
        }, token);
        
        return result;
    }

    public static Vector2 GetRandomPointForClosestCurrencyInAnInventory(ServerInventory serverInventory, CraftingBase craftingBase, string currency)
    {
        var closestCurrency = GetClosestCurrency(serverInventory, craftingBase, currency);
        var currencyClientRect = closestCurrency.GetClientRect();

        var randomPoint = HelperHandler.GetRandomPointInRectangleF(currencyClientRect);
        Logger.Log(LogType.Debug, $"Random point for {currency}: {randomPoint}");

        return randomPoint;
    }

    private static InventSlotItem GetClosestCurrency(ServerInventory serverInventory, CraftingBase craftingBase, string currency)
    {
        var closestCurrency = new InventSlotItem();
        var closestSquaredDistance = float.MaxValue;

        foreach (var item in GetAllSpecificCurrencyFromServerInventory(serverInventory, currency))
        {
            float squaredDistance = HelperHandler.CalculateSquaredDistance(craftingBase, item);

            if (squaredDistance < closestSquaredDistance)
            {
                closestSquaredDistance = squaredDistance;
                closestCurrency = item;
            }
        }

        return closestCurrency;
    }

    public static InventSlotItem GetRecentlyAddedInventSlotItem(List<InventSlotItem> oldItems, List<InventSlotItem> newItems)
    {
        var recentlyAddedItem = newItems.FirstOrDefault(item => !oldItems.Any(oldItem => oldItem.GetClientRect() == item.GetClientRect()));
        return recentlyAddedItem;
    }
}

