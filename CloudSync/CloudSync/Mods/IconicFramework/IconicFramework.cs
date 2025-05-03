using CloudSync.ViewModels;
using LeFauxMods.Common.Integrations.IconicFramework;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace CloudSync.Mods.IconicFramework;

public static class IconicFramework
{
    public static void Setup(IManifest manifest)
    {
        IIconicFrameworkApi? api;
        try
        {
            api = Mod.ModHelper.ModRegistry.GetApi<IIconicFrameworkApi>("furyx639.ToolbarIcons");
            if (api is null)
            {
                Mod.Logger.Log("Couldn't get the IconicFramework API.", LogLevel.Error);
                return;
            }
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"Failed to get the IconicFramework API: {ex}", LogLevel.Error);
            return;
        }

        Api.IconicFramework = api;
        Api.IconicFramework.AddToolbarIcon(
            manifest.UniqueID,
            $"Mods/{manifest.UniqueID}/Sprites/Icons",
            new Rectangle(20, 0, 24, 16),
            () => "CloudSync",
            I18n.OpenMenu);

        Api.IconicFramework.Subscribe(e =>
        {
            if (e.Id != manifest.UniqueID)
            {
                return;
            }

            HomeViewModel.Show();
        });
    }
}