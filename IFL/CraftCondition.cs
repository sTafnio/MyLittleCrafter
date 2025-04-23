using ItemFilterLibrary;
using static MyLittleCrafter.Enums.MyLittleCrafter;

namespace MyLittleCrafter.IFL;

public class CraftCondition(string header, ConditionType conditionType, bool useShift, string rawQuery)
{
    public string Header { get; set; } = header;
    public ConditionType ConditionType { get; set; } = conditionType;
    public bool UseShift { get; set; } = useShift;
    public string RawQuery { get; set; } = rawQuery;
    public ItemQuery CompiledQuery { get; set; }
}

