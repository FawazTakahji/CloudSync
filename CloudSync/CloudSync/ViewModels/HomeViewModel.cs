using CloudSync.Interfaces;
using CloudSync.Mods;
using StardewModdingAPI;
using StardewUI.Framework;
using StardewValley;
using StardewValley.Menus;

namespace CloudSync.ViewModels;

public class HomeViewModel : ViewModelBase
{
    public static void Show(bool playSound = true, bool showCloseButton = true)
    {
        if (Api.StardewUI.ViewEngine is null)
        {
            Mod.Logger.Log("ViewEngine is null.", LogLevel.Warn);
            return;
        }

        HomeViewModel viewModel = new();
        IMenuController controller = Api.StardewUI.ViewEngine.CreateMenuControllerFromAsset($"{Api.StardewUI.ViewsPrefix}/HomeView", viewModel);
        viewModel.Controller = controller;

        if (Game1.activeClickableMenu is TitleMenu titleMenu)
        {
            if (TitleMenu.subMenu is null)
            {
                TitleMenu.subMenu = controller.Menu;
            }
            else
            {
                if (showCloseButton)
                {
                    controller.EnableCloseButton();
                }
                titleMenu.SetChildMenu(controller.Menu);
            }
        }
        else
        {
            if (Game1.activeClickableMenu is not null)
            {
                Game1.activeClickableMenu.exitThisMenu();
            }

            if (showCloseButton)
            {
                controller.EnableCloseButton();
            }

            Game1.activeClickableMenu = controller.Menu;
        }

        if (playSound)
        {
            Game1.playSound("newArtifact");
        }
    }

    public void OpenMenu(string menu)
    {
        IClickableMenu? parent = Game1.activeClickableMenu is TitleMenu && TitleMenu.subMenu == Controller?.Menu ? Game1.activeClickableMenu : Controller?.Menu;

        switch (menu)
        {
            case "Settings":
            {
                SettingsViewModel.Show(Mod.Extensions, parent);
                break;
            }
            case "Local":
            {
                ICloudClient? client = CheckCloudClient();
                if (client is null)
                {
                    return;
                }
                LocalSavesViewModel.Show(client, parent);
                break;
            }
            case "Cloud":
            {
                ICloudClient? client = CheckCloudClient();
                if (client is null)
                {
                    return;
                }
                CloudSavesViewModel.Show(client, parent);
                break;
            }
        }
    }

    private ICloudClient? CheckCloudClient()
    {
        if (Mod.Config.SelectedExtension is not { } selectedExtension || string.IsNullOrEmpty(selectedExtension))
        {
            MessageBoxViewModel.Show(I18n.Messages_HomeViewModel_NeedSelectExtension(), parentMenu: Game1.activeClickableMenu);
            return null;
        }

        if (Mod.Extensions.All(ext => ext.UniqueId != selectedExtension))
        {
            MessageBoxViewModel.Show(I18n.Messages_HomeViewModel_ExtensionNotInstalled(selectedExtension), parentMenu: Game1.activeClickableMenu);
            return null;
        }

        IExtensionApi? api = Mod.ModHelper.ModRegistry.GetApi<IExtensionApi>(Mod.Config.SelectedExtension);
        if (api is null)
        {
            MessageBoxViewModel.Show(I18n.Messages_HomeViewModel_FailedRetrieveApi(selectedExtension), parentMenu: Game1.activeClickableMenu);
            return null;
        }
        ICloudClient client = api.GetClient();
        if (!client.IsAuthenticated())
        {
            MessageBoxViewModel.Show(I18n.Messages_HomeViewModel_NeedOpenSettingsAndAuth(), parentMenu: Game1.activeClickableMenu);
            return null;
        }

        return client;
    }
}