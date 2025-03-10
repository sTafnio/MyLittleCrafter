using static MyLittleCrafter.Enums.MyLittleCrafter;

namespace MyLittleCrafter.IFL;

public class EvaluationResult
{
    public bool IsItemFinished { get; set; }
    public string CurrencyOrCraftName { get; set; }
    public bool UseShift { get; set; }
    public ConditionType ConditionType { get; set; }

    public EvaluationResult(bool isItemFinished, string currency, bool useShift, ConditionType conditionType)
    {
        IsItemFinished = isItemFinished;
        CurrencyOrCraftName = currency;
        UseShift = useShift;
        ConditionType = conditionType;
        Logger.Log(LogType.Debug, $"Item Evaluation: IsItemFinished={isItemFinished}, CurrencyOrCraftName={currency}, UseShift={useShift}, ConditionType={conditionType}");
    }
}