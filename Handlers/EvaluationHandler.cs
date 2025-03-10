using System.Linq;
using ItemFilterLibrary;
using MyLittleCrafter.IFL;
using MyLittleCrafter.Items;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using static MyLittleCrafter.Enums.MyLittleCrafter;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Handlers;

public static class EvaluationHandler
{
    public static bool CompareItem(ItemData itemData, ItemQuery itemQuery)
    {
        return itemQuery.Matches(itemData);
    }

    public static EvaluationResult EvaluateCraftingBase(CraftingBase craftingBase)
    {
        var matchedHeader = string.Empty;
        var useShift = false;
        var conditionType = ConditionType.Global; // Default to global, we don't care about the type if item is finished anyway

        // If multiple conditions are met, the first one will be used to determine what to do next
        foreach (var condition in Main.CurrentCraftingConditionsList)
        {
            if (condition.Header == "Global") continue;

            if (CompareItem(craftingBase.ItemData, condition.CompiledQuery))
            {
                matchedHeader = condition.Header;
                useShift = condition.UseShift;
                conditionType = condition.ConditionType;
                break; // Exit the loop once a match is found
            }
        }

        var isItemFinished = string.IsNullOrEmpty(matchedHeader);

        return new EvaluationResult(isItemFinished, matchedHeader, useShift, conditionType);
    }

    public static bool IsItemMatchingGlobalCondition(InventSlotItem inventSlotItem)
    {
        var itemData = new ItemData(inventSlotItem.Item, Main.GameController);
        return CompareItem(itemData, Main.CurrentCraftingConditionsList.FirstOrDefault(c => c.Header == "Global")?.CompiledQuery);
    }

    public static bool IsItemFinished(ItemData itemData)
    {
        foreach (var condition in Main.CurrentCraftingConditionsList)
        {
            if (condition.Header == "Global")
                continue;

            // If the item matches any currency/craft condition, it is not finished
            if (CompareItem(itemData, condition.CompiledQuery))
            {
                return false;
            }
        }
        return true;
    }

    public static bool IsItemFinished(InventSlotItem inventSlotItem)
    {
        var itemData = new ItemData(inventSlotItem.Item, Main.GameController);
        var result = IsItemFinished(itemData);

        return result;
    }
}

