using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    [JsonConverter(typeof(QueryConverter))]
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
    
    [JsonIgnore]
    public JToken OriginalQueryJson { get; set; } // Store the original JSON for display

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

        return new CraftCondition(Type, conditionType, UseShift, Query, compiledQuery, OriginalQueryJson);
    }
}

/// <summary>
/// Custom converter that handles Query field - can be string or Or/And/Count object
/// </summary>
public class QueryConverter : JsonConverter<string>
{
    // Regex to match Count operators like "Count == 2", "Count < 3", etc.
    private static readonly Regex CountRegex = new Regex(
        @"^Count\s*(==|!=|<=|>=|<|>)\s*(-?\d+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );
    
    // List to store tokens in order during deserialization
    private static readonly List<JToken> _tokenList = new List<JToken>();
    
    public static void ClearTokenCache() => _tokenList.Clear();
    
    public static JToken GetTokenAtIndex(int index) => index >= 0 && index < _tokenList.Count ? _tokenList[index] : null;
    
    public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        
        // Store token in the list (order matters!)
        _tokenList.Add(token);

        // Simple string
        if (token.Type == JTokenType.String)
        {
            return token.ToString();
        }

        // Object with Or/And/Count
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;
            string result = null;

            // Check for Or (case-insensitive)
            var orProperty = obj.Properties().FirstOrDefault(p => p.Name.Equals("Or", StringComparison.OrdinalIgnoreCase));
            if (orProperty != null)
            {
                var conditions = orProperty.Value.Select(t => ProcessToken(t)).Where(s => !string.IsNullOrEmpty(s));
                result = string.Join(" || ", conditions.Select(c => $"({c})"));
            }
            // Check for And (case-insensitive)
            else
            {
                var andProperty = obj.Properties().FirstOrDefault(p => p.Name.Equals("And", StringComparison.OrdinalIgnoreCase));
                if (andProperty != null)
                {
                    var conditions = andProperty.Value.Select(t => ProcessToken(t)).Where(s => !string.IsNullOrEmpty(s));
                    result = string.Join(" && ", conditions.Select(c => $"({c})"));
                }
                // Check for Count operator (e.g., "Count == 2", "Count < 3")
                else
                {
                    var countProperty = obj.Properties().FirstOrDefault(p => CountRegex.IsMatch(p.Name));
                    if (countProperty != null)
                    {
                        result = ProcessCountToken(countProperty);
                    }
                }
            }
            
            return result ?? string.Empty;
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
            // Recursively process nested Or/And/Count
            var obj = (JObject)token;

            // Check for Or (case-insensitive)
            var orProperty = obj.Properties().FirstOrDefault(p => p.Name.Equals("Or", StringComparison.OrdinalIgnoreCase));
            if (orProperty != null)
            {
                var conditions = orProperty.Value.Select(t => ProcessToken(t)).Where(s => !string.IsNullOrEmpty(s));
                return string.Join(" || ", conditions.Select(c => $"({c})"));
            }

            // Check for And (case-insensitive)
            var andProperty = obj.Properties().FirstOrDefault(p => p.Name.Equals("And", StringComparison.OrdinalIgnoreCase));
            if (andProperty != null)
            {
                var conditions = andProperty.Value.Select(t => ProcessToken(t)).Where(s => !string.IsNullOrEmpty(s));
                return string.Join(" && ", conditions.Select(c => $"({c})"));
            }

            // Check for Count operator (e.g., "Count == 2", "Count < 3")
            var countProperty = obj.Properties().FirstOrDefault(p => CountRegex.IsMatch(p.Name));
            if (countProperty != null)
            {
                return ProcessCountToken(countProperty);
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Processes a Count token like { "Count == 2": [...] }
    /// </summary>
    private string ProcessCountToken(JProperty countProperty)
    {
        var match = CountRegex.Match(countProperty.Name);
        if (!match.Success)
        {
            throw new JsonException($"Invalid Count syntax: '{countProperty.Name}'. Expected format: 'Count <operator> <number>' where operator is ==, !=, <, >, <=, or >=.");
        }

        var operatorToken = match.Groups[1].Value;
        var valueString = match.Groups[2].Value;

        // Validate the value is an array
        if (countProperty.Value.Type != JTokenType.Array)
        {
            throw new JsonException($"Count property '{countProperty.Name}' must have an array value.");
        }

        var array = (JArray)countProperty.Value;
        
        // Handle empty array
        if (array.Count == 0)
        {
            throw new JsonException($"Count array for '{countProperty.Name}' must contain at least one condition.");
        }

        // Process each condition in the array
        var conditions = array.Select(t => ProcessToken(t)).Where(s => !string.IsNullOrEmpty(s)).ToList();
        
        if (conditions.Count == 0)
        {
            throw new JsonException($"Count array for '{countProperty.Name}' produced no valid conditions.");
        }

        // Build the boolean array and count expression
        var booleanArray = string.Join(", ", conditions);
        return $"new[] {{{booleanArray}}}.Count(x => x) {operatorToken} {valueString}";
    }

    public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }
}
