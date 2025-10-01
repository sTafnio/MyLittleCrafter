using ItemFilterLibrary;
using MyLittleCrafter.Enums;
using Newtonsoft.Json.Linq;

namespace MyLittleCrafter.IFL;

/// <summary>
/// Represents a crafting condition with its compiled query.
/// Immutable record for thread safety and value equality.
/// </summary>
public record CraftCondition(
    string Type,
    ConditionType ConditionType,
    bool UseShift,
    string RawQuery,
    ItemQuery CompiledQuery,
    JToken OriginalQueryJson = null);

