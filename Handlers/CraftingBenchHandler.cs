using System.Linq;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Handlers;

public static class CraftingBenchHandler
{
    // Only StrMasterCrafting is supported for now (Crafting Bench 2 in hideout editor)
    public static readonly InventorySlotE CraftingBenchInventory = InventorySlotE.StrMasterCrafting;
    public static ServerInventory CraftingBenchServerInventory => InventoryHandler.GetServerInventoryFromInventorySlotE(CraftingBenchInventory);
    public static int CraftingBenchServerRequestCounter => CraftingBenchServerInventory.ServerRequestCounter;
    public static InventSlotItem InventSlotItemInCraftingBench => InventoryHandler.GetCraftableInventSlotItemsFromServerInventory(CraftingBenchServerInventory).FirstOrDefault();
    public static CraftBenchWindow CraftingBenchWindow { get; } = Main?.GameController?.IngameState?.IngameUi?.CraftBench;
    public static RectangleF FirstCraftingBenchCraftRect => CraftingBenchWindow.GetChildFromIndices(2, 1, 0, 0, 1, 0).GetClientRect();
    public static RectangleF CraftingBenchCraftButtonRect => CraftingBenchWindow.GetChildFromIndices(2, 3, 0).GetClientRect();
    public static string CraftingBenchSearchFieldText => CraftingBenchWindow?.GetChildFromIndices(2, 0, 1, 0, 0)?.Text?.Trim() ?? string.Empty;
    public static RectangleF ItemInCraftingBenchRect => CraftingBenchWindow.GetChildFromIndices(2, 3, 1).GetClientRect();
    public static bool IsCraftingBenchPanelOpen => StateHandler.IsInGameUiElementVisible(ui => ui.CraftBench);
}

