using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace CloudSync.UI;

public record Sprite(Texture2D Texture, Rectangle SourceRect, SliceSettings SliceSettings)
{
    public static Sprite ForItem(string itemId)
    {
        var itemData = ItemRegistry.GetDataOrErrorItem(itemId);
        return new(itemData.GetTexture(), itemData.GetSourceRect(), new(4f));
    }
}

public record SliceSettings(float Scale);