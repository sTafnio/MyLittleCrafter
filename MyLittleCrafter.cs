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

        // Settings.InventoryOptions.AutoMatchAll.OnValueChanged += (_, _) => SelectedItemsList.Clear();

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

        Logger.Log(LogType.Info, "Crafter has been stopped.");
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