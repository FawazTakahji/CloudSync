using CloudSync.ViewModels;
using StardewModdingAPI;
using StardewUI.Framework;

namespace CloudSync.Mods.StardewUI;

public static class StardewUI
{
    public static void Setup(IManifest manifest)
    {
        IViewEngine? api;
        try
        {
            api = Mod.ModHelper.ModRegistry.GetApi<IViewEngine>("focustense.StardewUI");
            if (api is null)
            {
                Mod.Logger.Log("Couldn't load IViewEngine.", LogLevel.Alert);
                return;
            }
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while loading IViewEngine: {ex}", LogLevel.Error);
            return;
        }

        Api.StardewUI.ViewEngine = api;
        Api.StardewUI.ViewsPrefix = $"Mods/{manifest.UniqueID}/Views";
        Api.StardewUI.SpritesPrefix = $"Mods/{manifest.UniqueID}/Sprites";

        Api.StardewUI.ViewEngine.RegisterViews(Api.StardewUI.ViewsPrefix, "assets/views");
        Api.StardewUI.ViewEngine.RegisterSprites(Api.StardewUI.SpritesPrefix, "assets/sprites");
        Api.StardewUI.ViewEngine.PreloadAssets();
        Api.StardewUI.ViewEngine.PreloadModels(
            typeof(ButtonsBoxViewModel),
            typeof(CloudSavesViewModel),
            typeof(HomeViewModel),
            typeof(LocalSavesViewModel),
            typeof(MessageBoxViewModel),
            typeof(SettingsViewModel));
#if DEBUG
        Api.StardewUI.ViewEngine.EnableHotReloadingWithSourceSync();
#endif
    }
}