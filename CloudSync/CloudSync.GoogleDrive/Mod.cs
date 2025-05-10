using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace CloudSync.GoogleDrive;

public class Mod : StardewModdingAPI.Mod
{
    public static IModHelper ModHelper = null!;
    public static IMonitor Logger = null!;
    public static Config Config = null!;

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

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        Mods.StardewUI.Setup(ModManifest);
    }

    public override object? GetApi()
    {
        return new ExtensionApi();
    }
}