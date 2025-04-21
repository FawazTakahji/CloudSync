using StardewValley;
using StardewValley.Menus;

namespace CloudSync.Utilities;

public static class UI
{
    public static IClickableMenu? GetTopMenu()
    {
        if (Game1.activeClickableMenu is not { } menu)
            return null;

        while (menu.GetChildMenu() is not null)
        {
            menu = menu.GetChildMenu();
        }

        return menu;
    }
}