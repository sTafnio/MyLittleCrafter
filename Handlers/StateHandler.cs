using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using static MyLittleCrafter.Enums.MyLittleCrafter;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Handlers;

public static class StateHandler
{
    public static string CurrentlySelectedCurrency { get; set; } = string.Empty;
    public static bool IsCraftSelected { get; set; } = false;
    public static bool IsInGame => Main?.GameController?.Game?.IngameState?.InGame ?? false;
    public static bool IsGameFocused => Main?.GameController?.Window?.IsForeground() ?? false;
    public static int Timeout => 3;
    public static List<string> RequiredCurrenciesForSelectedCraft => Main.CurrentCraftingConditionsList
        .Where(condition => condition.ConditionType == ConditionType.StackableCurrencyUse)
        .Select(condition => condition.Header)
        .ToList();

    public static bool IsInGameUiElementVisible(Func<IngameUIElements, Element> panelSelector) =>
        panelSelector(Main?.GameController?.Game?.IngameState?.IngameUi)?.IsVisible ?? false;

    public static int GetServerLatency() => Main?.GameController?.IngameState?.ServerData?.Latency ?? 500;

    public static bool IsCursorOverRectangle(RectangleF clientRect) =>
        Main?.GameController?.Game?.IngameState?.IngameUi?.Cursor?.GetClientRect().Intersects(clientRect) ?? false;

    public static bool IsAnItemRightClicked() =>
        TryGetCursorState(out var cursorState) && cursorState == MouseActionType.UseItem;

    public static bool IsCursorFree() =>
        TryGetCursorState(out var cursorState) && cursorState == MouseActionType.Free;

    public static bool ShouldStopCrafting()
    {
        if (!IsInGame)
        {
            Logger.Log(LogType.Error, "Not in game.");
            return true;
        }
        if (!IsGameFocused)
        {
            Logger.Log(LogType.Error, "Game is not focused.");
            return true;
        }
        if (!PlayerInventoryHandler.IsPlayerInventoryPanelOpen)
        {
            Logger.Log(LogType.Error, "Player inventory is not open.");
            return true;
        }
        if (Main.Settings.General.SelectedMethod == CraftingMethod.HarvestBench && !HarvestBenchHandler.IsHarvestBenchPanelOpen)
        {
            Logger.Log(LogType.Error, "Harvest bench is not open.");
            return true;
        }
        if (Main.Settings.General.SelectedMethod == CraftingMethod.CraftingBench && !CraftingBenchHandler.IsCraftingBenchPanelOpen)
        {
            Logger.Log(LogType.Error, "Crafting bench is not open.");
            return true;
        }
        if (Main.Settings.General.SelectedMethod == CraftingMethod.FullStash)
        {
            if (!StashHandler.IsStashPanelOpen)
            {
                Logger.Log(LogType.Error, "Stash is not open.");
                return true;
            }
        }
        return false;
    }

    public static void ResetCraftSelection()
    {
        if (!IsCraftSelected) return;
        if (HarvestBenchHandler.IsHarvestBenchPanelOpen || CraftingBenchHandler.IsCraftingBenchPanelOpen) return;

        IsCraftSelected = false;
    }

    private static bool TryGetCursorState(out MouseActionType cursorState)
    {
        var gameController = Main.GameController;

        if (gameController?.Game?.IngameState?.IngameUi?.Cursor != null)
        {
            cursorState = gameController.Game.IngameState.IngameUi.Cursor.Action;
            return true;
        }

        cursorState = MouseActionType.HoldItemForSell;
        return false;
    }
}

