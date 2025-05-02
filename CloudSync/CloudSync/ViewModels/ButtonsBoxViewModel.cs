using CloudSync.Interfaces;
using CloudSync.Models;
using CloudSync.Mods;
using StardewModdingAPI;
using StardewUI.Framework;
using StardewValley.Menus;

namespace CloudSync.ViewModels;

public class ButtonsBoxViewModel : ViewModelBase, IReadyToClose, IOnClose
{
    public string Message;
    private readonly Func<bool>? _isReadyToClose;
    private readonly TaskCompletionSource? _tsc;
    public readonly BoxButton[] Buttons;

    public ButtonsBoxViewModel(string message, BoxButton[] buttons, Func<bool>? readyToClose = null, TaskCompletionSource? tsc = null)
    {
        Message = message;
        Buttons = buttons;
        _isReadyToClose = readyToClose;
        _tsc = tsc;
    }

    public static ButtonsBoxViewModel? Show(
        string message,
        BoxButton[] buttons,
        Func<bool>? readyToClose = null,
        IClickableMenu? parentMenu = null,
        bool replaceExisting = false)
    {
        if (Api.StardewUI.ViewEngine is null)
        {
            Mod.Logger.Log("ViewEngine is null.", LogLevel.Warn);
            return null;
        }

        ButtonsBoxViewModel viewModel = new(message, buttons, readyToClose);
        foreach (BoxButton button in buttons)
        {
            if (!button.ExitOnClick)
            {
                continue;
            }

            Action oldAction = button.Action;
            button.Action = () =>
            {
                oldAction.Invoke();
                viewModel.Controller?.Menu.exitThisMenu();
            };
        }

        IMenuController controller = Api.StardewUI.ViewEngine.CreateMenuControllerFromAsset($"{Api.StardewUI.ViewsPrefix}/ButtonsBoxView", viewModel);
        viewModel.Controller = controller;
        MenusManager.Show(controller, viewModel, parentMenu, replaceExisting);

        return viewModel;
    }

    public static async Task ShowAsync(
        string message,
        BoxButton[] buttons,
        Func<bool>? readyToClose = null,
        IClickableMenu? parentMenu = null,
        bool replaceExisting = false)
    {
        if (Api.StardewUI.ViewEngine is null)
        {
            Mod.Logger.Log("ViewEngine is null.", LogLevel.Warn);
            return;
        }

        TaskCompletionSource tsc = new();
        ButtonsBoxViewModel viewModel = new(message, buttons, readyToClose, tsc);
        foreach (BoxButton button in buttons)
        {
            Action oldAction = button.Action;
            button.Action = () =>
            {
                oldAction.Invoke();
                tsc.TrySetResult();
                if (button.ExitOnClick)
                {
                    viewModel.Controller?.Menu.exitThisMenu();
                }
            };
        }

        IMenuController controller = Api.StardewUI.ViewEngine.CreateMenuControllerFromAsset($"{Api.StardewUI.ViewsPrefix}/ButtonsBoxView", viewModel);
        viewModel.Controller = controller;
        MenusManager.Show(controller, viewModel, parentMenu, replaceExisting);

        await tsc.Task;
    }

    public bool ReadyToClose()
    {
        return _isReadyToClose?.Invoke() ?? true;
    }

    public void OnClose()
    {
        if (_tsc?.Task.IsCompleted ?? false)
        {
            _tsc.TrySetResult();
        }
    }

}