using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Enums;
using SharpDX;
using System.Collections.Generic;
using System.Linq;
using static MyLittleCrafter.MyLittleCrafter;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using ExileCore.PoEMemory.Components;
using MyLittleCrafter.Enums;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using System.Threading;
using ExileCore.Shared;

namespace MyLittleCrafter.Handlers;

public static class StashHandler
{
    public static StashElement StashElement => Main?.GameController?.Game?.IngameState?.IngameUi?.StashElement;
    public static bool IsStashPanelOpen => StateHandler.IsInGameUiElementVisible(ui => ui.StashElement);
    public static Inventory VisibleStashInventory => StashElement?.VisibleStash;
    public static List<string> AllStashNames => StashElement?.Inventories
        .Select(i => i.TabName)
        .ToList();

    // Input Stash
    public static int InputStashIndex => Main.Settings.StashOptions.InputStashIndex;
    public static InventoryType InputStashTabType => GetInventoryTypeForStashAtIndex(InputStashIndex);
    public static ServerInventory InputStashServerInventory => GetServerInventoryForStashAtIndex(InputStashIndex);
    public static bool InputStashIsQuadStash => InputStashTabType == InventoryType.QuadStash;
    public static RectangleF InputStashClientRect => GetStashAtIndex(InputStashIndex).Inventory.GetClientRect();

    // Currency Stash
    public static int CurrencyStashIndex => Main.Settings.StashOptions.CurrencyStashIndex;
    public static InventoryType CurrencyStashTabType => GetInventoryTypeForStashAtIndex(CurrencyStashIndex);
    public static ServerInventory CurrencyStashServerInventory => GetServerInventoryForStashAtIndex(CurrencyStashIndex);
    public static InventSlotItem NonCurrencyItemInCurrencyStash => InventoryHandler.GetCraftableInventSlotItemsFromServerInventory(CurrencyStashServerInventory).FirstOrDefault();
    public static int CurrencyStashServerRequestCounter => GetServerRequestCounterForStashAtIndex(CurrencyStashIndex);

    // Output Stash
    public static int OutputStashIndex => Main.Settings.StashOptions.OutputStashIndex;
    public static InventoryType OutputStashTabType => GetInventoryTypeForStashAtIndex(OutputStashIndex);


    public static int CurrentVisibleStashIndex => StashElement?.Inventories.FindIndex(inventory => inventory?.Inventory != null && inventory.Inventory.IsVisible) ?? -1;
    public static List<InventSlotItem> CraftableInventSlotItemsInInputStash => InventoryHandler.GetCraftableInventSlotItemsFromServerInventory(InputStashServerInventory);


    // Only used with currency stash, there can only be one craftable item in the currency stash
    public static NormalInventoryItem GetFirstCraftableItemInVisibleStash() =>
        VisibleStashInventory.VisibleInventoryItems
            .FirstOrDefault(item => item.Item.IsValid
                   && item.Entity.TryGetComponent<Base>(out var baseComp)
                   && baseComp != null && baseComp.Address != 0
                   && item.Entity.TryGetComponent<Mods>(out var modComp)
                   && modComp != null && modComp.Address != 0);

    public static bool CurrencyStashCurrencyListCheck() =>
        StateHandler.RequiredCurrenciesForSelectedCraft
            .Where(currency => !IsCurrencyAvailableInCurrencyStash(currency))
            .ToList()
            .Count == 0;

    public static bool IsCurrencyAvailableInCurrencyStash(string currency)
    {
        var isAvailable = InventoryHandler.GetAllSpecificCurrencyFromServerInventory(CurrencyStashServerInventory, currency).Count != 0;

        if (isAvailable) Logger.Log(LogType.Debug, $"{currency} is available in currency stash.");
        else Logger.Log(LogType.Error, $"{currency} is not available in currency stash.");

        return isAvailable;
    }

    public static ServerInventory GetServerInventoryForStashAtIndex(int index) => GetStashAtIndex(index).Inventory.ServerInventory;

    public static bool IsStashAtIndexVisible(int index) => CurrentVisibleStashIndex == index;

    public static RectangleF GetClientRectForCurrencyInStash(string currency)
    {
        var visibleInventory = Main.GameController?.Game?.IngameState?.IngameUi?.StashElement?.VisibleStash;

        if (visibleInventory == null) return RectangleF.Empty;

        return visibleInventory.VisibleInventoryItems
            .FirstOrDefault(item => item.Item.TryGetComponent<Base>(out var baseComp) && baseComp != null && baseComp.Address != 0 && baseComp.Name == currency)
            .GetClientRect();
    }

    public static RectangleF GetClientRectForInventSlotItemInInputStash(InventSlotItem inventSlotItem) =>
        GetClientRectForInventSlotItemInStash(InputStashClientRect, inventSlotItem, InputStashIsQuadStash);

    public static InventoryType GetInventoryTypeForStashAtIndex(int index) =>
        GetStashAtIndex(index).Inventory.InvType;

    public static StashTabContainerInventory GetStashAtIndex(int index) => StashElement?.Inventories[index];

    public static int GetServerRequestCounterForStashAtIndex(int index) =>
        GetStashAtIndex(index).Inventory.ServerInventory.ServerRequestCounter;

    public static RectangleF GetClientRectForInventSlotItemInStash(RectangleF stashTabRect, InventSlotItem inventSlotItem, bool isQuadStash)
    {
        return HelperHandler.CalculateStashItemClientRect(
            stashTabRect,
            inventSlotItem.PosX,
            inventSlotItem.PosY,
            inventSlotItem.SizeX,
            inventSlotItem.SizeY,
            isQuadStash
        );
    }

    public static async SyncTask<bool> WaitForCurrencyStashToUpdate(int initialServerRequestCounter, int timeoutS, CancellationToken token)
    {
        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(() =>
        {
            return CurrencyStashServerRequestCounter != initialServerRequestCounter;
        }, token);
    }
}