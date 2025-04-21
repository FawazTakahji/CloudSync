using CloudSync.Enums;
using CloudSync.Interfaces;
using StardewUI.Framework;
using StardewValley.Menus;

namespace CloudSync.ViewModels;

public class MessageBoxViewModel : ViewModelBase, IReadyToClose, IOnClose
{
    public string Message;
    private readonly Func<bool>? _isReadyToClose;
    private readonly TaskCompletionSource<MessageBoxResult>? _tsc;
    public MessageBoxButtons Buttons { get; set; }

    public MessageBoxViewModel(string message, MessageBoxButtons buttons = MessageBoxButtons.Ok, Func<bool>? readyToClose = null, bool isAsync = false)
    {
        Message = message;
        Buttons = buttons;
        _isReadyToClose = readyToClose;
        if (isAsync)
        {
            _tsc = new TaskCompletionSource<MessageBoxResult>();
        }
    }

    public static MessageBoxViewModel Show(
        string message,
        MessageBoxButtons buttons = MessageBoxButtons.Ok,
        Func<bool>? readyToClose = null,
        IClickableMenu? parentMenu = null,
        bool replaceExisting = false)
    {
        MessageBoxViewModel viewModel = new(message, buttons, readyToClose);
        IMenuController controller = Mod.ViewEngine.CreateMenuControllerFromAsset($"{Mod.ViewsPrefix}/MessageBox", viewModel);
        viewModel.Controller = controller;
        MenusManager.Show(controller, viewModel, parentMenu, replaceExisting);

        return viewModel;
    }

    public static async Task<MessageBoxResult?> ShowAsync(
        string message,
        MessageBoxButtons buttons = MessageBoxButtons.Ok,
        Func<bool>? readyToClose = null,
        IClickableMenu? parentMenu = null,
        bool replaceExisting = false)
    {
        MessageBoxViewModel viewModel = new(message, buttons, readyToClose, true);
        IMenuController controller = Mod.ViewEngine.CreateMenuControllerFromAsset($"{Mod.ViewsPrefix}/MessageBox", viewModel);
        viewModel.Controller = controller;
        MenusManager.Show(controller, viewModel, parentMenu, replaceExisting);

        return await viewModel._tsc?.Task;
    }

    public bool ReadyToClose()
    {
        return _isReadyToClose?.Invoke() ?? true;
    }

    public void Cancel()
    {
        if (ReadyToClose())
        {
            CloseMenu();
            _tsc?.TrySetResult(MessageBoxResult.Cancel);
        }
    }

    public void Ok()
    {
        if (ReadyToClose())
        {
            CloseMenu();
            _tsc?.TrySetResult(MessageBoxResult.Ok);
        }
    }

    public void Yes()
    {
        if (ReadyToClose())
        {
            CloseMenu();
            _tsc?.TrySetResult(MessageBoxResult.Yes);
        }
    }

    public void No()
    {
        if (ReadyToClose())
        {
            CloseMenu();
            _tsc?.TrySetResult(MessageBoxResult.No);
        }
    }

    public void OnClose()
    {
        if (_tsc?.Task.IsCompleted ?? false)
        {
            _tsc.TrySetResult(MessageBoxResult.Cancel);
        }
    }
}