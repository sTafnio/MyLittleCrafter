using ImGuiNET;

namespace MyLittleCrafter;

public static class ImGuiHelper
{
    public static void RightAlignText(string text)
    {
        var textSize = ImGui.CalcTextSize(text);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - textSize.X - ImGui.GetStyle().CellPadding.X * 2);
        ImGui.Text(text);
    }
}