using CloudSync.Extensions;
using CloudSync.Models;
using CloudSync.Mods;
using CloudSync.Mods.IconicFramework;
using CloudSync.Patches;
using CloudSync.UI;
using CloudSync.Utilities;
using CloudSync.ViewModels;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewUISetup = CloudSync.Mods.StardewUI.StardewUI;
using StarControlSetup = CloudSync.Mods.StarControl.StarControl;

namespace CloudSync;

internal sealed class Mod : StardewModdingAPI.Mod
{
    public static IModHelper ModHelper = null!;
    public static IMonitor Logger = null!;
    public static Config Config = null!;
    public static readonly List<Extension> Extensions = new();
    private static bool _shouldUpload;
    public static readonly HashSet<string> UploadingSaves = new();
    public static bool GCSInstalled;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModHelper = helper;
        Logger = Monitor;
        Config = helper.ReadConfig<Config>();
        Patcher.Apply(ModManifest.UniqueID);
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

        GCSInstalled = helper.ModRegistry.IsLoaded("FawazT.GlobalConfigSettingsRewrite") || helper.ModRegistry.IsLoaded("Gaphodil.GlobalConfigSettings");

        ModHelper.Events.GameLoop.GameLaunched += OnGameLaunched;
        ModHelper.Events.GameLoop.DayStarted += (_, _) => OnDayStarted().SafeFireAndForget(ex => Monitor.Log(ex.ToString(), LogLevel.Error));
        // Avoid uploading the save after loading it
        ModHelper.Events.GameLoop.ReturnedToTitle += (_, _) => _shouldUpload = false;
        ModHelper.Events.GameLoop.SaveCreated += (_, _) => _shouldUpload = true;

        helper.ConsoleCommands.Add("cloudsync_open", I18n.CommandDescription(), (_, _) => HomeViewModel.Show());
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        StardewUISetup.Setup(ModManifest);

        if (Api.StardewUI.ViewEngine is null)
        {
            ModHelper.Events.Content.AssetRequested += OnAssetRequested;
        }
        IconicFramework.Setup(ModManifest);
        StarControlSetup.Setup(ModManifest);


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

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo($"Mods/{ModManifest.UniqueID}/Sprites/Icons"))
        {
            e.LoadFromModFile<Texture2D>("assets/sprites/icons.png", AssetLoadPriority.Low);
        }
    }
}