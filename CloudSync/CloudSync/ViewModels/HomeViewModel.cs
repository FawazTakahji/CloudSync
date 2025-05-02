using CloudSync.Interfaces;
using CloudSync.Mods;
using StardewModdingAPI;
using StardewUI.Framework;
using StardewValley;

namespace CloudSync.ViewModels;

public class HomeViewModel : ViewModelBase
{
    public static void Show(bool playSound = true)
    {
        if (Api.StardewUI.ViewEngine is null)
        {
            Mod.Logger.Log("ViewEngine is null.", LogLevel.Warn);
            return;
        }

        HomeViewModel viewModel = new();
        IMenuController controller = Api.StardewUI.ViewEngine.CreateMenuControllerFromAsset($"{Api.StardewUI.ViewsPrefix}/HomeView", viewModel);
        MenusManager.Show(controller, viewModel, isTitleSubMenu: true);

        if (playSound)
        {
            Game1.playSound("newArtifact");
        }
    }

    public void OpenMenu(string menu)
    {
        switch (menu)
        {
            case "Settings":
            {
                SettingsViewModel.Show(Mod.Extensions);
                break;
            }
            case "Local":
            {
                ICloudClient? client = CheckCloudClient();
                if (client is null)
                {
                    return;
                }
                LocalSavesViewModel.Show(client, Game1.activeClickableMenu);
                break;
            }
            case "Cloud":
            {
                ICloudClient? client = CheckCloudClient();
                if (client is null)
                {
                    return;
                }
                CloudSavesViewModel.Show(client, Game1.activeClickableMenu);
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