using System;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;
using Newtonsoft.Json.Linq;

namespace MyLittleCrafter.Utils;

/// <summary>
/// Formats and displays query JSON structures in a readable hierarchical format
/// </summary>
public static class QueryDisplayFormatter
{
    private static readonly Regex CountRegex = new(
        @"^Count\s*(==|!=|<=|>=|<|>)\s*(-?\d+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    // Color scheme
    private static readonly Vector4 OperatorColor = new(1f, 0.8f, 0.2f, 1f); // Gold
    private static readonly Vector4 KeywordColor = new(0.4f, 0.9f, 1f, 1f); // Cyan
    private static readonly Vector4 StringColor = new(0.7f, 1f, 0.7f, 1f); // Light green
    private static readonly Vector4 NumberColor = new(1f, 0.6f, 0.8f, 1f); // Pink
    private static readonly Vector4 BracketColor = new(0.8f, 0.8f, 0.8f, 1f); // Gray

    /// <summary>
    /// Renders a query in a hierarchical, colored format
    /// </summary>
    public static void RenderQuery(JToken queryJson, string rawQuery, int indentLevel = 0)
    {
        if (queryJson == null)
        {
            // Fall back to raw query display
            ImGui.TextWrapped(rawQuery);
            return;
        }

        if (queryJson.Type == JTokenType.String)
        {
            RenderSimpleQuery(queryJson.ToString(), indentLevel);
        }
        else if (queryJson.Type == JTokenType.Object)
        {
            var obj = (JObject)queryJson;

            // Check for Or (case-insensitive)
            var orProperty = obj.Properties().FirstOrDefault(p => p.Name.Equals("Or", StringComparison.OrdinalIgnoreCase));
            if (orProperty != null)
            {
                RenderLogicalOperator("Or", (JArray)orProperty.Value, indentLevel);
            }
            // Check for And (case-insensitive)
            else
            {
                var andProperty = obj.Properties().FirstOrDefault(p => p.Name.Equals("And", StringComparison.OrdinalIgnoreCase));
                if (andProperty != null)
                {
                    RenderLogicalOperator("And", (JArray)andProperty.Value, indentLevel);
                }
                // Check for Count
                else
                {
                    var countProperty = obj.Properties().FirstOrDefault(p => CountRegex.IsMatch(p.Name));
                    if (countProperty != null)
                    {
                        RenderCountOperator(countProperty.Name, (JArray)countProperty.Value, indentLevel);
                    }
                }
            }
        }
    }

    private static void RenderSimpleQuery(string query, int indentLevel)
    {
        // Add indentation
        ImGui.Indent(indentLevel * 20);
        
        // Syntax highlight the query
        var parts = query.Split(new[] { "==", "!=", "<=", ">=", "<", ">", "&&", "||" }, StringSplitOptions.None);
        var operators = Regex.Matches(query, @"(==|!=|<=|>=|<|>|&&|\|\|)");

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i].Trim();
            
            // Highlight keywords
            if (part.StartsWith("Rarity") || part.StartsWith("ItemLevel") || part.StartsWith("ModsInfo") || 
                part.Contains("ItemRarity.") || part.Contains("Open") || part.Contains("Explicit"))
            {
                ImGui.TextColored(KeywordColor, part);
            }
            // Highlight numbers
            else if (int.TryParse(part, out _))
            {
                ImGui.TextColored(NumberColor, part);
            }
            // Regular text
            else if (!string.IsNullOrWhiteSpace(part))
            {
                ImGui.TextColored(StringColor, part);
            }

            if (i < operators.Count)
            {
                ImGui.SameLine(0, 0);
                ImGui.TextColored(OperatorColor, $" {operators[i].Value} ");
                ImGui.SameLine(0, 0);
            }
        }
        
        ImGui.Unindent(indentLevel * 20);
    }

    private static void RenderLogicalOperator(string operatorName, JArray conditions, int indentLevel)
    {
        // Add indentation
        ImGui.Indent(indentLevel * 20);
        
        ImGui.TextColored(OperatorColor, $"{operatorName}:");

        for (int i = 0; i < conditions.Count; i++)
        {
            ImGui.Indent(20);
            
            // Show condition number inline with the query
            ImGui.TextColored(BracketColor, $"{i + 1}) ");
            ImGui.SameLine(0, 0);
            RenderQuery(conditions[i], null, indentLevel + 1);
            
            ImGui.Unindent(20);

            if (i < conditions.Count - 1)
            {
                ImGui.Spacing();
            }
        }
        
        ImGui.Unindent(indentLevel * 20);
    }

    private static void RenderCountOperator(string countHeader, JArray conditions, int indentLevel)
    {
        var match = CountRegex.Match(countHeader);
        if (!match.Success) return;

        var operatorToken = match.Groups[1].Value;
        var valueString = match.Groups[2].Value;

        // Add indentation
        ImGui.Indent(indentLevel * 20);
        
        ImGui.TextColored(OperatorColor, "Count ");
        ImGui.SameLine(0, 0);
        ImGui.TextColored(OperatorColor, operatorToken);
        ImGui.SameLine(0, 0);
        ImGui.TextColored(NumberColor, $" {valueString}:");

        for (int i = 0; i < conditions.Count; i++)
        {
            ImGui.Indent(20);
            
            // Show condition number inline with the query
            ImGui.TextColored(BracketColor, $"{i + 1}) ");
            ImGui.SameLine(0, 0);
            RenderQuery(conditions[i], null, indentLevel + 1);
            
            ImGui.Unindent(20);

            if (i < conditions.Count - 1)
            {
                ImGui.Spacing();
            }
        }
        
        ImGui.Unindent(indentLevel * 20);
    }

}
