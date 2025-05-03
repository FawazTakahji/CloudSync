﻿using LeFauxMods.Common.Integrations.IconicFramework;
using StarControl;
using StardewUI.Framework;

namespace CloudSync.Mods;

public static class Api
{
    public static class StardewUI
    {
        public static string ViewsPrefix = "Mods/FawazT.CloudSync/Views";
        public static string SpritesPrefix = "Mods/FawazT.CloudSync/Sprites";
        public static IViewEngine? ViewEngine = null;
    }

    public static IIconicFrameworkApi? IconicFramework = null;

    public static IStarControlApi? StarControl = null;
}