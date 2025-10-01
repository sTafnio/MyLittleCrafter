using System.Collections.Generic;
using System.Linq;
using ItemFilterLibrary;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using MyLittleCrafter.Enums;

namespace MyLittleCrafter.IFL;

/// <summary>
/// Domain object representing a loaded crafting file with all its conditions
/// </summary>
public class CraftingFile
{
    public string Name { get; init; }
    public string Description { get; init; }
    public CraftCondition ItemSelectionCondition { get; init; }
    public List<CraftCondition> CraftingConditions { get; init; }

    /// <summary>
    /// Finds the first matching crafting condition for the given item
    /// </summary>
    public CraftCondition FindMatchingCondition(ItemData itemData)
    {
        return CraftingConditions.FirstOrDefault(c => c.CompiledQuery.Matches(itemData));
    }

    /// <summary>
    /// Evaluates the item and returns an evaluation result
    /// </summary>
    public EvaluationResult EvaluateItem(ItemData itemData)
    {
        // Find first matching condition
        foreach (var condition in CraftingConditions)
        {
            if (condition.CompiledQuery.Matches(itemData))
            {
                return EvaluationResult.Create(
                    isItemFinished: false,
                    currency: condition.Type,
                    useShift: condition.UseShift,
                    conditionType: condition.ConditionType
                );
            }
        }

        // No conditions matched - item is finished
        return EvaluationResult.Create(
            isItemFinished: true,
            currency: string.Empty,
            useShift: false,
            conditionType: ConditionType.ItemSelection
        );
    }

    /// <summary>
    /// Checks if the item is finished crafting (no conditions match)
    /// </summary>
    public bool IsItemFinished(ItemData itemData)
    {
        return !CraftingConditions.Any(c => c.CompiledQuery.Matches(itemData));
    }

    /// <summary>
    /// Checks if the item matches the ItemSelection condition
    /// </summary>
    public bool MatchesItemSelection(ItemData itemData)
    {
        return ItemSelectionCondition?.CompiledQuery?.Matches(itemData) ?? false;
    }

    /// <summary>
    /// Checks if the item matches the ItemSelection condition
    /// </summary>
    public bool MatchesItemSelection(InventSlotItem inventSlotItem, ExileCore.GameController gameController)
    {
        var itemData = new ItemData(inventSlotItem.Item, gameController);
        return MatchesItemSelection(itemData);
    }

    /// <summary>
    /// Gets all conditions including the ItemSelection condition
    /// </summary>
    public List<CraftCondition> GetAllConditions()
    {
        var all = new List<CraftCondition>();
        if (ItemSelectionCondition != null)
        {
            all.Add(ItemSelectionCondition);
        }
        all.AddRange(CraftingConditions);
        return all;
    }
}