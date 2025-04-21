using CloudSync.Interfaces;
using StardewValley.Menus;

namespace ExampleExtension;

public class ExtensionApi : IExtensionApi
{
    /// <inheritdoc />
    public ICloudClient GetClient()
    {
        return new CloudClient();
    }

    /// <inheritdoc />
    public void ShowSettings(IClickableMenu? parentMenu = null)
    {
        throw new NotImplementedException();
    }
}