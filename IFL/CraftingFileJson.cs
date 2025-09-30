using System;
using System.Collections.Generic;
using System.Linq;
using ItemFilterLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MyLittleCrafter.Enums;

namespace MyLittleCrafter.IFL;

/// <summary>
/// Root object for JSON crafting files
/// </summary>
public class CraftingFileJson
{
    [JsonProperty("Name")]
    public string Name { get; set; }

    [JsonProperty("Description")]
    public string Description { get; set; }

    [JsonProperty("ItemSelection")]
    public string ItemSelection { get; set; }

    [JsonProperty("Conditions")]
    public List<ConditionJson> Conditions { get; set; }
}

/// <summary>
/// Individual condition in a crafting file
/// </summary>
public class ConditionJson
{
    [JsonProperty("Type")]
    public string Type { get; set; }

    [JsonProperty("Query")]
    [JsonConverter(typeof(QueryConverter))]
    public string Query { get; set; } // Automatically converted from string or Or/And object

    [JsonProperty("UseShift")]
    public bool UseShift { get; set; }

    /// <summary>
    /// Converts this JSON condition to a runtime CraftCondition (without compiled query)
    /// </summary>
    public CraftCondition ToRuntimeCondition(ItemQuery compiledQuery)
    {
        var conditionType = Type switch
        {
            string s when s.Contains("Craft") => ConditionType.CraftingBenchCraft,
            string s when s.Contains("Reforge") =>ConditionType.HarvestBenchCraft,
            _ => ConditionType.StackableCurrencyUse,
        };

        return new CraftCondition(Type, conditionType, UseShift, Query, compiledQuery);
    }
}

/// <summary>
/// Custom converter that handles Query field - can be string or Or/And object
/// </summary>
public class QueryConverter : JsonConverter<string>
{
    public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var token = JToken.Load(reader);

        // Simple string
        if (token.Type == JTokenType.String)
        {
            return token.ToString();
        }

        // Object with Or/And
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;

            if (obj["Or"] != null)
            {
                var conditions = obj["Or"].Select(t => ProcessToken(t)).Where(s => !string.IsNullOrEmpty(s));
                return string.Join(" || ", conditions.Select(c => $"({c})"));
            }

            if (obj["And"] != null)
            {
                var conditions = obj["And"].Select(t => ProcessToken(t)).Where(s => !string.IsNullOrEmpty(s));
                return string.Join(" && ", conditions.Select(c => $"({c})"));
            }
        }

        return string.Empty;
    }

    private string ProcessToken(JToken token)
    {
        if (token.Type == JTokenType.String)
        {
            return token.ToString();
        }

        if (token.Type == JTokenType.Object)
        {
            // Recursively process nested Or/And
            var obj = (JObject)token;

            if (obj["Or"] != null)
            {
                var conditions = obj["Or"].Select(t => ProcessToken(t)).Where(s => !string.IsNullOrEmpty(s));
                return string.Join(" || ", conditions.Select(c => $"({c})"));
            }

            if (obj["And"] != null)
            {
                var conditions = obj["And"].Select(t => ProcessToken(t)).Where(s => !string.IsNullOrEmpty(s));
                return string.Join(" && ", conditions.Select(c => $"({c})"));
            }
        }

        return string.Empty;
    }

    public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }
}
