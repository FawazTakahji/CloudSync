using CloudSync.Mods;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewUI.Framework;
using StardewValley;

namespace CloudSync.UI;

public static class UploadBanner
{
    private static IViewDrawable? _drawable;

    public static void Check()
    {
        if (Api.StardewUI.ViewEngine is null)
        {
            Mod.Logger.Log("Couldn't show upload banner: ViewEngine is null.", LogLevel.Alert);
            return;
        }

        if (Mod.UploadingSaves.Count > 0)
        {
            if (_drawable is not null)
            {
                return;
            }

            _drawable = Api.StardewUI.ViewEngine.CreateDrawableFromAsset($"{Api.StardewUI.ViewsPrefix}/BannerView");
            _drawable.Context = new { Text = I18n.Ui_UploadBanner_Uploading() };

            Mod.ModHelper.Events.Display.Rendered += OnRendered;
        }
        else
        {
            _drawable?.Dispose();
            _drawable = null;

            Mod.ModHelper.Events.Display.Rendered -= OnRendered;
        }
    }

    private static void OnRendered(object? sender, RenderedEventArgs e)
    {
        _drawable?.Draw(e.SpriteBatch,
            new Vector2(16, (Game1.viewport.Height / 2) - (_drawable.ActualSize.Y / 2)));
    }
}