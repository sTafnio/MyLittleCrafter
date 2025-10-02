using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using ExileCore;
using ExileCore.Shared.Attributes;
using ImGuiNET;
using MyLittleCrafter.Handlers;
using MyLittleCrafter.IFL;
using MyLittleCrafter.Utils;
using static MyLittleCrafter.MyLittleCrafter;

namespace MyLittleCrafter.Settings;

[Submenu(EnableCollapsing = false, RenderMethod = nameof(Render))]
public class FileSelection
{
    public void Render()
    {
        if (Main?.Settings == null)
        {
            ImGui.Text("Settings not initialized yet...");
            return;
        }

        if (ImGui.Button("Open Folder"))
        {
            var directory = Main.ConfigDirectory;
            if (Directory.Exists(directory))
            {
                Process.Start("explorer.exe", directory);
            }
            else
            {
                Log.Error($"{directory} not found.");
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Update File List"))
        {
            Main.UpdateAvailableCraftFiles();
        }

        ImGui.SameLine();
        if (ImGui.Button("Reload All"))
        {
            LoadAndApplyCraftFiles();
        }

        ImGui.Separator();
        ImGui.Text(
            "Craft Files\nFiles are processed in order. Enabled files will be loaded and used during crafting.");
        ImGui.Separator();

        if (ImGui.BeginTable("CraftFilesTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("Drag", ImGuiTableColumnFlags.WidthFixed, 40);
            ImGui.TableSetupColumn("Toggle", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 50);
            ImGui.TableSetupColumn("File", ImGuiTableColumnFlags.None);
            ImGui.TableHeadersRow();

            var rules = Main.Settings.CraftFileRules;
            for (var i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                ImGui.TableNextRow();

                // Drag handle column
                ImGui.TableSetColumnIndex(0);
                ImGui.PushID($"drag_{rule.FileName}");

                var dropTargetStart = ImGui.GetCursorScreenPos();

                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
                ImGui.Button("=", new Vector2(30, 20));
                ImGui.PopStyleColor();

                if (ImGui.BeginDragDropSource())
                {
                    ImGuiHelpers.SetDragDropPayload("CraftFileIndex", i);
                    ImGui.Text(rule.FileName);
                    ImGui.EndDragDropSource();
                }
                else if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Drag to reorder");
                }

                ImGui.SetCursorScreenPos(dropTargetStart);
                ImGui.InvisibleButton($"dropTarget_{rule.FileName}", new Vector2(30, 20));

                if (ImGui.BeginDragDropTarget())
                {
                    var payload = ImGuiHelpers.AcceptDragDropPayload<int>("CraftFileIndex");
                    if (payload != null && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        var movedRule = rules[payload.Value];
                        rules.RemoveAt(payload.Value);
                        rules.Insert(i, movedRule);
                        LoadAndApplyCraftFiles();
                    }

                    ImGui.EndDragDropTarget();
                }

                ImGui.PopID();

                // Toggle checkbox column
                ImGui.TableSetColumnIndex(1);
                ImGui.PushID($"toggle_{rule.FileName}");
                var enabled = rule.Enabled;
                if (ImGui.Checkbox("", ref enabled))
                {
                    rule.Enabled = enabled;
                    LoadAndApplyCraftFiles();
                }
                ImGui.PopID();

                // File name column
                ImGui.TableSetColumnIndex(2);
                ImGui.PushID(rule.FileName);

                var fileName = rule.FileName;
                var fileFullPath = Path.Combine(Main.ConfigDirectory, $"{fileName}.json");

                var cellWidth = ImGui.GetContentRegionAvail().X;

                // Check if this file is currently selected for display
                var isSelected = Main.CurrentCraftingFile != null && 
                                 Main.CurrentCraftingFile.Name == fileName;

                // Highlight selected file
                if (isSelected)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.5f, 0.7f, 0.4f));
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
                }

                // Clickable button for selection
                if (ImGui.Button($"##FileCell_{rule.FileName}", new Vector2(cellWidth, ImGui.GetFrameHeight())))
                {
                    SelectFileForDisplay(fileName);
                }
                
                ImGui.PopStyleColor();

                // Context menu on right-click
                StartContextMenu(fileName, fileFullPath, $"##FileCell_{rule.FileName}");

                // Draw text over the button
                var textPos = ImGui.GetItemRectMin();
                ImGui.SetCursorScreenPos(new Vector2(textPos.X + 5, textPos.Y));

                ImGui.Text(fileName);

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }

    private async void SelectFileForDisplay(string fileName)
    {
        try
        {
            var filePath = Path.Combine(Main.ConfigDirectory, $"{fileName}.json");
            
            if (!File.Exists(filePath))
            {
                Log.Error($"File not found: {fileName}.json");
                return;
            }

            var result = await JsonFileParser.LoadFileAsync(filePath);
            
            if (result.Success)
            {
                Main.CurrentCraftingFile = result.CraftingFile;
                Log.Info($"Selected {fileName} for display.");
            }
            else
            {
                Log.Error($"Failed to load {fileName} for display: {result.ErrorMessage}");
                Main.CurrentCraftingFile = null;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error selecting file for display: {ex.Message}");
            Main.CurrentCraftingFile = null;
        }
    }

    private void LoadAndApplyCraftFiles()
    {
        // Get all enabled craft files in order
        var enabledFiles = Main.Settings.CraftFileRules
            .Where(r => r.Enabled)
            .Select(r => r.FileName)
            .ToArray();

        if (enabledFiles.Length > 0)
        {
            _ = FileHandler.LoadCraftingFilesAsync(enabledFiles);
            Log.Info($"Loading {enabledFiles.Length} craft file(s)...");
        }
        else
        {
            Main.SelectedCraftingFiles.Clear();
            Main.CurrentCraftingFile = null;
            Log.Info("No enabled craft files. List cleared.");
        }
    }

    private void StartContextMenu(string fileName, string fileFullPath, string contextMenuId)
    {
        if (ImGui.BeginPopupContextItem(contextMenuId))
        {
            if (ImGui.MenuItem("Open File"))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = fileFullPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to open file: {ex.Message}");
                }
            }

            if (ImGui.MenuItem("Remove from List"))
            {
                var rule = Main.Settings.CraftFileRules.FirstOrDefault(r => r.FileName == fileName);
                if (rule != null)
                {
                    Main.Settings.CraftFileRules.Remove(rule);
                    LoadAndApplyCraftFiles();
                }
            }

            ImGui.EndPopup();
        }
    }
}
