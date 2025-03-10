using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using static MyLittleCrafter.Enums.MyLittleCrafter;

namespace MyLittleCrafter.Handlers;

public static class PlayerInventoryHandler
{
    public readonly static InventorySlotE PlayerInventory = InventorySlotE.MainInventory1;
    public static bool IsPlayerInventoryPanelOpen => StateHandler.IsInGameUiElementVisible(ui => ui.InventoryPanel);
    public static ServerInventory PlayerInventoryServerInventory => InventoryHandler.GetServerInventoryFromInventorySlotE(PlayerInventory);
    public static int PlayerInventoryServerRequestCounter => PlayerInventoryServerInventory.ServerRequestCounter;
    public static List<InventSlotItem> CraftableInventSlotItemsInPlayerInventory =>
        InventoryHandler.GetCraftableInventSlotItemsFromServerInventory(PlayerInventoryServerInventory)
        .OrderBy(item => item.PosX)
        .ThenBy(item => item.PosY)
        .ToList();

    public static bool PlayerInventoryCurrencyListCheck()
    {
        var missingCurrencies = StateHandler.RequiredCurrenciesForSelectedCraft
            .Where(currency => !IsCurrencyAvailableInPlayerInventory(currency))
            .ToList();
        return missingCurrencies.Count == 0;
    }

    public static bool IsCurrencyAvailableInPlayerInventory(string currency)
    {
        var isAvailable = InventoryHandler.GetAllSpecificCurrencyFromServerInventory(PlayerInventoryServerInventory, currency).Count != 0;

        if (isAvailable) Logger.Log(LogType.Debug, $"{currency} is available in player inventory.");
        else Logger.Log(LogType.Error, $"{currency} is not available in player inventory.");

        return isAvailable;
    }

    public static InventSlotItem GetInventSlotItemFromClientRectInPlayerInventory(RectangleF rect) =>
        InventoryHandler.GetCraftableInventSlotItemsFromServerInventory(PlayerInventoryServerInventory)
            .FirstOrDefault(item => item.Item.Address != 0
                                    && item.GetClientRect().Center == rect.Center);
}

