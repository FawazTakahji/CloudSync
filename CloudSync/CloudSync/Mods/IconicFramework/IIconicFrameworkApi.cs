using Microsoft.Xna.Framework;
using StardewModdingAPI;

// ReSharper disable once CheckNamespace
namespace LeFauxMods.Common.Integrations.IconicFramework;

public interface IIconicFrameworkApi
{
    /// <summary>Adds an icon.</summary>
    /// <param name="id">A unique identifier for the icon.</param>
    /// <param name="texturePath">The path to the texture icon.</param>
    /// <param name="sourceRect">The source rectangle of the icon.</param>
    /// <param name="getTitle">Text to appear as the title in the Radial Menu.</param>
    /// <param name="getDescription">Text to appear when hovering over the icon.</param>
    public void AddToolbarIcon(
        string id,
        string texturePath,
        Rectangle? sourceRect,
        Func<string>? getTitle,
        Func<string>? getDescription);

/*
    /// <summary>Adds an icon.</summary>
    /// <param name="id">A unique identifier for the icon.</param>
    /// <param name="texturePath">The path to the texture icon.</param>
    /// <param name="sourceRect">The source rectangle of the icon.</param>
    /// <param name="getTitle">Text to appear as the title in the Radial Menu.</param>
    /// <param name="getDescription">Text to appear when hovering over the icon.</param>
    /// <param name="onClick">An action to perform when the icon is pressed.</param>
    /// <param name="onRightClick">An optional secondary action to perform when the icon is pressed.</param>
    public void AddToolbarIcon(
        string id,
        string texturePath,
        Rectangle? sourceRect,
        Func<string>? getTitle,
        Func<string>? getDescription,
        Action onClick,
        Action? onRightClick = null);
*/

/*
    /// <summary>Adds or replaces a single icon with a default identifier and onClick action.</summary>
    /// <param name="texturePath">The path to the texture icon.</param>
    /// <param name="sourceRect">The source rectangle of the icon.</param>
    /// <param name="getTitle">Text to appear as the title in the Radial Menu.</param>
    /// <param name="getDescription">Text to appear when hovering over the icon.</param>
    /// <param name="onClick">An action to perform when the icon is pressed.</param>
    /// <param name="onRightClick">An optional secondary action to perform when the icon is pressed.</param>
    public void AddToolbarIcon(
        string texturePath,
        Rectangle? sourceRect,
        Func<string>? getTitle,
        Func<string>? getDescription,
        Action onClick,
        Action? onRightClick = null);
*/

    /// <summary>Subscribes to an event handler.</summary>
    /// <param name="handler">The event handler to subscribe.</param>
    public void Subscribe(Action<IIconPressedEventArgs> handler);

/*
    /// <summary>Unsubscribes an event handler from an event.</summary>
    /// <param name="handler">The event handler to unsubscribe.</param>
    public void Unsubscribe(Action<IIconPressedEventArgs> handler);
*/
}

/// <summary>Represents the event arguments for a toolbar icon being pressed.</summary>
public interface IIconPressedEventArgs
{
    /// <summary>Gets the button that was pressed.</summary>
    public SButton Button { get; }

    /// <summary>Gets the id of the icon that was pressed.</summary>
    public string Id { get; }
}