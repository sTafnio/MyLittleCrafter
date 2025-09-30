using System.Threading;
using ExileCore.Shared;
using SharpDX;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;

namespace MyLittleCrafter.Handlers;

/// <summary>
/// Interface for handling item retrieval and operations at different locations
/// </summary>
public interface IItemLocationHandler
{
    /// <summary>
    /// Gets the item at the specified client rectangle
    /// </summary>
    InventSlotItem GetItem(RectangleF clientRect);

    /// <summary>
    /// Waits for an item to be available at the specified location
    /// </summary>
    SyncTask<bool> WaitForItem(RectangleF clientRect, CancellationToken token);

    /// <summary>
    /// Gets the standard client rectangle for items at this location
    /// </summary>
    RectangleF GetStandardRect();
}