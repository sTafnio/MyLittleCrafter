using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using MyLittleCrafter.Enums;
using MyLittleCrafter.Handlers;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Settings;

[Submenu(EnableCollapsing = false)]
public class StashOptions
{
    public int InputStashIndex { get; set; } = -1;
    public int OutputStashIndex { get; set; } = -1;
    public int CurrencyStashIndex { get; set; } = -1;

    [Newtonsoft.Json.JsonIgnore]
    [Menu(null, null)]
    public CustomNode StashButtons { get; set; } = new();

    public StashOptions()
    {
        StashButtons.DrawDelegate = () =>
        {
            if (Main?.Settings?.General != null && Main.Settings.General.SelectedMethod == CraftingMethod.FullStash)
            {
                var stashNames = StashHandler.AllStashNames;

                if (stashNames != null && stashNames.Count > 0)
                {
                    if (ImGui.BeginCombo("Input Stash", InputStashIndex >= 0 && InputStashIndex < stashNames.Count ? stashNames[InputStashIndex] : "Select Input Stash"))
                    {
                        for (int i = 0; i < stashNames.Count; i++)
                        {
                            bool isSelected = InputStashIndex == i;
                            if (ImGui.Selectable(stashNames[i], isSelected))
                            {
                                InputStashIndex = i;
                                Logger.Log(LogType.Debug, $"Input Stash selected: {stashNames[i]}");
                            }

                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }

                    if (ImGui.BeginCombo("Output Stash", OutputStashIndex >= 0 && OutputStashIndex < stashNames.Count ? stashNames[OutputStashIndex] : "Select Output Stash"))
                    {
                        for (int i = 0; i < stashNames.Count; i++)
                        {
                            bool isSelected = OutputStashIndex == i;
                            if (ImGui.Selectable(stashNames[i], isSelected))
                            {
                                OutputStashIndex = i;
                                Logger.Log(LogType.Debug, $"Output Stash selected: {stashNames[i]}");
                            }

                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }

                    if (ImGui.BeginCombo("Currency Stash", CurrencyStashIndex >= 0 && CurrencyStashIndex < stashNames.Count ? stashNames[CurrencyStashIndex] : "Select Currency Stash"))
                    {
                        for (int i = 0; i < stashNames.Count; i++)
                        {
                            bool isSelected = CurrencyStashIndex == i;
                            if (ImGui.Selectable(stashNames[i], isSelected))
                            {
                                CurrencyStashIndex = i;
                                Logger.Log(LogType.Debug, $"Currency Stash selected: {stashNames[i]}");
                            }

                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }
                }
            }
        };
    }
}
