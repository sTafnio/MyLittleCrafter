using System;
using System.Collections.Generic;
using MyLittleCrafter.Enums;

namespace MyLittleCrafter.Items;

/// <summary>
/// State machine for managing valid CraftingBase location transitions
/// </summary>
public class CraftingBaseStateMachine
{
    private readonly Dictionary<(ItemLocation From, ItemLocation To), string> _validTransitions;
    private ItemLocation _currentLocation;

    public ItemLocation CurrentLocation => _currentLocation;

    public CraftingBaseStateMachine(ItemLocation initialLocation)
    {
        _currentLocation = initialLocation;
        _validTransitions = BuildTransitionMap();
    }

    /// <summary>
    /// Checks if a transition from current location to target location is valid
    /// </summary>
    public bool CanTransitionTo(ItemLocation targetLocation)
    {
        // Same location is always valid (no-op)
        if (_currentLocation == targetLocation)
            return true;

        return _validTransitions.ContainsKey((_currentLocation, targetLocation));
    }

    /// <summary>
    /// Attempts to transition to a new location
    /// </summary>
    /// <returns>True if transition was successful, false if invalid</returns>
    public bool TryTransitionTo(ItemLocation targetLocation, out string errorMessage)
    {
        if (_currentLocation == targetLocation)
        {
            errorMessage = null;
            return true; // No-op transition
        }

        if (!_validTransitions.TryGetValue((_currentLocation, targetLocation), out var transitionName))
        {
            errorMessage = $"Invalid transition from {_currentLocation} to {targetLocation}";
            Logger.Log(LogType.Error, errorMessage);
            return false;
        }

        Logger.Log(LogType.Debug, $"State transition: {_currentLocation} → {targetLocation} ({transitionName})");
        _currentLocation = targetLocation;
        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Forces a transition without validation (use with caution)
    /// </summary>
    public void ForceTransitionTo(ItemLocation targetLocation)
    {
        Logger.Log(LogType.Error, $"Forced state transition: {_currentLocation} → {targetLocation}");
        _currentLocation = targetLocation;
    }

    /// <summary>
    /// Gets all valid target locations from the current location
    /// </summary>
    public List<ItemLocation> GetValidTargetLocations()
    {
        var validTargets = new List<ItemLocation>();
        foreach (ItemLocation location in Enum.GetValues(typeof(ItemLocation)))
        {
            if (location != _currentLocation && CanTransitionTo(location))
            {
                validTargets.Add(location);
            }
        }
        return validTargets;
    }

    /// <summary>
    /// Builds the map of valid state transitions
    /// Workflow: InputStash/CurrencyStash/Benches → Inventory → OutputStash/CurrencyStash/Benches
    /// </summary>
    private Dictionary<(ItemLocation From, ItemLocation To), string> BuildTransitionMap()
    {
        return new Dictionary<(ItemLocation From, ItemLocation To), string>
        {
            // FROM PlayerInventory TO other locations
            [(ItemLocation.PlayerInventory, ItemLocation.HarvestBench)] = "Place in Harvest Bench",
            [(ItemLocation.PlayerInventory, ItemLocation.CraftingBench)] = "Place in Crafting Bench",
            [(ItemLocation.PlayerInventory, ItemLocation.CurrencyStash)] = "Move to Currency Stash",
            [(ItemLocation.PlayerInventory, ItemLocation.OutputStash)] = "Move to Output Stash (Finished)",

            // TO PlayerInventory FROM other locations
            [(ItemLocation.HarvestBench, ItemLocation.PlayerInventory)] = "Remove from Harvest Bench",
            [(ItemLocation.CraftingBench, ItemLocation.PlayerInventory)] = "Remove from Crafting Bench",
            [(ItemLocation.InputStash, ItemLocation.PlayerInventory)] = "Take from Input Stash",
            [(ItemLocation.CurrencyStash, ItemLocation.PlayerInventory)] = "Take from Currency Stash",
        };
    }

    /// <summary>
    /// Gets a human-readable description of a transition
    /// </summary>
    public string GetTransitionDescription(ItemLocation from, ItemLocation to)
    {
        if (_validTransitions.TryGetValue((from, to), out var description))
        {
            return description;
        }
        return $"Invalid: {from} → {to}";
    }
}