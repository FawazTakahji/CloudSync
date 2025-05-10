using CloudSync.GoogleDrive.ViewModels;
using CloudSync.Interfaces;
using StardewValley.Menus;

namespace CloudSync.GoogleDrive;

public class ExtensionApi : IExtensionApi
{
    private static CloudClient? _client;

    public ICloudClient GetClient()
    {
        return _client ??= new CloudClient();
    }

    public void ShowSettings(IClickableMenu? parentMenu = null)
    {
        SettingsViewModel.Show(parentMenu);
    }
}