using MyLittleCrafter.Enums;

namespace MyLittleCrafter.IFL;

/// <summary>
/// Immutable result of evaluating an item against crafting conditions
/// </summary>
public record EvaluationResult(
    bool IsItemFinished,
    string CurrencyOrCraftName,
    bool UseShift,
    ConditionType ConditionType)
{
    /// <summary>
    /// Factory method that creates an evaluation result and logs it
    /// </summary>
    public static EvaluationResult Create(bool isItemFinished, string currency, bool useShift, ConditionType conditionType)
    {
        Logger.Log(LogType.Debug, $"Item Evaluation: IsItemFinished={isItemFinished}, CurrencyOrCraftName={currency}, UseShift={useShift}, ConditionType={conditionType}");
        return new EvaluationResult(isItemFinished, currency, useShift, conditionType);
    }
}
