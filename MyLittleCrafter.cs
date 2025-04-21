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
using System.Threading.Tasks;
using InputHumanizer.Input;
using static MyLittleCrafter.Enums.MyLittleCrafter;
using Vector2N = System.Numerics.Vector2;
using ExileCore.Shared.Helpers;
using ExileCore.PoEMemory.Models;

namespace MyLittleCrafter;

public class MyLittleCrafter : BaseSettingsPlugin<MyLittleCrafterSettings>
{
    public static MyLittleCrafter Main;

    public CancellationTokenSource OperationCts;
    public IInputController InputController;
    public Random Random;
    public Vector2N ClickWindowOffset;
    public SyncTask<bool> CurrentOperation;
    private List<Keys> keysToRelease = [];
    private List<string> availableCraftFilesList = [];
    public List<CraftCondition> CurrentCraftingConditionsList = [];
    public List<CraftingBase> ItemsToCraftOnList = [];
    public PluginBridge PluginBridge;

    public MyLittleCrafter()
    {
        Name = "My Little Crafter";
        OperationCts = new CancellationTokenSource();
        Random = new Random();
    }

    public override bool Initialise()
    {
        Main = this;

        RegisterHotkey(Settings.General.ToggleButton);

        keysToRelease = [Keys.LButton, Keys.RButton, Keys.LControlKey, Keys.LShiftKey, Keys.F, Keys.V, Keys.Left, Keys.Right];
        foreach (var key in keysToRelease) Input.RegisterKey(key);

        UpdateAvailableCraftFiles();

        Settings.FileOptions.SelectedCraftingFile.OnValueSelected += FileHandler.LoadCraftingFile;

        if (!string.IsNullOrEmpty(Settings.FileOptions.SelectedCraftingFile))
        {
            FileHandler.LoadCraftingFile(Settings.FileOptions.SelectedCraftingFile);
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
            Logger.Log(LogType.Info, "NinjaPrice plugin bridge found. NinjaPrice integration enabled.");
        }
        else
        {
            Logger.Log(LogType.Info, "NinjaPrice plugin bridge not found. NinjaPrice integration will be disabled.");
        }

        return true;
    }

    public void UpdateAvailableCraftFiles()
    {
        availableCraftFilesList = new DirectoryInfo(ConfigDirectory)
            .GetFiles("*.craft")
            .Select(x => Path.GetFileNameWithoutExtension(x.Name))
            .ToList();

        Settings.FileOptions.SelectedCraftingFile.SetListValues(availableCraftFilesList);
        Logger.Log(LogType.Info, $"Updated available craft files.");
    }

    private static void RegisterHotkey(HotkeyNode hotkey)
    {
        Input.RegisterKey(hotkey);
        hotkey.OnValueChanged += () => Input.RegisterKey(hotkey);
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
                ItemsToCraftOnList = [];
                ResetCancellationTokenSource();
                CurrentOperation = CraftingStart(OperationCts.Token);
            }
        }

        // Don't know of a better way to track/reset selection
        StateHandler.ResetCraftSelection();
        return null;
    }

    public void Stop()
    {
        CurrentOperation = null;

        foreach (var key in keysToRelease.Where(Input.IsKeyDown)) Input.KeyUp(key);

        if (StateHandler.IsAnItemRightClicked())
        {
            Input.KeyPressRelease(Keys.Escape);
        }

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

        Logger.Log(LogType.Info, "Crafter has been stopped.");

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
        // Refresh might solve some issues with items not being up to date
        GameController.Area.ForceRefreshArea(true);
        await Task.Delay(500, token);

        if (!CraftingSetupManager.SetUpCrafting()) return false;

        Tracker.Tracker.StartCraft();

        var tryGetInputController = GameController.PluginBridge.GetMethod<Func<string, IInputController>>("InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            Logger.Log(LogType.Error, "InputHumanizer method not registered.");
            return false;
        }

        IInputController inputController = null;

        inputController = tryGetInputController(Name);
        if (inputController == null)
        {
            Logger.Log(LogType.Error, "Input controller not found.");
            return false;
        }

        InputController = inputController;

        using (inputController)
        {
            try
            {
                switch (Settings.General.SelectedMethod)
                {
                    case CraftingMethod.Inventory:
                        if (!await InventoryCraftingManager.CraftItems(token)) return false;
                        break;
                    case CraftingMethod.CraftingBench:
                        if (!await BenchCraftingManager.CraftItems(token)) return false;
                        break;
                    case CraftingMethod.HarvestBench:
                        if (!await HarvestCraftingManager.CraftItems(token)) return false;
                        return false;
                    case CraftingMethod.FullStash:
                        if (!await FullStashCraftingManager.CraftItems(token)) return false;
                        break;
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