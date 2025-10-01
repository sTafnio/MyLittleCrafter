using ItemFilterLibrary;
using MyLittleCrafter.Enums;
using MyLittleCrafter.IFL;
using MyLittleCrafter.Items;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Handlers;

public static class EvaluationHandler
{
    public static EvaluationResult EvaluateCraftingBase(CraftingBase craftingBase)
    {
        if (Main.CurrentCraftingFile == null)
        {
            return EvaluationResult.Create(
                isItemFinished: true,
                currency: string.Empty,
                useShift: false,
                conditionType: ConditionType.ItemSelection
            );
        }

        return Main.CurrentCraftingFile.EvaluateItem(craftingBase.ItemData);
    }

    public static bool IsItemMatchingItemSelection(InventSlotItem inventSlotItem)
    {
        if (Main.CurrentCraftingFile == null)
            return false;

        return Main.CurrentCraftingFile.MatchesItemSelection(inventSlotItem, Main.GameController);
    }

    public static bool IsItemFinished(ItemData itemData)
    {
        if (Main.CurrentCraftingFile == null)
            return true;

        return Main.CurrentCraftingFile.IsItemFinished(itemData);
    }

    public static bool IsItemFinished(InventSlotItem inventSlotItem)
    {
        var itemData = new ItemData(inventSlotItem.Item, Main.GameController);
        return IsItemFinished(itemData);
    }
}

