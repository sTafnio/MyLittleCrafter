using System.Linq;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Handlers;

public static class CardTradeHandler
{
    public static readonly InventorySlotE DivCardTrade = InventorySlotE.DivinationCardTrade;
    public static ServerInventory DivCardTradeServerInventory => InventoryHandler.GetServerInventoryFromInventorySlotE(DivCardTrade);
    public static int DivCardTradeServerRequestCounter => DivCardTradeServerInventory.ServerRequestCounter;
    public static InventSlotItem InventSlotItemInDivCardTrade => InventoryHandler.GetCraftableInventSlotItemsFromServerInventory(DivCardTradeServerInventory).FirstOrDefault();
    public static CardTradeWindow DivCardTradeWindow => Main?.GameController?.IngameState?.IngameUi?.CardTradeWindow;
    public static RectangleF DivCardTradeButton => DivCardTradeWindow.TradeButton.GetClientRect();
    public static RectangleF ItemInDivCardTrade => DivCardTradeWindow.CardSlotItem.GetClientRect();
    public static bool IsCardTradingWindowOpen => StateHandler.IsInGameUiElementVisible(ui => ui.CardTradeWindow);
}