using CloudSync.Dropbox.ViewModels;
using Dropbox.Api;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewUI.Framework;

namespace CloudSync.Dropbox;

public class Mod : StardewModdingAPI.Mod
{
    public static IModHelper ModHelper = null!;
    public static IMonitor Logger = null!;
    public static Config Config = null!;
    public static IViewEngine ViewEngine = null!;
    public static string ViewsPrefix = null!;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModHelper = helper;
        Logger = Monitor;
        Config = helper.ReadConfig<Config>();
        ViewsPrefix = $"Mods/{ModManifest.UniqueID}/Views";

        if (!string.IsNullOrEmpty(Config.RefreshToken) && !string.IsNullOrEmpty(Config.AppKey))
        {
            CloudClient.DropboxClient = new DropboxClient(Config.RefreshToken, Config.AppKey);
        }

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        IViewEngine? viewEngine = ModHelper.ModRegistry.GetApi<IViewEngine>("focustense.StardewUI");
        if (viewEngine is null)
        {
            Monitor.Log("Couldn't load IViewEngine", LogLevel.Alert);
        }
        else
        {
            ViewEngine = viewEngine;
            ViewEngine.RegisterViews(ViewsPrefix, "Assets/Views");
            ViewEngine.PreloadAssets();
            ViewEngine.PreloadModels(typeof(SettingsViewModel));
#if DEBUG
            ViewEngine.EnableHotReloading();
#endif
        }

        ModHelper.Events.GameLoop.GameLaunched -= OnGameLaunched;
    }

    public override object? GetApi()
    {
        return new ExtensionApi();
    }
}