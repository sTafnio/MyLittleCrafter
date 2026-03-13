using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ExileCore;
using ExileCore.Shared;
using ExileCore.Shared.Nodes;
using MyLittleCrafter.IFL;
using MyLittleCrafter.Handlers;
using System;
using MyLittleCrafter.Managers;
using MyLittleCrafter.Items;
using InputHumanizer.Input;
using Vector2N = System.Numerics.Vector2;
using ExileCore.Shared.Helpers;
using ExileCore.PoEMemory.Models;
using MyLittleCrafter.Enums;
using MyLittleCrafter.Utils;
using MoreLinq;

namespace MyLittleCrafter;

public class MyLittleCrafter : BaseSettingsPlugin<MyLittleCrafterSettings>
{
    public static MyLittleCrafter Main;

    public CancellationTokenSource OperationCts;
    public IInputController InputController;
    public Random Random;
    public PluginBridge PluginBridge;
    public Vector2N ClickWindowOffset;
    public SyncTask<bool> CurrentOperation;
    private List<Keys> keysToRelease = [];

    public List<string> AvailableCraftFilesList = [];
    public List<CraftingFile> SelectedCraftingFiles = [];

    public CraftingFile CurrentCraftingFile = null;
    public List<CraftingBase> ItemsToCraftOnList = [];


    public MyLittleCrafter()
    {
        Name = "My Little Crafter";
        OperationCts = new CancellationTokenSource();
        Random = new Random();
    }

    public override bool Initialise()
    {
        Main = this;
        RegisterHotkey(Settings.General.ToggleButton.Value);

        keysToRelease = [Keys.LButton, Keys.RButton, Keys.LControlKey, Keys.LShiftKey, Keys.F, Keys.V, Keys.Left, Keys.Right];
        foreach (var key in keysToRelease) Input.RegisterKey(key);

        UpdateAvailableCraftFiles();

        // Load enabled craft files on plugin initialization
        var enabledFiles = Settings.CraftFileRules
            .Where(r => r.Enabled)
            .Select(r => r.FileName)
            .ToArray();

        if (enabledFiles.Length > 0)
        {
            _ = FileHandler.LoadCraftingFilesAsync(enabledFiles);
            Log.Info($"Auto-loading {enabledFiles.Length} enabled craft file(s) on plugin start...");
        }

        Settings.DiscordNotifications.TestWebhook.OnPressed += () =>
        {
            DiscordService.SendDiscordNotification("Test notification from MyLittleCrafter!", null, true);
        };

        // Register system actions test buttons
        Settings.SystemOptions.TestCloseExileAPI.OnPressed += () =>
        {
            SystemService.CloseExileAPI(Settings.SystemOptions.SendNotificationBeforeAction);
        };

        Settings.SystemOptions.TestCloseGame.OnPressed += () =>
        {
            SystemService.CloseGame(Settings.SystemOptions.SendNotificationBeforeAction);
        };

        Settings.SystemOptions.TestShutdown.OnPressed += () =>
        {
            SystemService.ShutdownComputer(Settings.SystemOptions.SendNotificationBeforeAction);
        };

        PluginBridge = GameController.PluginBridge;
        if (PluginBridge != null)
        {
            Tracker.Tracker.GetBaseItemTypeValue = PluginBridge.GetMethod<Func<BaseItemType, double>>("NinjaPrice.GetBaseItemTypeValue");
            Log.Info("NinjaPrice plugin bridge found. NinjaPrice integration enabled.");
        }
        else
        {
            Log.Info("NinjaPrice plugin bridge not found. NinjaPrice integration will be disabled.");
        }

        PluginBridge.SaveMethod("MyLittleCrafter.Start", (Action)Start);
        PluginBridge.SaveMethod("MyLittleCrafter.Stop", (Action)Stop);


        return true;
    }

    public void UpdateAvailableCraftFiles()
    {
        AvailableCraftFilesList = new DirectoryInfo(ConfigDirectory)
            .GetFiles("*.json")
            .Select(x => Path.GetFileNameWithoutExtension(x.Name))
            .OrderBy(x => x)
            .ToList();

        // Sync with CraftFileRules
        foreach (var fileName in AvailableCraftFilesList)
        {
            if (!Settings.CraftFileRules.Any(r => r.FileName == fileName))
            {
                Settings.CraftFileRules.Add(new Settings.CraftFileRule
                {
                    FileName = fileName,
                    Enabled = false
                });
            }
        }

        // Remove rules for files that no longer exist
        var rulesToRemove = Settings.CraftFileRules
            .Where(r => !AvailableCraftFilesList.Contains(r.FileName))
            .ToList();

        foreach (var rule in rulesToRemove)
        {
            Settings.CraftFileRules.Remove(rule);
        }

        Log.Info($"Updated available craft files (found {AvailableCraftFilesList.Count} JSON files).");
    }

    private static void RegisterHotkey(HotkeyNode hotkey)
    {
        Input.RegisterKey(hotkey.Value);
        hotkey.OnValueChanged += () => Input.RegisterKey(hotkey.Value);
    }

    public override Job Tick()
    {
        ClickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft.ToVector2Num();

        if (CurrentOperation is not null && StateHandler.ShouldStopCrafting())
        {
            Stop();
            return null;
        }

        if (Settings.General.ToggleButton.PressedOnce())
        {
            if (CurrentOperation is not null)
            {
                Stop();
            }
            else
            {
                Start();
            }
        }

        // Don't know of a better way to track/reset selection
        StateHandler.ResetCraftSelection();
        return null;
    }

    public void Start()
    {
        // Check if we have any craft files selected
        if (SelectedCraftingFiles.Count == 0)
        {
            Log.Error("No craft files are selected. Please enable at least one craft file in the File Selection tab.");
            return;
        }

        Log.Info($"Starting crafter with {SelectedCraftingFiles.Count} craft file(s): {string.Join(", ", SelectedCraftingFiles.Select(f => f.Name))}");

        ItemsToCraftOnList = [];
        ResetCancellationTokenSource();
        CurrentOperation = CraftingStart(OperationCts.Token);
    }

    public void Stop()
    {
        if (CurrentOperation == null)
            return;

        CurrentOperation = null;

        foreach (var key in keysToRelease.Where(Input.IsKeyDown)) Input.KeyUp(key);

        // if (StateHandler.IsAnItemRightClicked())
        // {
        //     Input.KeyDown(Keys.Escape);
        //     Input.KeyUp(Keys.Escape);
        // }

        if (InputController != null)
        {
            InputController.Dispose();
            InputController = null;
        }

        StateHandler.CurrentlySelectedCurrency = string.Empty;

        // Stop tracking the current craft
        Tracker.Tracker.StopCraft();

        // Send Discord notification if enabled
        if (Settings.DiscordNotifications.EnableDiscordNotifications.Value)
        {
            string messageContent = Settings.DiscordNotifications.MessageContent.Value;
            string statsContent = DiscordService.FormatCraftStats(Tracker.Tracker.LastTrackedCrafts[0]);
            DiscordService.SendDiscordNotification(messageContent, statsContent);
        }

        CurrentCraftingFile = null;

        Log.Info("Crafter has been stopped.");

        // Execute system actions if enabled
        var sendNotification = Settings.SystemOptions.SendNotificationBeforeAction.Value;

        if (Settings.SystemOptions.CloseExileAPIOnStop.Value)
        {
            SystemService.CloseExileAPI(sendNotification);
        }

        if (Settings.SystemOptions.CloseGameOnStop.Value)
        {
            SystemService.CloseGame(sendNotification);
        }

        if (Settings.SystemOptions.ShutdownPCOnStop.Value)
        {
            SystemService.ShutdownComputer(sendNotification);
        }
    }

    private void ResetCancellationTokenSource()
    {
        if (OperationCts != null)
        {
            if (!OperationCts.IsCancellationRequested)
            {
                OperationCts.Cancel();
            }

            OperationCts.Dispose();
        }

        OperationCts = new CancellationTokenSource();
    }

    private async SyncTask<bool> CraftingStart(CancellationToken token)
    {
        var tryGetInputController = GameController.PluginBridge.GetMethod<Func<string, IInputController>>("InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            Log.Error("InputHumanizer method not registered.");
            return false;
        }

        IInputController inputController = null;

        inputController = tryGetInputController(Name);
        if (inputController == null)
        {
            Log.Error("Input controller not found.");
            return false;
        }

        InputController = inputController;

        // Refresh might solve some issues with items not being up to date
        // GameController.Area.ForceRefreshArea(true);
        // await Task.Delay(500, token);

        using (inputController)
        {
            try
            {
                if (Settings.General.SelectedMethod == CraftingMethod.OpenDivinationCard)
                {
                    if (!await DivCardOpener.OpenDivCards(token)) return false;
                    return true;
                }

                else
                {
                    if (!await CraftingSetupManager.LoadStashes(token)) return false;

                    for (int i = 0; i < SelectedCraftingFiles.Count; i++)
                    {
                        // Check for cancellation before processing each craft file
                        token.ThrowIfCancellationRequested();

                        CurrentCraftingFile = SelectedCraftingFiles[i];
                        Log.Info($"Processing craft file {i + 1}/{SelectedCraftingFiles.Count}: {CurrentCraftingFile.Name}");

                        try
                        {
                            // Clear items list for this craft file to avoid stale entries from previous iterations
                            Main.ItemsToCraftOnList.Clear();

                            if (!CraftingSetupManager.SetUpCrafting())
                            {
                                Log.Error($"Failed to setup crafting for {CurrentCraftingFile.Name}. Skipping to next file.");
                                continue;
                            }

                            Tracker.Tracker.StartCraft();

                            bool craftingSucceeded = false;
                            switch (Settings.General.SelectedMethod)
                            {
                                case CraftingMethod.Inventory:
                                    craftingSucceeded = await InventoryCraftingManager.CraftItems(token);
                                    break;
                                case CraftingMethod.CraftingBench:
                                    craftingSucceeded = await BenchCraftingManager.CraftItems(token);
                                    break;
                                case CraftingMethod.HarvestBench:
                                    craftingSucceeded = await HarvestCraftingManager.CraftItems(token);
                                    break;
                                case CraftingMethod.FullStash:
                                    craftingSucceeded = await FullStashCraftingManager.CraftItems(token);
                                    break;
                            }

                            if (!craftingSucceeded)
                            {
                                Log.Error($"Crafting failed for {CurrentCraftingFile.Name}. Skipping to next file.");
                                continue;
                            }

                            Log.Info($"Successfully completed crafting for {CurrentCraftingFile.Name}");
                        }
                        catch (OperationCanceledException)
                        {
                            Log.Info("Crafting cancelled by user.");
                            throw; // Re-throw to be caught by outer catch
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error processing {CurrentCraftingFile?.Name ?? "Unknown"}: {ex.Message}");
                            Log.Error($"Stack trace: {ex.StackTrace}");
                            if (ex.InnerException != null)
                            {
                                Log.Error($"Inner exception: {ex.InnerException.Message}");
                            }
                            continue;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                Stop();
            }
        }
        return true;
    }

    public override void Render()
    {
        base.Render();

        if (CurrentOperation is not null)
        {
            TaskUtils.RunOrRestart(ref CurrentOperation, () => null);
        }
    }
}