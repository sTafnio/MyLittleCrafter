using MyLittleCrafter.Items;
using SharpDX;
using Vector2 = System.Numerics.Vector2;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Handlers;

public static class HelperHandler
{
    public static float CalculateSquaredDistance(CraftingBase craftingBase, InventSlotItem currency)
    {
        var clientRect1 = craftingBase.ClientRect;
        var center1 = clientRect1.Center;

        var currencyClientRect = currency.GetClientRect();
        var center2 = currencyClientRect.Center;

        return (center1.X - center2.X) * (center1.X - center2.X) + (center1.Y - center2.Y) * (center1.Y - center2.Y);
    }


    public static Vector2 GetRandomPointInRectangleF(RectangleF clientRect)
    {
        var clickPadding = 7;
        var minX = clientRect.TopLeft.X + clickPadding;
        var maxX = clientRect.BottomRight.X - clickPadding;
        var minY = clientRect.TopLeft.Y + clickPadding;
        var maxY = clientRect.BottomRight.Y - clickPadding;

        if (minX > maxX) minX = maxX;
        if (minY > maxY) minY = maxY;

        var randomX = (float)(Main.Random.NextDouble() * (maxX - minX) + minX);
        var randomY = (float)(Main.Random.NextDouble() * (maxY - minY) + minY);

        var randomPoint = new Vector2(randomX, randomY);

        var pointWithOffset = randomPoint + Main.ClickWindowOffset;

        return pointWithOffset;
    }

    public static RectangleF CalculateStashItemClientRect(RectangleF stashTabRect, int posX, int posY, int sizeX, int sizeY, bool isQuadStash)
    {
        // Calculate the size of each cell in the stash grid
        float cellWidth = stashTabRect.Width / (isQuadStash ? 24 : 12);
        float cellHeight = stashTabRect.Height / (isQuadStash ? 24 : 12);

        // Calculate the top-left position
        float topLeftX = stashTabRect.Left + (posX * cellWidth);
        float topLeftY = stashTabRect.Top + (posY * cellHeight);

        // Calculate the bottom-right position based on the item size
        float bottomRightX = topLeftX + (sizeX * cellWidth);
        float bottomRightY = topLeftY + (sizeY * cellHeight);

        return new RectangleF(topLeftX, topLeftY, bottomRightX - topLeftX, bottomRightY - topLeftY);
    }
}

