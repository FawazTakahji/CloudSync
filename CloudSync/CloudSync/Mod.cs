using CloudSync.Extensions;
using CloudSync.Models;
using CloudSync.Patches;
using CloudSync.UI;
using CloudSync.Utilities;
using CloudSync.ViewModels;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewUI.Framework;
using StardewValley;

namespace CloudSync;

internal sealed class Mod : StardewModdingAPI.Mod
{
    public static IModHelper ModHelper = null!;
    public static IMonitor Logger = null!;
    public static Config Config = null!;
    public static IViewEngine ViewEngine = null!;
    public static string ViewsPrefix = null!;
    public static string SpritesPrefix = null!;
    public static readonly List<Extension> Extensions = new();
    private static bool _shouldUpload;
    public static readonly HashSet<string> UploadingSaves = new();

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModHelper = helper;
        Logger = Monitor;
        Config = helper.ReadConfig<Config>();
        Patcher.Apply(ModManifest.UniqueID);
        ViewsPrefix = $"Mods/{ModManifest.UniqueID}/Views";
        SpritesPrefix = $"Mods/{ModManifest.UniqueID}/Sprites";
        MenuButton.Init();

        var extensions = helper.ModRegistry
            .GetAll()
            .Where(m => m.Manifest.ExtraFields.ContainsKey("IsCloudSyncExtension"));
        foreach (IModInfo modInfo in extensions)
        {
            string? name = null;
            if (modInfo.Manifest.ExtraFields.TryGetValue("CloudProvider", out var provider) && provider is string providerString)
                name = providerString;

            Extension extension = new(name, modInfo.Manifest.Author, modInfo.Manifest.UniqueID);
            Extensions.Add(extension);
        }

        ModHelper.Events.GameLoop.GameLaunched += OnGameLaunched;
        ModHelper.Events.GameLoop.DayStarted += (_, _) => OnDayStarted().SafeFireAndForget(ex => Monitor.Log(ex.ToString(), LogLevel.Error));
        // Avoid uploading the save after loading it
        ModHelper.Events.GameLoop.ReturnedToTitle += (_, _) => _shouldUpload = false;
        ModHelper.Events.GameLoop.SaveCreated += (_, _) => _shouldUpload = true;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        IViewEngine? viewEngine = ModHelper.ModRegistry.GetApi<IViewEngine>("focustense.StardewUI");
        if (viewEngine is null)
        {
            Monitor.Log("Couldn't load IViewEngine.", LogLevel.Alert);
        }
        else
        {
            ViewEngine = viewEngine;
            ViewEngine.RegisterViews(ViewsPrefix, "assets/views");
            ViewEngine.RegisterSprites(SpritesPrefix, "assets/sprites");
            ViewEngine.PreloadAssets();
            ViewEngine.PreloadModels(
                typeof(ButtonsBoxViewModel),
                typeof(CloudSavesViewModel),
                typeof(HomeViewModel),
                typeof(LocalSavesViewModel),
                typeof(MessageBoxViewModel),
                typeof(SettingsViewModel));
#if DEBUG
            ViewEngine.EnableHotReloadingWithSourceSync();
#endif
        }

        Saves.Purge().SafeFireAndForget(ex => Logger.Log(ex.ToString(), LogLevel.Error));
        ModHelper.Events.GameLoop.GameLaunched -= OnGameLaunched;
    }

    private async Task OnDayStarted()
    {
        if (!Game1.IsMasterGame || !Config.AutoUpload)
        {
            return;
        }
        // Avoid uploading the save after loading it
        if (!_shouldUpload)
        {
            _shouldUpload = true;
            return;
        }

        await Saves.Upload();
    }
}