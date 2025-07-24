using CloudSync.GoogleDrive.ViewModels;
using StardewModdingAPI;
using StardewUI.Framework;

namespace CloudSync.GoogleDrive.Mods;

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
                Mod.Logger.Log("Couldn't load IViewEngine.", LogLevel.Warn);
                return;
            }
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while loading IViewEngine: {ex}", LogLevel.Error);
            return;
        }

        Api.ViewEngine = api;
        Api.ViewsPrefix = $"Mods/{manifest.UniqueID}/Views";

        Api.ViewEngine.RegisterViews(Api.ViewsPrefix, "assets/views");
        Api.ViewEngine.PreloadAssets();
        Api.ViewEngine.PreloadModels(typeof(SettingsViewModel));
#if DEBUG
        Api.ViewEngine.EnableHotReloadingWithSourceSync();
#endif
    }

}