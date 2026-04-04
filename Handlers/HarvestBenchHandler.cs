using System.Linq;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Handlers;

public static class HarvestBenchHandler
{
    public static readonly InventorySlotE HarvestBenchInventory = InventorySlotE.HarvestCraftingItem;
    public static ServerInventory HarvestBenchServerInventory => InventoryHandler.GetServerInventoryFromInventorySlotE(HarvestBenchInventory);
    public static int HarvestBenchServerRequestCounter => HarvestBenchServerInventory.ServerRequestCounter;
    public static InventSlotItem InventSlotItemInHarvestBench => HarvestWindow.CraftInventory.ServerInventory.InventorySlotItems.FirstOrDefault();
    public static HarvestWindow HarvestWindow => Main?.GameController?.IngameState?.IngameUi?.HorticraftingStationWindow;
    public static RectangleF FirstHarvestCraftRect => HarvestWindow.GetChildFromIndices(7, 0, 1, 0).GetClientRect();
    public static RectangleF HarvestCraftButtonRect => HarvestWindow.CraftButton.GetClientRect();
    public static string HarvestSearchFieldText => HarvestWindow?.GetChildFromIndices(8, 1, 0)?.Text?.Trim() ?? string.Empty;
    public static RectangleF ItemInHarvestBenchRect => HarvestWindow.CraftInventory.GetClientRect();
    public static bool IsHarvestBenchPanelOpen => StateHandler.IsInGameUiElementVisible(ui => ui.HorticraftingStationWindow);
}

