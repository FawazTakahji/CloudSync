using CloudSync.Dropbox.ViewModels;
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

        try
        {
            Config = helper.ReadConfig<Config>();
        }
        catch (Exception ex)
        {
            Logger.Log($"An error occured while loading config: {ex}", LogLevel.Error);
            Config = new();
        }

        ViewsPrefix = $"Mods/{ModManifest.UniqueID}/Views";

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        IViewEngine? viewEngine = ModHelper.ModRegistry.GetApi<IViewEngine>("focustense.StardewUI");
        if (viewEngine is null)
        {
            Monitor.Log("Couldn't load IViewEngine", LogLevel.Warn);
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