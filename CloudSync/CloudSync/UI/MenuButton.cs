using CloudSync.ViewModels;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace CloudSync.UI;

public static class MenuButton
{
    private static ClickableTextureComponent _menuButton = null!;
    private static bool _wasButtonHovered;

    public static void Init()
    {
        Texture2D texture = Mod.ModHelper.ModContent.Load<Texture2D>("Assets/MenuButton.png");
        _menuButton = new ClickableTextureComponent(ButtonLocation, texture, new Rectangle(0, 0, 27, 25), 3)
        {
            myID = 82000,
            upNeighborID = 81112,
            downNeighborID = 81118,
            leftNeighborID = 81117
        };

        Mod.ModHelper.Events.Display.MenuChanged += OnMenuChanged;
        SubscribeEvents();
    }

    private static void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.NewMenu is TitleMenu)
        {
            _menuButton.bounds = ButtonLocation;
            SubscribeEvents();
        }
        else if (e.NewMenu is not TitleMenu && e.OldMenu is TitleMenu)
        {
            UnsubscribeEvents();
        }
    }

    private static void SubscribeEvents()
    {
        Mod.ModHelper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;
        Mod.ModHelper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
        Mod.ModHelper.Events.Display.WindowResized += OnWindowResized;
        Mod.ModHelper.Events.Input.ButtonPressed += OnButtonPressed;
        Mod.ModHelper.Events.Input.CursorMoved += OnCursorMoved;
    }

    private static void UnsubscribeEvents()
    {
        Mod.ModHelper.Events.GameLoop.OneSecondUpdateTicked -= OnOneSecondUpdateTicked;
        Mod.ModHelper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;
        Mod.ModHelper.Events.Display.WindowResized -= OnWindowResized;
        Mod.ModHelper.Events.Input.ButtonPressed -= OnButtonPressed;
        Mod.ModHelper.Events.Input.CursorMoved -= OnCursorMoved;
    }

    private static void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
    {
        if (Game1.activeClickableMenu is not TitleMenu titleMenu)
        {
            return;
        }

        titleMenu.allClickableComponents?.Add(_menuButton);
        ClickableComponent? windowButton =
            titleMenu.allClickableComponents?.FirstOrDefault(button => button.myID == 81112);
        ClickableComponent? languageButton =
            titleMenu.allClickableComponents?.FirstOrDefault(button => button.myID == 81118);
        if (windowButton != null)
        {
            windowButton.downNeighborID = _menuButton.myID;
        }
        if (languageButton != null)
        {
            languageButton.upNeighborID = _menuButton.myID;
        }
    }

    private static void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (!ShouldDrawButton)
            return;

        Draw(e.SpriteBatch);
    }

    private static void OnWindowResized(object? sender, WindowResizedEventArgs e)
    {
        _menuButton.bounds = ButtonLocation;
    }

    private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!ShouldDrawButton)
            return;

        if (e.Button is SButton.MouseLeft or SButton.ControllerA && PointContainsButton(e.Cursor.ScreenPixels))
        {
            HomeViewModel.Show(showCloseButton: false);
        }
    }

    private static void OnCursorMoved(object? sender, CursorMovedEventArgs e)
    {
        if (!ShouldDrawButton)
            return;

        Point mousePosition = new Point((int)e.NewPosition.ScreenPixels.X, (int)e.NewPosition.ScreenPixels.Y);
        bool isUpdateButtonHovered = _menuButton.containsPoint(mousePosition.X, mousePosition.Y);
        if (isUpdateButtonHovered != _wasButtonHovered)
        {
            _menuButton.sourceRect.X += _wasButtonHovered ? -27 : 27;
            if (!_wasButtonHovered)
            {
                Game1.playSound("Cowboy_Footstep");
            }
        }
        _wasButtonHovered = isUpdateButtonHovered;
    }

    private static bool PointContainsButton(Vector2 p)
    {
        return _menuButton.containsPoint((int)p.X, (int)p.Y);
    }

    private static void Draw(SpriteBatch b)
    {
        _menuButton.draw(Game1.spriteBatch);
        Game1.activeClickableMenu.drawMouse(b);
    }

    private static Rectangle ButtonLocation => new(
        Game1.viewport.Width - 114,
        Game1.viewport.Height - 250 - 48,
        81,
        75);

    private static bool ShouldDrawButton =>
        Game1.activeClickableMenu is TitleMenu
            { isTransitioningButtons: false, titleInPosition: true, transitioningCharacterCreationMenu: false}
        && TitleMenu.subMenu is null;
}