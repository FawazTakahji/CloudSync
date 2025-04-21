using CloudSync.Interfaces;
using PropertyChanged.SourceGenerator;
using StardewUI.Framework;
using StardewValley.Menus;

namespace CloudSync.ViewModels;

public partial class InputBoxViewModel : ViewModelBase, IOnClose
{
    private readonly TaskCompletionSource<string?> _tsc;
    public string Message;
    [Notify] private string _input = string.Empty;

    public InputBoxViewModel(string message)
    {
        Message = message;
        _tsc = new TaskCompletionSource<string?>();
    }

    public static async Task<string?> ShowAsync(string message, IClickableMenu? parentMenu = null)
    {
        InputBoxViewModel viewModel = new(message);
        IMenuController controller = Mod.ViewEngine.CreateMenuControllerFromAsset($"{Mod.ViewsPrefix}/InputBox", viewModel);
        viewModel.Controller = controller;
        MenusManager.Show(controller, viewModel, parentMenu);

        return await viewModel._tsc.Task;
    }

    public void Ok()
    {
        _tsc.TrySetResult(Input);
        CloseMenu();
    }

    public void Cancel()
    {
        _tsc.TrySetResult(null);
        CloseMenu();
    }

    public void Paste()
    {
        string? clipboardText = string.Empty;
        StardewValley.DesktopClipboard.GetText(ref clipboardText);
        if (!string.IsNullOrEmpty(clipboardText))
        {
            Input = clipboardText;
        }
    }

    public void OnClose()
    {
        if (!_tsc.Task.IsCompleted)
            _tsc.TrySetResult(null);
    }
}