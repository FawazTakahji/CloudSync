using CloudSync.Interfaces;
using CloudSync.ViewModels;
using StardewUI.Framework;
using StardewValley;
using StardewValley.Menus;

namespace CloudSync;

public static class MenusManager
{
    // private static readonly Dictionary<IClickableMenu, ViewModelBase> Menus = new();

    public static void Show(IClickableMenu menu, IClickableMenu? parentMenu = null, bool replaceExisting = false, bool isTitleSubMenu = false)
    {
        // Menus.Add(menu, viewModel);
        if (isTitleSubMenu && Game1.activeClickableMenu is TitleMenu)
        {
            TitleMenu.subMenu = menu;
        }
        else if (parentMenu is not null)
        {
            // remove later
            parentMenu.GetChildMenu()?.exitThisMenu();
            parentMenu.SetChildMenu(menu);
        }
        else if (replaceExisting || Utilities.UI.GetTopMenu() is not { } topMenu)
        {
            Game1.activeClickableMenu = menu;
        }
        else
        {
            topMenu.SetChildMenu(menu);
        }
    }

    public static void Show(
        IMenuController controller,
        ViewModelBase viewModel,
        IClickableMenu? parentMenu = null,
        bool replaceExisting = false,
        bool isTitleSubMenu = false,
        bool enableCloseButton = false)
    {
        viewModel.Controller = controller;
        if (viewModel is IReadyToClose readyToClose)
        {
            controller.CanClose = () => readyToClose.ReadyToClose();
        }
        if (viewModel is IOnClosing onClosing)
        {
            controller.Closing += () => onClosing.OnClosing();
        }
        if (viewModel is IOnClose onClose)
        {
            controller.Closed += () => onClose.OnClose();
        }
        if (enableCloseButton)
        {
            controller.EnableCloseButton();
        }

        Show(controller.Menu, parentMenu, replaceExisting, isTitleSubMenu);
    }

    // public static void Remove(IClickableMenu menu)
    // {
    //     Menus.Remove(menu);
    // }
    //
    // public static void Remove(ViewModelBase viewModel)
    // {
    //     var menu = Menus.FirstOrDefault(kvp => kvp.Value == viewModel).Key;
    //     Menus.Remove(menu);
    // }
    //
    // public static IClickableMenu? GetMenu(ViewModelBase viewModel)
    // {
    //     return Menus.FirstOrDefault(kvp => kvp.Value == viewModel).Key;
    // }
    //
    // public static ViewModelBase? GetViewModel(IClickableMenu menu)
    // {
    //     return Menus.FirstOrDefault(kvp => kvp.Key == menu).Value;
    // }
}