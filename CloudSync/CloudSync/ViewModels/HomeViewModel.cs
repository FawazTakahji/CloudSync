using CloudSync.Interfaces;
using CloudSync.UI;
using StardewUI.Framework;
using StardewValley;

namespace CloudSync.ViewModels;

public class HomeViewModel : ViewModelBase
{
    public Sprite LocalSavesSprite { get; } = Sprite.ForItem("(O)842");
    public Sprite CloudSavesSprite { get; } = Sprite.ForItem("(O)864");
    public Sprite SettingsSprite { get; } = Sprite.ForItem("(O)867");

    public static void Show()
    {
        HomeViewModel viewModel = new();
        IMenuController controller = Mod.ViewEngine.CreateMenuControllerFromAsset($"{Mod.ViewsPrefix}/HomeView", viewModel);
        MenusManager.Show(controller, viewModel, isTitleSubMenu: true);
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