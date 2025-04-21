using CloudSync.Dropbox.ViewModels;
using CloudSync.Interfaces;
using StardewValley.Menus;

namespace CloudSync.Dropbox;

public class ExtensionApi : IExtensionApi
{
    private static CloudClient? _client;

    /// <inheritdoc />
    public ICloudClient GetClient()
    {
        return _client ??= new CloudClient();
    }

    /// <inheritdoc />
    public void ShowSettings(IClickableMenu? parentMenu = null)
    {
        SettingsViewModel.Show(parentMenu);
    }
}