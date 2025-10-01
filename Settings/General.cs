using System;
using System.Linq;
using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using MyLittleCrafter.Enums;
using MyLittleCrafter.Utils;

namespace MyLittleCrafter.Settings;

[Submenu(EnableCollapsing = false)]
public class General
{
    [Menu("Toggle", "Toggle to start/stop the crafting process.")]
    public HotkeyNode ToggleButton { get; set; } = new HotkeyNode(Keys.None);

    [Newtonsoft.Json.JsonIgnore]
    [Menu(null, "Select crafting type.")]
    public CustomNode CraftTypeSelection { get; set; } = new();
    public CraftingMethod SelectedMethod { get; set; }

    public General()
    {
        CraftTypeSelection.DrawDelegate = () =>
        {
            var craftingMethods = Enum.GetValues(typeof(CraftingMethod))
                .Cast<CraftingMethod>()
                .Where(method => method != CraftingMethod.None)
                .ToArray();
            var currentMethod = SelectedMethod;

            var methodCount = craftingMethods.Length;
            var i = 0;

            foreach (var method in craftingMethods)
            {
                var isSelected = currentMethod == method;
                if (ImGui.RadioButton(method.ToString(), isSelected))
                {
                    SelectedMethod = method;
                    Log.Debug( $"Selected crafting method: {method}");
                }

                if (i < methodCount - 1)
                {
                    ImGui.SameLine();
                }
                i++;
            }
        };
    }
}
