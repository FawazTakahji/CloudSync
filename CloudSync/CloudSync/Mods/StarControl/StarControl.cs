using CloudSync.ViewModels;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarControl;
using StardewModdingAPI;
using StardewValley;

namespace CloudSync.Mods.StarControl;

public static class StarControl
{
    public static void Setup(IManifest manifest)
    {
        IStarControlApi? api;
        try
        {
            api = Mod.ModHelper.ModRegistry.GetApi<IStarControlApi>("focustense.StarControl");
            if (api is null)
            {
                Mod.Logger.Log("Couldn't get the StarControl API.", LogLevel.Warn);
                return;
            }
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"Failed to get the StarControl API: {ex}", LogLevel.Error);
            return;
        }

        Api.StarControl = api;
        Api.StarControl.RegisterItems(
            manifest,
            new []
            {
                new OpenMenuItem($"{manifest.UniqueID}.OpenMenu")
            });
    }
}

internal class OpenMenuItem : IRadialMenuItem
{
    public string Id { get; }
    public string Title { get; } = "CloudSync";
    public string Description => I18n.OpenMenu();

    public Texture2D? Texture { get; }
    public Rectangle? SourceRectangle { get; }

    public OpenMenuItem(string id)
    {
        Id = id;

        try
        {
            Texture = Mod.ModHelper.ModContent.Load<Texture2D>("assets/sprites/Icons.png");
            SourceRectangle = new Rectangle(0, 42, 24, 16);
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while loading icons texture: {ex}");
        }
    }

    public ItemActivationResult Activate(Farmer who, DelayedActions delayedActions, ItemActivationType activationType = ItemActivationType.Primary)
    {
        HomeViewModel.Show();
        return ItemActivationResult.Custom;
    }
}